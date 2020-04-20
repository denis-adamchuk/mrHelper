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
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
         Action<string> onGitStatusChange, Action<bool> onCancelEnabled, ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepository repo, IEnumerable<string> headShas, bool singleCommitFetchSupported)
      {
         if (repo == null || hostProperties == null || headShas == null)
         {
            throw new ArgumentException("Bad arguments");
         }

         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _onGitStatusChange = onGitStatusChange;
         _onCancelEnabled = onCancelEnabled;
         _synchronizeInvoke = synchronizeInvoke;
         _repo = repo;
         _headShas = headShas;
         _singleCommitFetchSupported = singleCommitFetchSupported;
      }

      async public Task<bool> CreateChainAsync()
      {
         if (_repo == null)
         {
            Trace.TraceInformation("[CommitChainCreator] No commits will be created because repository object is null");
            return false;
         }

         IEnumerable<string> heads = _headShas.Where(x => !_repo.ContainsSHA(x)).ToArray();
         if (!_singleCommitFetchSupported)
         {
            return await createBranches(heads);
         }

         await fetchMissingCommits(heads);
         return true;
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

         await _repo.Updater.CancelUpdate();
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
                  "Creating {0} branch{1} at GitLab...", shas.Count(), shas.Count() > 1 ? "es" : ""));

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

            await fetchMissingCommits(shas);
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

      async private Task fetchMissingCommits(IEnumerable<string> shas)
      {
         if (shas == null || !shas.Any())
         {
            return;
         }

         _onStatusChange?.Invoke("Fetching new branches from remote repository...");
         try
         {
            await _repo.Updater.Update(new CommitBasedContext(shas), _onGitStatusChange);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle("Cannot update git repository", ex);
         }
         finally
         {
            _onGitStatusChange?.Invoke(String.Empty);
         }
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
      private readonly IEnumerable<string> _headShas;

      private RepositoryManager _repositoryManager;
      private bool _isCancelEnabled = true;
      private readonly bool _singleCommitFetchSupported;
   }
}

