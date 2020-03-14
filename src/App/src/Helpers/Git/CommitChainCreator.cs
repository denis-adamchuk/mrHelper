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
using mrHelper.Client.Types;
using mrHelper.Client.Versions;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
        ILocalGitRepository repo, MergeRequestKey mrk)
      {
         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _repo = repo;
         _mrk = mrk;
         _versionManager = new VersionManager(hostProperties);
      }

      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
        ILocalGitRepository repo, string baseSha, IEnumerable<string> commits)
      {
         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _repo = repo;
         _baseSha = baseSha;
         _commits = commits;
      }

      async public Task CreateChainAsync()
      {
         if (_creator != null || _repo == null)
         {
            return;
         }

         try
         {
            if (_baseSha != null && _commits != null)
            {
               await doCreateSingleChainAsync();
            }
            else
            {
               await doCreateMultiChainAsync();
            }
         }
         finally
         {
            _creator = null;
         }
      }

      async private Task doCreateSingleChainAsync()
      {
         if (!String.IsNullOrEmpty(_baseSha) && !_repo.ContainsSHA(_baseSha))
         {
            MessageBox.Show("Base commit of this merge request cannot be found in repository. "
               + "Such merge requests can be viewed in Web UI only.", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Trace.TraceWarning(String.Format("[CommitChainCreator] Base commit is missing: {0}", _baseSha));
            Debug.Assert(false);
            return;
         }

         _creator = new SingleCommitChainCreatorImpl(_hostProperties, _onStatusChange, _repo, _baseSha, _commits,
            () => saveState(), () => _stateData?.Branch ?? String.Empty);

         try
         {
            await _creator.CreateChainAsync(String.Empty);
         }
         catch (Exception ex)
         {
            restoreState();
            if (ex is LocalGitRepositoryOperationException gex)
            {
               handleGitException(gex);
            }
            else
            {
               ExceptionHandlers.Handle("Failed to create a commit chain", ex);
            }
         }

         restoreState();
      }

      async private Task doCreateMultiChainAsync()
      {
         _onStatusChange("Loading versions meta information from GitLab");
         IEnumerable<GitLabSharp.Entities.Version> versions = await _versionManager.GetVersions(_mrk);
         if (versions == null)
         {
            return; // cancelled by user
         }

         int iVersion = 1;
         foreach (GitLabSharp.Entities.Version version in versions)
         {
            _onStatusChange(String.Format(
               "Loading versions from GitLab: {0}/{1}", iVersion, versions.Count()));
            GitLabSharp.Entities.Version? versionDetailed = await _versionManager.GetVersion(version, _mrk);
            if (versionDetailed == null)
            {
               break; // cancelled by user
            }

            _creator = new SingleCommitChainCreatorImpl(_hostProperties, _onStatusChange, _repo,
               versionDetailed.Value.Base_Commit_SHA, versionDetailed.Value.Commits.Select(x => x.Id),
               () => saveState(), () => _stateData?.Branch ?? String.Empty);

            try
            {
               string prefix = String.Format("Processing versions: {0}/{1}. ", iVersion++, versions.Count());
               if (!await _creator.CreateChainAsync(prefix))
               {
                  break; // failed or cancelled by user
               }
            }
            catch (Exception ex)
            {
               restoreState();
               if (ex is LocalGitRepositoryOperationException gex)
               {
                  handleGitException(gex);
               }
               else
               {
                  ExceptionHandlers.Handle("Failed to create a commit chain", ex);
               }
               break; // failed or cancelled by user
            }
         }

         restoreState();
      }

      private void handleGitException(LocalGitRepositoryOperationException ex)
      {
         ExceptionHandlers.Handle("Cannot create a branch for patch", ex);

         _onStatusChange(String.Format("Reverting changes made to repository {0}", _repo.Path));
         ex.Rollback();

         if (!ex.CancelledByUser)
         {
            _onStatusChange("Failed to create a branch for patch");
         }
      }

      private void saveState()
      {
         if (_stateData.HasValue)
         {
            return;
         }

         _onStatusChange(String.Format("Saving current state of git repository {0}", _repo.Path));
         _stateData = _repo.State.SaveState();
      }

      private void restoreState()
      {
         if (!_stateData.HasValue)
         {
            return;
         }

         _onStatusChange(String.Format("Restoring state of repository {0}", _repo.Path));
         _repo.State.RestoreState(_stateData.Value);
         _stateData = null;
      }

      async public Task CancelAsync()
      {
         if (_versionManager != null)
         {
            await _versionManager.CancelAsync();
         }

         if (_creator != null)
         {
            await _creator.CancelAsync();
         }
      }

      private readonly VersionManager _versionManager;
      private readonly IHostProperties _hostProperties;
      private readonly Action<string> _onStatusChange;
      private readonly ILocalGitRepository _repo;
      private readonly MergeRequestKey _mrk;
      private readonly string _baseSha;
      private readonly IEnumerable<string> _commits;
      private SingleCommitChainCreatorImpl _creator;
      private LocalGitRepositoryStateData? _stateData;

      private class SingleCommitChainCreatorImpl
      {
         internal SingleCommitChainCreatorImpl(IHostProperties hostProperties, Action<string> onStatusChange,
           ILocalGitRepository repo, string baseSha, IEnumerable<string> commits, Action onPrePatch,
           Func<string> getCurrentBranch)
         {
            _repositoryManager = new RepositoryManager(hostProperties);
            _onStatusChange = onStatusChange;
            _onPrePatch = onPrePatch;
            _getCurrentBranch = getCurrentBranch;
            _repo = repo;
            _originalBaseSHA = baseSha;
            _originalCommitCollection = commits;
         }

         /// <summary>
         /// Return value means if operation completed (true) or cancelled by user (false)
         /// </summary>
         async public Task<bool> CreateChainAsync(string messagePrefix)
         {
            string baseSha = _originalBaseSHA;
            List<string> commits = _originalCommitCollection.ToList(); // copy
            while (!_repo.ContainsSHA(baseSha))
            {
               Commit? baseCommit = await _repositoryManager.LoadCommitAsync(_repo.ProjectKey, baseSha);
               if (baseCommit == null || baseCommit.Value.Parent_Ids == null || !baseCommit.Value.Parent_Ids.Any())
               {
                  return false;
               }

               commits.Add(baseSha);
               baseSha = baseCommit.Value.Parent_Ids.First();
            }

            IEnumerable<Tuple<string, string>> shaPairs = collectShaPairs(baseSha, commits, messagePrefix);
            if (shaPairs == null)
            {
               return false;
            }

            if (!shaPairs.Any())
            {
               return true;
            }

            _onPrePatch?.Invoke();

            IEnumerable<ComparisonResult> comparisonResults = await getComparisonResults(shaPairs, messagePrefix);
            if (comparisonResults == null)
            {
               return false;
            }

            IEnumerable<Patch> patches = await createPatches(comparisonResults, messagePrefix);
            if (patches == null)
            {
               return false;
            }

            await createBranchesForPatches(patches, messagePrefix);
            return true;
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

         private IEnumerable<Tuple<string, string>> collectShaPairs(string baseSHA, IEnumerable<string> commits,
            string prefix)
         {
            string prevSHA = baseSHA;

            List<Tuple<string, string>> shaPairs = new List<Tuple<string, string>>();
            int iCommit = 1;
            foreach (string SHA in commits.Reverse())
            {
               _onStatusChange(String.Format(
                  "{0}Checking commit cache: {1}/{2}", prefix, iCommit++, commits.Count()));

               if (!_repo.ContainsSHAOrBranch(SHA, Helpers.GitTools.FakeSHA(SHA)))
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
            IEnumerable<Tuple<string, string>> shaPairs, string prefix)
         {
            List<ComparisonResult> comparisons = new List<ComparisonResult>();
            int iCommit = 1;
            foreach (Tuple<string, string> shaPair in shaPairs)
            {
               _onStatusChange(String.Format(
                  "{0}Loading commit comparison results from GitLab: {1}/{2}", prefix, iCommit, shaPairs.Count()));

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
                     PrevSHA = shaPair.Item1,
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

         async Task<IEnumerable<Patch>> createPatches(IEnumerable<ComparisonResult> comparisonResults, string prefix)
         {
            List<Patch> patches = new List<Patch>();
            int iComparison = 1;
            foreach (ComparisonResult comparisonResult in comparisonResults)
            {
               string status = String.Format(
                  "{0}Creating patches: {1}/{2}. ", prefix, iComparison++, comparisonResults.Count());
               _onStatusChange(status);

               try
               {
                  string text = await createPatchText(comparisonResult, status);
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

         async private Task<string> createPatchText(ComparisonResult comparisonResult, string prefix)
         {
            StringBuilder stringBuilder = new StringBuilder();
            int iFile = 1;
            foreach (DiffStruct diff in comparisonResult.Comparison.Diffs)
            {
               _onStatusChange(String.Format(
                  "{0}Processing file: {1}/{2}", prefix, iFile++, comparisonResult.Comparison.Diffs.Count()));

               string gitDiff;
               if (diff.Diff == String.Empty)
               {
                  if (diff.Renamed_File)
                  {
                     gitDiff = String.Format(
                          "diff --git a/{0} b/{1}\n"
                        + "similarity index 100%\n"
                        + "rename from {0}\n"
                        + "rename to {1}", diff.Old_Path, diff.New_Path);
                  }
                  else
                  {
                     gitDiff = await createDiffFromRawFiles(diff.Old_Path, comparisonResult.PrevSHA,
                        diff.New_Path, comparisonResult.SHA, diff.New_File, diff.Deleted_File);
                     if (gitDiff == null)
                     {
                        return null;
                     }
                  }
               }
               else
               {
                  gitDiff = diff.Diff.Replace("\\n", "\n");
               }

               if (gitDiff == String.Empty)
               {
                  continue;
               }

               stringBuilder.AppendLine(String.Format("--- a/{0}", diff.Old_Path));
               stringBuilder.AppendLine(String.Format("+++ b/{0}", diff.New_Path));
               stringBuilder.AppendLine(gitDiff);
            }
            return stringBuilder.ToString();
         }

         async private Task<string> createDiffFromRawFiles(string oldFilename, string oldSha,
            string newFilename, string newSha, bool isNew, bool isDeleted)
         {
            GitLabSharp.Entities.File? oldFile = null;
            GitLabSharp.Entities.File? newFile = null;

            try
            {
               oldFile = isNew
                  ? new File { Content = String.Empty, File_Name = Guid.NewGuid().ToString() }
                  : await _repositoryManager.LoadFileAsync(_repo.ProjectKey, oldFilename, oldSha);
               if (oldFile == null)
               {
                  return null;
               }

               newFile = isDeleted
                  ? new File { Content = String.Empty, File_Name = Guid.NewGuid().ToString() }
                  : await _repositoryManager.LoadFileAsync(_repo.ProjectKey, newFilename, newSha);
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

         async private Task createBranchesForPatches(IEnumerable<Patch> patches, string prefix)
         {
            if (!patches.Any())
            {
               return;
            }

            _currentOperation = _repo.Operations.CreateOperation("CreateBranchFromPatch");

            try
            {
               int iPatch = 1;
               foreach (Patch patch in patches)
               {
                  _onStatusChange(String.Format(
                     "{0}Applying patches: {1}/{2}", prefix, iPatch++, patches.Count()));
                  string prevSHA = _repo.ContainsSHA(patch.PrevSHA)
                     ? patch.PrevSHA
                     : GitTools.FakeSHA(patch.PrevSHA);
                  await _currentOperation.Run(prevSHA, Helpers.GitTools.FakeSHA(patch.SHA),
                     patch.Text, _getCurrentBranch());
               }
            }
            finally
            {
               _currentOperation = null;
            }
         }

         private readonly RepositoryManager _repositoryManager;
         private readonly Action<string> _onStatusChange;
         private readonly Action _onPrePatch;
         private readonly Func<string> _getCurrentBranch;
         private ILocalGitRepositoryOperation _currentOperation;
         private readonly ILocalGitRepository _repo;
         private readonly string _originalBaseSHA;
         private readonly IEnumerable<string> _originalCommitCollection;
      }
   }
}

