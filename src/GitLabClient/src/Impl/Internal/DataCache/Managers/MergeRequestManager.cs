using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient.Managers
{
   internal class MergeRequestManager :
      IDisposable,
      IMergeRequestCache
   {
      internal MergeRequestManager(
         DataCacheContext dataCacheContext,
         InternalCacheUpdater cacheUpdater,
         string hostname,
         IHostProperties hostProperties,
         DataCacheConnectionContext context,
         IModificationNotifier modificationNotifier)
      {
         _dataCacheContext = dataCacheContext;
         _cacheUpdater = cacheUpdater;
         _modificationNotifier = modificationNotifier;

         _modificationNotifier.MergeRequestModified += onMergeRequestModified;

         if (context.UpdateRules.UpdateMergeRequestsPeriod.HasValue)
         {
            DataCacheConnectionContext updateContext = new DataCacheConnectionContext(
               new DataCacheCallbacks(null, null), // disable callbacks from updates
               context.UpdateRules,
               context.CustomData);

            _updateManager = new UpdateManager(_dataCacheContext, hostname, hostProperties,
               updateContext, _cacheUpdater);
            _updateManager.MergeRequestEvent += onUpdate;
         }
      }

      public void Dispose()
      {
         _modificationNotifier.MergeRequestModified -= onMergeRequestModified;

         if (_updateManager != null)
         {
            _updateManager.MergeRequestEvent -= onUpdate;
            _updateManager.Dispose();
         }
      }

      public event Action<UserEvents.MergeRequestEvent> MergeRequestEvent;

      public IEnumerable<ProjectKey> GetProjects()
      {
         return _cacheUpdater.Cache.GetProjects();
      }

      public IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey)
      {
         return _cacheUpdater.Cache.GetMergeRequests(projectKey);
      }

      public MergeRequest GetMergeRequest(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequest> mergeRequests = GetMergeRequests(mrk.ProjectKey);
         return mergeRequests.FirstOrDefault(x => x.IId == mrk.IId); // `null` if not found
      }

      public Version GetLatestVersion(MergeRequestKey mrk)
      {
         return _cacheUpdater.Cache.GetVersions(mrk).OrderBy(x => x.Created_At).LastOrDefault();
      }

      public Version GetLatestVersion(ProjectKey projectKey)
      {
         return getAllVersions(projectKey).OrderBy(x => x.Created_At).LastOrDefault();
      }

      public IEnumerable<GitLabSharp.Entities.Version> GetVersions(MergeRequestKey mrk)
      {
         return _cacheUpdater.Cache.GetVersions(mrk);
      }

      public IEnumerable<GitLabSharp.Entities.Version> GetVersions(ProjectKey projectKey)
      {
         return getAllVersions(projectKey);
      }

      public IEnumerable<GitLabSharp.Entities.Commit> GetCommits(MergeRequestKey mrk)
      {
         return _cacheUpdater.Cache.GetCommits(mrk);
      }

      private IEnumerable<Version> getAllVersions(ProjectKey projectKey)
      {
         List<Version> versions = new List<Version>();
         foreach (MergeRequest mergeRequest in _cacheUpdater.Cache.GetMergeRequests(projectKey))
         {
            MergeRequestKey mrk = new MergeRequestKey(projectKey, mergeRequest.IId);
            foreach (Version version in _cacheUpdater.Cache.GetVersions(mrk))
            {
               versions.Add(version);
            }
         }
         return versions;
      }

      /// <summary>
      /// Request to update the specified MR after the specified time periods (in milliseconds)
      /// </summary>
      public void RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         _updateManager?.RequestOneShotUpdate(mrk, intervals, onUpdateFinished);
      }

      private void onUpdate(UserEvents.MergeRequestEvent e)
      {
         MergeRequestEvent?.Invoke(e);
      }

      private void onMergeRequestModified(MergeRequestKey mergeRequestKey)
      {
         throw new NotImplementedException();
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly UpdateManager _updateManager;
      private readonly DataCacheContext _dataCacheContext;
      private readonly IModificationNotifier _modificationNotifier;
   }
}

