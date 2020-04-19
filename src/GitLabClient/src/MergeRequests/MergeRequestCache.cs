using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   public class MergeRequestCache : IDisposable, ICachedMergeRequestProvider
   {
      public event Action<Common.UserEvents.MergeRequestEvent> MergeRequestEvent;

      public MergeRequestCache(IWorkflowEventNotifier workflowEventNotifier, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties settings, int autoUpdatePeriodMs)
      {
         _synchronizeInvoke = synchronizeInvoke;
         _autoUpdatePeriodMs = autoUpdatePeriodMs;

         _updateOperator = new UpdateOperator(settings);

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;
         _workflowEventNotifier.LoadedMergeRequests += onLoadedMergeRequests;
         _workflowEventNotifier.LoadMergeRequestVersions += onLoadedMergeRequestVersions;
         _workflowEventNotifier.LoadedProjects += onLoadedProjects;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;
         _workflowEventNotifier.LoadedMergeRequests -= onLoadedMergeRequests;
         _workflowEventNotifier.LoadMergeRequestVersions -= onLoadedMergeRequestVersions;
         _workflowEventNotifier.LoadedProjects -= onLoadedProjects;

         if (_updateManager != null)
         {
            _updateManager.OnUpdate -= onUpdate;
            _updateManager.Dispose();
         }
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

      public IProjectUpdateFactory GetLocalProjectChecker(MergeRequestKey mrk)
      {
         return _cache != null ? new LocalVersionBasedUpdateFactory(getAllVersions(mrk.ProjectKey)) : null;
      }

      public IProjectUpdateFactory GetLocalProjectChecker(ProjectKey projectKey)
      {
         return _cache != null ? new LocalVersionBasedUpdateFactory(getAllVersions(projectKey)) : null;
      }

      public IProjectUpdateFactory GetRemoteProjectChecker(MergeRequestKey mrk)
      {
         return new RemoteVersionBasedUpdateFactory(getAllVersions(mrk.ProjectKey), mrk, _updateOperator);
      }

      public Version GetLatestVersion(MergeRequestKey mrk)
      {
         return _cache?.Details.GetVersions(mrk).LastOrDefault() ?? default(Version);
      }

      public Version GetLatestVersion(ProjectKey projectKey)
      {
         return getAllVersions(projectKey).OrderBy(x => x.Created_At).LastOrDefault();
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return _projectWatcher;
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
         _updateManager.RequestOneShotUpdate(mrk, intervals, onUpdateFinished);
      }

      private void onUpdate(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         _projectWatcher.ProcessUpdates(updates);

         foreach (UserEvents.MergeRequestEvent update in updates)
         {
            MergeRequestEvent?.Invoke(update);
         }
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         Trace.TraceInformation(String.Format( "[MergeRequestCache] Connected to {0}", hostname));

         _hostname = hostname;
         _cache = new WorkflowDetailsCache();

         if (_updateManager != null)
         {
            _updateManager.OnUpdate -= onUpdate;
            _updateManager.Dispose();
            _updateManager = null;
         }
      }

      private void onLoadedMergeRequests(string hostname, Project project, IEnumerable<MergeRequest> mergeRequests)
      {
         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Loaded {0} merge requests for project {1} at hostname {2}",
            mergeRequests.Count(), project.Path_With_Namespace, hostname));

         _cache?.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);
      }

      private void onLoadedMergeRequestVersions(string hostname, string projectname,
         MergeRequest mergeRequest, IEnumerable<Version> versions)
      {
         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Loaded {0} version of merge request with IId {1} for project {2} at hostname {3}",
            versions.Count(), mergeRequest.IId, projectname, hostname));

         _cache?.UpdateVersions(new MergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            IId = mergeRequest.IId
         }, versions);
      }

      private void onLoadedProjects(string hostname, IEnumerable<Project> projects)
      {
         Debug.Assert(_updateManager == null);

         _updateManager = new UpdateManager(_synchronizeInvoke, _updateOperator, _hostname, projects, _cache,
            _autoUpdatePeriodMs);
         _updateManager.OnUpdate += onUpdate;

         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Set hostname for updates to {0}, will trace updates in {1} projects",
            hostname, projects.Count()));
      }

      private string _hostname;
      private WorkflowDetailsCache _cache = new WorkflowDetailsCache();
      private readonly ProjectWatcher _projectWatcher = new ProjectWatcher();
      private UpdateManager _updateManager;
      private readonly UpdateOperator _updateOperator;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly int _autoUpdatePeriodMs;
   }
}

