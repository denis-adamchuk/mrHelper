using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitClient;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Repository;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(RepositoryManager repositoryManager, Action<string> onStatusChange,
        ILocalGitRepository repo, string baseSHA, IEnumerable<string> commits)
      {
         _repositoryManager = repositoryManager;
         _onStatusChange = onStatusChange;
         _repo = repo;
         _baseSHA = baseSHA;
         _commits = commits.ToArray(); // make a copy
      }

      async public Task CreateChainAsync()
      {
         Debug.Assert(String.IsNullOrEmpty(_baseSHA) || _repo.ContainsSHA(_baseSHA));
         if (String.IsNullOrEmpty(_baseSHA) || _commits == null)
         {
            return;
         }

         IEnumerable<ComparisonResult> comparisonResults = await getComparisonResults(_baseSHA, _commits);
         if (comparisonResults == null)
         {
            return;
         }

         IEnumerable<Patch> patches = await createPatches(comparisonResults);
         if (patches == null)
         {
            return;
         }

         await createBranchesForPatches(patches);
      }

      async public Task CancelAsync()
      {
         if (_currentOperation != null)
         {
            await _currentOperation.Cancel();
         }
      }

      private struct ComparisonResult
      {
         internal Comparison Comparison;
         internal string PrevSHA;
         internal string SHA;
      }

      async private Task<IEnumerable<ComparisonResult>> getComparisonResults(
         string baseSHA, IEnumerable<string> commits)
      {
         string prevSHA = baseSHA;

         List<ComparisonResult> comparisons = new List<ComparisonResult>();
         int iCommit = 1;
         foreach (string SHA in commits.Reverse())
         {
            _onStatusChange(String.Format(
               "Loading commit comparison results from GitLab: {0}/{1}", iCommit++, commits.Count()));

            if (!_repo.ContainsSHA(SHA) && !_repo.ContainsBranch(Helpers.GitTools.FakeSHA(SHA)))
            {
               try
               {
                  comparisons.Add(new ComparisonResult
                     {
                        Comparison = await _repositoryManager.CompareAsync(_repo.ProjectKey, prevSHA, SHA),
                        PrevSHA = prevSHA,
                        SHA = SHA
                     });
               }
               catch (RepositoryManagerException ex)
               {
                  ExceptionHandlers.Handle("Cannot obtain comparison result", ex);
                  _onStatusChange("Failed to obtain comparison result");
                  return null;
               }
            }

            prevSHA = SHA;
         }
         return comparisons;
      }

      private struct Patch
      {
         internal string Text;
         internal string PrevSHA;
         internal string SHA;
      }

      async Task<IEnumerable<Patch>> createPatches(IEnumerable<ComparisonResult> comparisonResults)
      {
         List<Patch> patches = new List<Patch>();
         int iComparison = 1;
         foreach (ComparisonResult comparisonResult in comparisonResults)
         {
            _onStatusChange(String.Format(
               "Creating patches: {0}/{1}", iComparison++, comparisonResults.Count()));

            string patch = await createPatchText(comparisonResult);
            if (String.IsNullOrEmpty(patch))
            {
               _onStatusChange("Failed to create a patch");
               return null;
            }
            patches.Add(new Patch
               {
                  Text = patch,
                  PrevSHA = comparisonResult.PrevSHA,
                  SHA = comparisonResult.SHA
               });
         }
         return patches;
      }

      async private Task<string> createPatchText(ComparisonResult comparisonResult)
      {
         StringBuilder stringBuilder = new StringBuilder();
         foreach (DiffStruct diff in comparisonResult.Comparison.Diffs)
         {
            string gitDiff;
            if (diff.Diff == String.Empty)
            {
               try
               {
                  gitDiff = await createDiffFromRawFiles(diff.Old_Path, comparisonResult.PrevSHA,
                     diff.New_Path, comparisonResult.SHA);
               }
               catch (Exception ex)
               {
                  ExceptionHandlers.Handle("Cannot create diff from raw files", ex);
                  return null;
               }
            }
            else
            {
               gitDiff = diff.Diff.Replace("\\n", "\n");
            }

            stringBuilder.AppendLine(String.Format("--- a/{0}", diff.Old_Path));
            stringBuilder.AppendLine(String.Format("+++ b/{0}", diff.New_Path));
            stringBuilder.AppendLine(gitDiff);
         }
         return stringBuilder.ToString();
      }

      async private Task<string> createDiffFromRawFiles(string oldFilename, string oldSha,
         string newFilename, string newSha)
      {
         GitLabSharp.Entities.File oldFile =
            await _repositoryManager.LoadFileAsync(_repo.ProjectKey, oldFilename, oldSha);
         GitLabSharp.Entities.File newFile =
            await _repositoryManager.LoadFileAsync(_repo.ProjectKey, newFilename, newSha);

         string oldTempFilename = System.IO.Path.Combine(_repo.Path, "temp1___" + oldFile.File_Name);
         string newTempFilename = System.IO.Path.Combine(_repo.Path, "temp2___" + newFile.File_Name);

         string diffArguments = String.Format("diff --no-index -- {0} {1}",
            StringUtils.EscapeSpaces(oldTempFilename), StringUtils.EscapeSpaces(newTempFilename));

         string oldFileContent = StringUtils.Base64Decode(oldFile.Content).Replace("\n", "\r\n");
         string newFileContent = StringUtils.Base64Decode(newFile.Content).Replace("\n", "\r\n");

         try
         {
            FileUtils.OverwriteFile(oldTempFilename, oldFileContent);
            FileUtils.OverwriteFile(newTempFilename, newFileContent);

            IEnumerable<string> gitDiffOutput =
               ExternalProcess.Start("git", diffArguments, true, _repo.Path).StdOut;

            return String.Join("\n", gitDiffOutput.Skip(4));
         }
         finally
         {
            if (System.IO.File.Exists(oldTempFilename))
            {
               System.IO.File.Delete(oldTempFilename);
            }
            if (System.IO.File.Exists(newTempFilename))
            {
               System.IO.File.Delete(newTempFilename);
            }
         }
      }

      async private Task createBranchesForPatches(IEnumerable<Patch> patches)
      {
         _currentOperation = _repo.Operations.CreateOperation("CreateBranchFromPatch");

         LocalGitRepositoryStateData sdata = _repo.State.SaveState();
         int iPatch = 1;
         foreach (Patch patch in patches)
         {
            _onStatusChange(String.Format("Applying patches: {0}/{1}", iPatch++, patches.Count()));

            try
            {
               await _currentOperation.Run(patch.PrevSHA, Helpers.GitTools.FakeSHA(patch.SHA), patch.Text);
            }
            catch (LocalGitRepositoryOperationException ex)
            {
               _repo.State.RestoreState(sdata);
               ex.Rollback();
               ExceptionHandlers.Handle("Cannot create a branch for patch", ex);
               _onStatusChange("Failed to apply a patch");
               return;
            }
         }
         _repo.State.RestoreState(sdata);

         _currentOperation = null;
      }

      private readonly RepositoryManager _repositoryManager;
      private readonly Action<string> _onStatusChange;
      private ILocalGitRepositoryOperation _currentOperation;
      private readonly ILocalGitRepository _repo;
      private readonly string _baseSHA;
      private readonly IEnumerable<string> _commits;
   }
}

