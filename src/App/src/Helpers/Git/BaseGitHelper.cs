using System;
using System.Linq;
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
         ILocalGitCommitStorageFactory gitFactory)
      {
         _mergeRequestCache = mergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += onMergeRequestEvent;

         _discussionCache = discussionCache;

         _gitFactory = gitFactory;
         _synchronizeInvoke = synchronizeInvoke;

         scheduleAllProjectsUpdate();
      }

      public void Dispose()
      {
         _mergeRequestCache.MergeRequestEvent -= onMergeRequestEvent;
      }

      protected void scheduleAllProjectsUpdate()
      {
         foreach (ProjectKey key in _mergeRequestCache.GetProjects())
         {
            IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestCache.GetMergeRequests(key)
               .Select(x => new MergeRequestKey(key, x.IId));
            foreach (MergeRequestKey mrk in mergeRequestKeys)
            {
               scheduleSingleProjectUpdate(mrk);
            }
         }
      }

      private void onMergeRequestEvent(UserEvents.MergeRequestEvent e)
      {
         if (e.New || e.Commits)
         {
            FullMergeRequestKey fmk = e.FullMergeRequestKey;
            MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
            scheduleSingleProjectUpdate(mrk);
         }
      }

      private void scheduleSingleProjectUpdate(MergeRequestKey mrk)
      {
         ILocalGitCommitStorage repo = getRepository(mrk);
         if (repo != null)
         {
            _synchronizeInvoke.BeginInvoke(new Action(async () => await updateAsync(mrk, repo)), null);
         }
      }

      async private Task updateAsync(MergeRequestKey mrk, ILocalGitCommitStorage repo)
      {
         if (repo.Data == null || repo.Updater == null)
         {
            Debug.WriteLine(String.Format(
               "[BaseGitHelper] Update failed. Repository is not ready: {0}",
               repo.ToString()));
            return;
         }

         if (!_updating.Add(repo))
         {
            return;
         }
         preUpdate(mrk, repo);

         try
         {
            await doUpdate(mrk, repo);
         }
         finally
         {
            _updating.Remove(repo);
         }
      }

      protected ILocalGitCommitStorage getRepository(MergeRequestKey mrk)
      {
         return _gitFactory?.GetStorage(mrk);
      }

      protected abstract void preUpdate(MergeRequestKey mrk, ILocalGitCommitStorage repo);
      protected abstract Task doUpdate(MergeRequestKey mrk, ILocalGitCommitStorage repo);

      private readonly ILocalGitCommitStorageFactory _gitFactory;
      protected readonly IDiscussionCache _discussionCache;
      protected readonly IMergeRequestCache _mergeRequestCache;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      protected readonly HashSet<ILocalGitCommitStorage> _updating = new HashSet<ILocalGitCommitStorage>();
   }
}
