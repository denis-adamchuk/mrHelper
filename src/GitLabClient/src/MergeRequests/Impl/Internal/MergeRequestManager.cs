using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Common;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestManager :
      IDisposable,
      IMergeRequestCache,
      IProjectUpdateContextProviderFactory
   {
      internal MergeRequestManager(GitLabClientContext clientContext, InternalCacheUpdater cacheUpdater,
         string hostname, SessionContext context)
      {
         _clientContext = clientContext;
         _cacheUpdater = cacheUpdater;

         _updateManager = new UpdateManager(_clientContext, hostname, context, _cacheUpdater);
         _updateManager.MergeRequestEvent += onUpdate;
      }

      public void Dispose()
      {
         _updateManager.MergeRequestEvent -= onUpdate;
         _updateManager.Dispose();
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

      public IProjectUpdateContextProvider GetLocalBasedContextProvider(ProjectKey projectKey)
      {
         return new LocalBasedContextProvider(getAllVersions(projectKey));
      }

      public IProjectUpdateContextProvider GetRemoteBasedContextProvider(MergeRequestKey mrk)
      {
         SessionOperator tempOperator = new SessionOperator(mrk.ProjectKey.HostName, _clientContext.HostProperties);
         return new RemoteBasedContextProvider(getAllVersions(mrk.ProjectKey), mrk, tempOperator);
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
      public IUpdateToken RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         _updateManager.RequestOneShotUpdate(mrk, intervals, onUpdateFinished);
         return null;
      }

      private void onUpdate(UserEvents.MergeRequestEvent e)
      {
         MergeRequestEvent?.Invoke(e);
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly UpdateManager _updateManager;
      private readonly GitLabClientContext _clientContext;
   }
}

