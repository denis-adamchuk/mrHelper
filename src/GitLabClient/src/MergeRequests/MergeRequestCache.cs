using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   public class MergeRequestCache : IDisposable, ICachedMergeRequestProvider, IProjectCheckerFactory
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
         _workflowEventNotifier.LoadedMergeRequestVersion += onLoadedMergeRequestVersion;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;
         _workflowEventNotifier.LoadedMergeRequests -= onLoadedMergeRequests;
         _workflowEventNotifier.LoadedMergeRequestVersion -= onLoadedMergeRequestVersion;

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

      public IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk)
      {
         return _cache != null ? new LocalProjectChecker(mrk, _cache.Details.Clone()) : null;
      }

      public IInstantProjectChecker GetLocalProjectChecker(ProjectKey projectKey)
      {
         return GetLocalProjectChecker(getLatestMergeRequest(projectKey));
      }

      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk)
      {
         return new RemoteProjectChecker(mrk, _updateOperator);
      }

      public Version GetLatestVersion(MergeRequestKey mrk)
      {
         return _cache?.Details.GetLatestVersion(mrk) ?? default(Version);
      }

      public IProjectCheckerFactory GetProjectCheckerFactory()
      {
         return this;
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return _projectWatcher;
      }

      private MergeRequestKey getLatestMergeRequest(ProjectKey projectKey)
      {
         if (_cache == null)
         {
            return default(MergeRequestKey);
         }

         return _cache.Details.GetMergeRequests(projectKey).
            Select(x => new MergeRequestKey
            {
               ProjectKey = projectKey,
               IId = x.IId
            }).OrderByDescending(x => _cache.Details.GetLatestVersion(x).Created_At).FirstOrDefault();
      }

      /// <summary>
      /// Request to update the specified MR after the specified time periods (in milliseconds)
      /// </summary>
      public void CheckForUpdates(MergeRequestKey mrk, int[] intervals)
      {
         _updateManager.RequestOneShotUpdate(mrk, intervals);
      }

      private void onUpdate(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         _projectWatcher.ProcessUpdates(updates, _cache.Details);

         foreach (UserEvents.MergeRequestEvent update in updates)
         {
            MergeRequestEvent?.Invoke(update);
         }
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         _hostname = hostname;
         _cache = new WorkflowDetailsCache();

         if (_updateManager != null)
         {
            _updateManager.OnUpdate -= onUpdate;
            _updateManager.Dispose();
         }

         _updateManager = new UpdateManager(_synchronizeInvoke, _updateOperator, _hostname, projects, _cache,
            _autoUpdatePeriodMs);
         _updateManager.OnUpdate += onUpdate;

         Trace.TraceInformation(String.Format(
            "[MergeRequestCache] Set hostname for updates to {0}, will trace updates in {1} projects",
            hostname, projects.Count()));
      }

      private void onLoadedMergeRequests(string hostname, Project project, IEnumerable<MergeRequest> mergeRequests)
      {
         _cache?.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);
      }

      private void onLoadedMergeRequestVersion(string hostname, string projectname,
         MergeRequest mergeRequest, Version version)
      {
         _cache?.UpdateLatestVersion(new MergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            IId = mergeRequest.IId
         }, version);
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

