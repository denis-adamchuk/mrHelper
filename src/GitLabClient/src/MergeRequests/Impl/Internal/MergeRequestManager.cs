using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using System.Collections;
using mrHelper.Client.Common;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestManager :
      IDisposable,
      IMergeRequestManager,
      IProjectUpdateContextProviderFactory,
      IWorkflowEventListener,
      IMergeRequestLoaderListener,
      IMergeRequestListLoaderListener,
      IVersionLoaderListener
   {
      internal MergeRequestManager(GitLabClientContext clientContext, IWorkflowLoader workflowLoader)
      {
         _clientContext = clientContext;

         _workflowLoaderNotifier = workflowLoader.GetNotifier();
         _workflowLoaderNotifier.AddListener(this);
      }

      public void Dispose()
      {
         _workflowLoaderNotifier.RemoveListener(this);

         unsubscribe();
      }

      private void unsubscribe()
      {
         _mergeRequestListLoaderNotifier?.RemoveListener(this);
         _mergeRequestListLoaderNotifier = null;

         _versionLoaderNotifier?.RemoveListener(this);
         _versionLoaderNotifier = null;

         (_updateManager.GetNotifier() as INotifier<IMergeRequestLoaderListener>).RemoveListener(this);
         (_updateManager.GetNotifier() as INotifier<IMergeRequestListLoaderListener>).RemoveListener(this);
         (_updateManager.GetNotifier() as INotifier<IVersionLoaderListener>).RemoveListener(this);

         _updateManager?.Dispose();
         _updateManager = null;
      }

      public INotifier<IMergeRequestEventListener> GetNotifier() => _updateManager.GetNotifier();

      public IEnumerable<ProjectKey> GetProjects()
      {
         return _cache?.Details.GetProjects();
      }

      public IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey)
      {
         return _cache?.Details.GetMergeRequests(projectKey);
      }

      public MergeRequest? GetMergeRequest(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequest> mergeRequests = GetMergeRequests(mrk.ProjectKey);
         MergeRequest result = mergeRequests.FirstOrDefault(x => x.IId == mrk.IId);
         return result.Id == default(MergeRequest).Id ? new MergeRequest?() : result;
      }

      public IProjectUpdateContextProvider GetLocalBasedContextProvider(ProjectKey projectKey)
      {
         return new LocalBasedContextProvider(getAllVersions(projectKey));
      }

      public IProjectUpdateContextProvider GetRemoteBasedContextProvider(MergeRequestKey mrk)
      {
         WorkflowDataOperator tempOperator = new WorkflowDataOperator(
            mrk.ProjectKey.HostName, _clientContext.HostProperties.GetAccessToken(mrk.ProjectKey.HostName));
         return new RemoteBasedContextProvider(getAllVersions(mrk.ProjectKey), mrk, tempOperator);
      }

      public Version GetLatestVersion(MergeRequestKey mrk)
      {
         return _cache?.Details.GetVersions(mrk).OrderBy(x => x.Created_At).LastOrDefault() ?? default(Version);
      }

      public Version GetLatestVersion(ProjectKey projectKey)
      {
         return getAllVersions(projectKey).OrderBy(x => x.Created_At).LastOrDefault();
      }

      private IEnumerable<Version> getAllVersions(ProjectKey projectKey)
      {
         List<Version> versions = new List<Version>();
         if (_cache != null)
         {
            foreach (MergeRequest mergeRequest in _cache.Details.GetMergeRequests(projectKey))
            {
               MergeRequestKey mrk = new MergeRequestKey
               {
                  ProjectKey = projectKey,
                  IId = mergeRequest.IId
               };
               foreach (Version version in _cache.Details.GetVersions(mrk))
               {
                  versions.Add(version);
               }
            }
         }
         return versions;
      }

      /// <summary>
      /// Request to update the specified MR after the specified time periods (in milliseconds)
      /// </summary>
      public void CheckForUpdates(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         _updateManager?.RequestOneShotUpdate(mrk, intervals, onUpdateFinished);
      }

      public void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoader,
         ILoader<IVersionLoaderListener> versionLoader)
      {
         Trace.TraceInformation(String.Format( "[MergeRequestCache] Connecting to {0}", hostname));

         _cache = new WorkflowDetailsCache();
         unsubscribe();

         _mergeRequestListLoaderNotifier = mergeRequestListLoader.GetNotifier();
         _mergeRequestListLoaderNotifier.AddListener(this);

         _versionLoaderNotifier = versionLoader.GetNotifier();
         _versionLoaderNotifier.AddListener(this);
      }

      public void OnPostLoadProjectMergeRequests(ProjectKey projectKey, IEnumerable<MergeRequest> mergeRequests)
      {
         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Loaded {0} merge requests for project {1} at hostname {2}",
            mergeRequests.Count(), projectKey.ProjectName, projectKey.HostName));

         _cache?.UpdateMergeRequests(projectKey, mergeRequests);
      }

      public void OnPostLoadVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Loaded {0} versions of merge request with IId {1} for project {2} at hostname {3}",
            versions.Count(), mrk.IId, mrk.ProjectKey.ProjectName, mrk.ProjectKey.HostName));

         _cache?.UpdateVersions(mrk, versions);
      }

      public void OnPostLoadMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         _cache.UpdateMergeRequest(mrk, mergeRequest);
      }

      public void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade)
      {
         Trace.TraceInformation(String.Format("[MergeRequestCache] Connected to {0}", hostname));

         Debug.Assert(_mergeRequestListLoaderNotifier != null);
         _mergeRequestListLoaderNotifier.RemoveListener(this);
         _mergeRequestListLoaderNotifier = null;

         Debug.Assert(_versionLoaderNotifier != null);
         _versionLoaderNotifier.RemoveListener(this);
         _versionLoaderNotifier = null;

         Debug.Assert(_updateManager == null);
         _updateManager = new UpdateManager(_clientContext, hostname, context, _cache);

         (_updateManager.GetNotifier() as INotifier<IMergeRequestLoaderListener>).AddListener(this);
         (_updateManager.GetNotifier() as INotifier<IMergeRequestListLoaderListener>).AddListener(this);
         (_updateManager.GetNotifier() as INotifier<IVersionLoaderListener>).AddListener(this);
      }

      public void OnPreLoadProjectMergeRequests(ProjectKey project) { }
      public void OnFailedLoadProjectMergeRequests(ProjectKey project) { }

      public void OnPreLoadComparableEntities(MergeRequestKey mrk) { }
      public void OnPostLoadComparableEntities(MergeRequestKey mrk, IEnumerable commits) { }
      public void OnFailedLoadComparableEntities(MergeRequestKey mrk) { }

      public void OnPreLoadVersions(MergeRequestKey mrk) { }
      public void OnFailedLoadVersions(MergeRequestKey mrk) { }

      public void OnPreLoadMergeRequest(MergeRequestKey mrk) { }
      public void OnFailedLoadMergeRequest(MergeRequestKey mrk) { }

      private INotifier<IMergeRequestListLoaderListener> _mergeRequestListLoaderNotifier;
      private INotifier<IVersionLoaderListener> _versionLoaderNotifier;
      private readonly INotifier<IWorkflowEventListener> _workflowLoaderNotifier;

      private WorkflowDetailsCache _cache = new WorkflowDetailsCache();
      private UpdateManager _updateManager;
      private readonly GitLabClientContext _clientContext;
   }
}

