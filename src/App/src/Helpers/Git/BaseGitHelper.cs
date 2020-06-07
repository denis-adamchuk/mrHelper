using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   internal abstract class BaseGitHelper : IDisposable
   {
      internal BaseGitHelper(
         IMergeRequestCache mergeRequestCache,
         IDiscussionCache discussionCache,
         IProjectUpdateContextProviderFactory updateContextProviderFactory,
         ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepositoryFactory gitFactory)
      {
         _mergeRequestCache = mergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += onMergeRequestEvent;

         _discussionCache = discussionCache;
         _contextProviderFactory = updateContextProviderFactory;

         _gitFactory = gitFactory;
         _gitFactory.RepositoryCloned += onRepositoryCloned;
         _synchronizeInvoke = synchronizeInvoke;

         scheduleAllProjectsUpdate();
      }

      public void Dispose()
      {
         _gitFactory.RepositoryCloned -= onRepositoryCloned;
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

      private void onRepositoryCloned(ILocalGitRepository repo)
      {
         scheduleSingleProjectUpdate(repo.ProjectKey);
      }

      private void scheduleSingleProjectUpdate(ProjectKey projectKey)
      {
         ILocalGitRepository repo = getRepository(projectKey);
         if (repo != null)
         {
            _synchronizeInvoke.BeginInvoke(new Action(async () => await updateAsync(repo)), null);
         }
      }

      async private Task updateAsync(ILocalGitRepository repo)
      {
         if (repo.Data == null || repo.Updater == null || repo.ExpectingClone)
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

      protected ILocalGitRepository getRepository(ProjectKey projectKey)
      {
         return _gitFactory?.GetRepository(projectKey);
      }

      protected abstract void preUpdate(ILocalGitRepository repo);
      protected abstract Task doUpdate(ILocalGitRepository repo);

      private readonly ILocalGitRepositoryFactory _gitFactory;
      protected readonly IDiscussionCache _discussionCache;
      protected readonly IMergeRequestCache _mergeRequestCache;
      private readonly IProjectUpdateContextProviderFactory _contextProviderFactory;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      protected readonly HashSet<ILocalGitRepository> _updating = new HashSet<ILocalGitRepository>();
   }
}
