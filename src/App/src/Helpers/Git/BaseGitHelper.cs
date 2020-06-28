using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   internal abstract class BaseGitHelper : IDisposable
   {
      internal BaseGitHelper(
         IMergeRequestCache mergeRequestCache,
         IDiscussionCache discussionCache,
         ISynchronizeInvoke synchronizeInvoke,
         ILocalCommitStorageFactory gitFactory)
      {
         _mergeRequestCache = mergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += onMergeRequestEvent;

         _discussionCache = discussionCache;

         _gitFactory = gitFactory;
         if (_gitFactory != null)
         {
            _gitFactory.GitRepositoryCloned += onGitRepositoryCloned;
         }
         _synchronizeInvoke = synchronizeInvoke;

         scheduleAllProjectsUpdate();
      }

      public void Dispose()
      {
         if (_gitFactory != null)
         {
            _gitFactory.GitRepositoryCloned -= onGitRepositoryCloned;
         }
         _mergeRequestCache.MergeRequestEvent -= onMergeRequestEvent;
      }

      protected void scheduleAllProjectsUpdate()
      {
         foreach (ProjectKey key in _mergeRequestCache.GetProjects())
         {
            scheduleSingleProjectUpdate(key);
         }
      }

      private void onMergeRequestEvent(UserEvents.MergeRequestEvent e)
      {
         if (e.New || e.Commits)
         {
            scheduleSingleProjectUpdate(e.FullMergeRequestKey.ProjectKey);
         }
      }

      private void onGitRepositoryCloned(ILocalCommitStorage repo)
      {
         scheduleSingleProjectUpdate(repo.ProjectKey);
      }

      private void scheduleSingleProjectUpdate(ProjectKey projectKey)
      {
         ILocalCommitStorage repo = getRepository(projectKey);
         if (repo != null)
         {
            _synchronizeInvoke.BeginInvoke(new Action(async () => await updateAsync(repo)), null);
         }
      }

      async private Task updateAsync(ILocalCommitStorage repo)
      {
         if (repo.Git == null || repo.Updater == null)
         {
            Debug.WriteLine(String.Format(
               "[BaseGitHelper] Update failed. Repository is not ready (Host={0}, Project={1})",
               repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
            return;
         }

         if (!_updating.Add(repo))
         {
            return;
         }
         preUpdate(repo);

         try
         {
            await doUpdate(repo);
         }
         finally
         {
            _updating.Remove(repo);
         }
      }

      protected ILocalCommitStorage getRepository(ProjectKey projectKey)
      {
         return _gitFactory?.GetStorage(projectKey, ConfigurationHelper.GetPreferredStorageType(Program.Settings));
      }

      protected abstract void preUpdate(ILocalCommitStorage repo);
      protected abstract Task doUpdate(ILocalCommitStorage repo);

      private readonly ILocalCommitStorageFactory _gitFactory;
      protected readonly IDiscussionCache _discussionCache;
      protected readonly IMergeRequestCache _mergeRequestCache;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      protected readonly HashSet<ILocalCommitStorage> _updating = new HashSet<ILocalCommitStorage>();
   }
}
