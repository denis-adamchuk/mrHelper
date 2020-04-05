using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitClient;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Repository;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
         Action<string> onGitStatusChange, Action<bool> onCancelEnabled, ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepository repo, MergeRequestKey mrk)
      {
         if (repo == null || hostProperties == null || mrk.Equals(default(MergeRequestKey)))
         {
            throw new ArgumentException("Bad arguments");
         }

         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _onGitStatusChange = onGitStatusChange;
         _onCancelEnabled = onCancelEnabled;
         _synchronizeInvoke = synchronizeInvoke;
         _repo = repo;
         _mrk = mrk;
      }

      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
         Action<string> onGitStatusChange, Action<bool> onCancelEnabled, ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepository repo, string headSha)
      {
         if (repo == null || hostProperties == null || headSha == null)
         {
            throw new ArgumentException("Bad arguments");
         }

         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _onGitStatusChange = onGitStatusChange;
         _onCancelEnabled = onCancelEnabled;
         _synchronizeInvoke = synchronizeInvoke;
         _repo = repo;
         _headSha = headSha;
      }

      async public Task<bool> CreateChainAsync()
      {
         if (_repo == null)
         {
            Trace.TraceInformation("[CommitChainCreator] No commits will be created because repository object is null");
            return false;
         }

         if (_headSha != null)
         {
            if (!_repo.ContainsSHA(_headSha))
            {
               return await createBranches(new string[] { _headSha });
            }
            return true;
         }

         _versionManager = new VersionManager(_hostProperties);
         IEnumerable<string> shas = await getAllVersionHeadSHA(_mrk);
         return await createBranches(shas);
      }

      async public Task CancelAsync()
      {
         if (!IsCancelEnabled)
         {
            Trace.TraceInformation("[CommitChainCreator] Cancellation is not enabled, waiting");
         }

         while (!IsCancelEnabled)
         {
            await Task.Delay(50);
         }

         Trace.TraceInformation("[CommitChainCreator] Cancellation is enabled");

         if (_repositoryManager != null)
         {
            await _repositoryManager.CancelAsync();
         }

         if (_versionManager != null)
         {
            await _versionManager.CancelAsync();
         }

         await _repo.Updater.CancelUpdate();
      }

      async private Task<IEnumerable<string>> getAllVersionHeadSHA(MergeRequestKey mrk)
      {
         IEnumerable<GitLabSharp.Entities.Version> versions;
         try
         {
            _onStatusChange?.Invoke("Loading meta information about versions from GitLab");

            versions = await _versionManager.GetVersions(mrk);
            if (versions == null)
            {
               Trace.TraceInformation("[CommitChainCreator] User cancelled loading meta information");
               return null;
            }
         }
         catch (VersionManagerException ex)
         {
            ExceptionHandlers.Handle("Cannot load meta information about versions", ex);
            return null;
         }

         Trace.TraceInformation(String.Format("[CommitChainCreator] Found {0} versions", versions.Count()));

         HashSet<string> heads = new HashSet<string>();
         bool cancelled = false;
         async Task loadHeads(GitLabSharp.Entities.Version version)
         {
            if (cancelled)
            {
               return;
            }

            GitLabSharp.Entities.Version? versionDetailed;
            try
            {
               _onStatusChange?.Invoke(String.Format(
                  "Loading {0} version{1} from GitLab", versions.Count(), versions.Count() > 1 ? "s" : ""));

               versionDetailed = await _versionManager.GetVersion(version, mrk);
               if (versionDetailed == null)
               {
                  Trace.TraceInformation(String.Format(
                     "[CommitChainCreator] User cancelled loading detailed version {0}", version.Id));
                  cancelled = true;
                  return;
               }
            }
            catch (VersionManagerException ex)
            {
               ExceptionHandlers.Handle("Cannot load meta information about versions", ex);
               return;
            }

            if (!_repo.ContainsSHA(versionDetailed.Value.Head_Commit_SHA))
            {
               heads.Add(versionDetailed.Value.Head_Commit_SHA);
               Trace.TraceInformation(String.Format(
                  "[CommitChainCreator] SHA {0} is not found in {1}",
                  versionDetailed.Value.Head_Commit_SHA, _repo.Path));
            }
            else
            {
               Trace.TraceInformation(String.Format(
                  "[CommitChainCreator] SHA {0} is found in {1}",
                  versionDetailed.Value.Head_Commit_SHA, _repo.Path));
            }
         }
         await TaskUtils.RunConcurrentFunctionsAsync(versions, x => loadHeads(x),
            Constants.VersionsInBatch, Constants.VersionsInterBatchDelay, () => cancelled);
         return heads;
      }

      async private Task<bool> createBranches(IEnumerable<string> shas)
      {
         if (shas == null)
         {
            return false;
         }

         if (!shas.Any())
         {
            return true;
         }

         IsCancelEnabled = false;

         Trace.TraceInformation(String.Format(
            "[CommitChainCreator] Will create/delete {0} branches in {1} at {2}",
            shas.Count(), _repo.ProjectKey.ProjectName, _repo.ProjectKey.HostName));

         _repositoryManager = new RepositoryManager(_hostProperties);

         string getFakeSha(string sha) => "fake_" + sha;

         try
         {
            async Task createBranch(string sha)
            {
               _onStatusChange?.Invoke(String.Format(
                  "Creating {0} branch{1} at GitLab", shas.Count(), shas.Count() > 1 ? "es" : ""));

               Trace.TraceInformation(String.Format(
                  "[CommitChainCreator] Creating branch {0} at GitLab", getFakeSha(sha)));
               Branch? branch = await _repositoryManager.CreateNewBranchAsync(
                  _repo.ProjectKey, getFakeSha(sha), sha);
               Debug.Assert(branch.HasValue); // it is not possible to cancel it
            }
            await TaskUtils.RunConcurrentFunctionsAsync(shas, x => createBranch(x),
               Constants.BranchInBatch, Constants.BranchInterBatchDelay, null);

            Trace.TraceInformation(String.Format(
               "[CommitChainCreator] Created {0} branches", shas.Count()));

            _onStatusChange?.Invoke("Fetching new branches from remote repository");
            try
            {
               await _repo.Updater.Update(null, _onGitStatusChange);
            }
            catch (RepositoryUpdateException ex)
            {
               ExceptionHandlers.Handle("Cannot update git repository", ex);
               return false;
            }
            finally
            {
               _onGitStatusChange?.Invoke(String.Empty);
            }
         }
         catch (RepositoryManagerException ex)
         {
            ExceptionHandlers.Handle("Cannot create a branch at GitLab", ex);
            return false;
         }
         finally
         {
            _synchronizeInvoke.BeginInvoke(new Action(
               async () =>
               {
                  int iBranch = 0;
                  async Task deleteBranch(string sha)
                  {
                     try
                     {
                        Trace.TraceInformation(String.Format(
                           "[CommitChainCreator] Deleting branch {0} at GitLab", getFakeSha(sha)));
                        await _repositoryManager.DeleteBranchAsync(_repo.ProjectKey, getFakeSha(sha));
                        iBranch++;
                     }
                     catch (Exception ex)
                     {
                        ExceptionHandlers.Handle("Cannot delete a branch at GitLab", ex);
                     }
                  }
                  await TaskUtils.RunConcurrentFunctionsAsync(shas, x => deleteBranch(x),
                     Constants.BranchInBatch, Constants.BranchInterBatchDelay, null);

                  Trace.TraceInformation(String.Format("[CommitChainCreator] Deleted {0} branches", iBranch));
                  IsCancelEnabled = true;
               }), null);
         }
         return true;
      }

      public bool IsCancelEnabled
      {
         get
         {
            return _isCancelEnabled;
         }
         set
         {
            _onCancelEnabled?.Invoke(value);
            _isCancelEnabled = value;
         }
      }

      private readonly IHostProperties _hostProperties;
      private readonly Action<string> _onStatusChange;
      private readonly Action<string> _onGitStatusChange;
      private readonly Action<bool> _onCancelEnabled;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly ILocalGitRepository _repo;
      private readonly MergeRequestKey _mrk;
      private readonly string _headSha;

      private RepositoryManager _repositoryManager;
      private VersionManager _versionManager;
      private bool _isCancelEnabled = true;
   }
}

