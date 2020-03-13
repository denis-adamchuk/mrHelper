using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitClient;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Repository;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(IHostProperties settings, Action<string> onStatusChange,
        ILocalGitRepository repo, string baseSHA, IEnumerable<string> commits)
      {
         _repositoryManager = new RepositoryManager(Program.Settings);
         _onStatusChange = onStatusChange;
         _repo = repo;
         _baseSHA = baseSHA;
         _commits = commits.ToArray(); // make a copy
      }

      async public Task CreateChainAsync()
      {
         if (String.IsNullOrEmpty(_baseSHA) || _commits == null)
         {
            return;
         }

         if (!String.IsNullOrEmpty(_baseSHA) && !_repo.ContainsSHA(_baseSHA))
         {
            MessageBox.Show("Base commit of this merge request cannot be found in repository. "
               + "Such merge requests can be viewed in Web UI only.", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         IEnumerable<Tuple<string, string>> shaPairs = collectShaPairs(_baseSHA, _commits);
         if (shaPairs == null)
         {
            return;
         }

         IEnumerable<ComparisonResult> comparisonResults = await getComparisonResults(shaPairs);
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
         if (_repositoryManager != null)
         {
            await _repositoryManager.CancelAsync();
         }

         if (_currentOperation != null)
         {
            await _currentOperation.Cancel();
         }
      }

      private IEnumerable<Tuple<string, string>> collectShaPairs(string baseSHA, IEnumerable<string> commits)
      {
         string prevSHA = baseSHA;

         List<Tuple<string, string>> shaPairs = new List<Tuple<string, string>>();
         int iCommit = 1;
         foreach (string SHA in commits.Reverse())
         {
            _onStatusChange(String.Format(
               "Checking if all commits are already cached: {0}/{1}", iCommit++, commits.Count()));

            if (!_repo.ContainsSHA(SHA) && !_repo.ContainsBranch(Helpers.GitTools.FakeSHA(SHA)))
            {
               shaPairs.Add(new Tuple<string, string>(prevSHA, SHA));
            }

            prevSHA = SHA;
         }
         return shaPairs;
      }

      private struct ComparisonResult
      {
         internal Comparison Comparison;
         internal string PrevSHA;
         internal string SHA;
      }

      async private Task<IEnumerable<ComparisonResult>> getComparisonResults(
         IEnumerable<Tuple<string, string>> shaPairs)
      {
         List<ComparisonResult> comparisons = new List<ComparisonResult>();
         int iCommit = 1;
         foreach (Tuple<string, string> shaPair in shaPairs)
         {
            _onStatusChange(String.Format(
               "Loading commit comparison results from GitLab: {0}/{1}", iCommit, shaPairs.Count()));

            try
            {
               Comparison? comparison = await _repositoryManager.CompareAsync(
                  _repo.ProjectKey, shaPair.Item1, shaPair.Item2);
               if (comparison == null)
               {
                  return null;
               }
               comparisons.Add(new ComparisonResult
                  {
                     Comparison = comparison.Value,
                     PrevSHA = _repo.ContainsSHA(shaPair.Item1) ? shaPair.Item1 : GitTools.FakeSHA(shaPair.Item1),
                     SHA = shaPair.Item2
                  });
            }
            catch (RepositoryManagerException ex)
            {
               ExceptionHandlers.Handle("Cannot obtain comparison result", ex);
               _onStatusChange("Failed to load comparison result from GitLab");
               return null;
            }

            ++iCommit;
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

            try
            {
               string text = await createPatchText(comparisonResult);
               if (String.IsNullOrEmpty(text))
               {
                  return null;
               }
               patches.Add(new Patch
               {
                  Text = text,
                  PrevSHA = comparisonResult.PrevSHA,
                  SHA = comparisonResult.SHA
               });
            }
            catch (Exception ex) // there are many I/O exceptions
            {
               ExceptionHandlers.Handle("Failed to create a patch", ex);
               _onStatusChange("Failed to create a patch");
               return null;
            }
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
               gitDiff = await createDiffFromRawFiles(diff.Old_Path, comparisonResult.PrevSHA,
                  diff.New_Path, comparisonResult.SHA);
               if (String.IsNullOrEmpty(gitDiff))
               {
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
         GitLabSharp.Entities.File? oldFile = null;
         GitLabSharp.Entities.File? newFile = null;

         try
         {
            oldFile = await _repositoryManager.LoadFileAsync(_repo.ProjectKey, oldFilename, oldSha);
            if (oldFile == null)
            {
               return null;
            }

            newFile = await _repositoryManager.LoadFileAsync(_repo.ProjectKey, newFilename, newSha);
            if (newFile == null)
            {
               return null;
            }
         }
         catch (RepositoryManagerException ex)
         {
            ExceptionHandlers.Handle("Failed to obtain raw data", ex);
            _onStatusChange("Failed to load comparison raw file data from GitLab");
            return null;
         }

         string oldTempFilename = System.IO.Path.Combine(_repo.Path, "temp1___" + oldFile?.File_Name);
         string newTempFilename = System.IO.Path.Combine(_repo.Path, "temp2___" + newFile?.File_Name);

         string diffArguments = String.Format("diff --no-index -- {0} {1}",
            StringUtils.EscapeSpaces(oldTempFilename), StringUtils.EscapeSpaces(newTempFilename));

         string oldFileContent = StringUtils.Base64Decode(oldFile?.Content).Replace("\n", "\r\n");
         string newFileContent = StringUtils.Base64Decode(newFile?.Content).Replace("\n", "\r\n");

         try
         {
            FileUtils.OverwriteFile(oldTempFilename, oldFileContent);
            FileUtils.OverwriteFile(newTempFilename, newFileContent);

            IEnumerable<string> gitDiffOutput =
               ExternalProcess.Start("git", diffArguments, true, _repo.Path).StdOut;

            return String.Join("\n", gitDiffOutput.Skip(4));
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               ExceptionHandlers.Handle("Failed to run git diff", ex);
               _onStatusChange("Failed to run git diff for raw files obtained from GitLab");
               return null;
            }
            throw;
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
         if (!patches.Any())
         {
            return;
         }

         _currentOperation = _repo.Operations.CreateOperation("CreateBranchFromPatch");

         _onStatusChange(String.Format("Saving current state of git repository {0}", _repo.Path));
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
               _onStatusChange(String.Format(
                  "Failed to apply a patch. Reverting changes made to repository {0}", _repo.Path));
               ex.Rollback1();

               _onStatusChange(String.Format(
                  "Failed to apply a patch. Restoring state of repository {0}", _repo.Path));
               _repo.State.RestoreState(sdata);

               _onStatusChange(String.Format(
                  "Failed to apply a patch. Reverting changes made to repository {0}", _repo.Path));
               ex.Rollback2();

               ExceptionHandlers.Handle("Cannot create a branch for patch", ex);
               _onStatusChange("Failed to apply a patch");
               return;
            }
         }

         _onStatusChange(String.Format("Restoring state of repository {0}", _repo.Path));
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

