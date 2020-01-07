using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   public class MergeRequestManager : IDisposable, IMergeRequestProvider
   {
      public event Action<Common.UserEvents.MergeRequestEvent> MergeRequestEvent;

      public MergeRequestManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties settings, int autoUpdatePeriodMs)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            // TODO Current version supports updates of projects of the most recent loaded host only
            if (String.IsNullOrEmpty(_hostname) || _hostname != hostname)
            {
               _hostname = hostname;
               if (_updateManager != null)
               {
                  _updateManager.OnUpdate -= onUpdate;
                  _updateManager.Dispose();
               }

               _cache = new WorkflowDetailsCache();
               _updateManager = new UpdateManager(synchronizeInvoke, settings, _hostname, projects, _cache,
                  autoUpdatePeriodMs);
               _updateManager.OnUpdate += onUpdate;

               Trace.TraceInformation(String.Format(
                  "[MergeRequestManager] Set hostname for updates to {0}, will trace updates in {1} projects",
                  hostname, projects.Count()));
            }
         };

         workflow.PostLoadProjectMergeRequests += (hostname, project, mergeRequests) =>
            _cache.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);

         workflow.PostLoadLatestVersion += (hostname, projectname, mergeRequest, version) =>
            _cache.UpdateLatestVersion(new MergeRequestKey
            {
               ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
               IId = mergeRequest.IId
            }, version);
      }

      public void Dispose()
      {
         _updateManager?.Dispose();
      }

      public IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey)
      {
         return _cache.Details.GetMergeRequests(projectKey);
      }

      public MergeRequest? GetMergeRequest(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequest> mergeRequests = GetMergeRequests(mrk.ProjectKey);
         MergeRequest result = mergeRequests.FirstOrDefault(x => x.IId == mrk.IId);
         return result.Id == default(MergeRequest).Id ? new MergeRequest?() : result;
      }

      public IUpdateManager GetUpdateManager()
      {
         return _updateManager;
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return _projectWatcher;
      }

      /// <summary>
      /// Request to update the specified MR after the specified time period (in milliseconds)
      /// </summary>
      public void CheckForUpdates(MergeRequestKey mrk, int firstChanceDelay, int secondChanceDelay)
      {
         _updateManager.RequestOneShotUpdate(mrk, firstChanceDelay, secondChanceDelay);
      }

      private void onUpdate(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         _projectWatcher.ProcessUpdates(updates, _cache.Details);

         foreach (UserEvents.MergeRequestEvent update in updates)
         {
            MergeRequestEvent?.Invoke(update);
         }
      }

      private string _hostname;
      private WorkflowDetailsCache _cache = new WorkflowDetailsCache();
      private readonly ProjectWatcher _projectWatcher = new ProjectWatcher();
      private UpdateManager _updateManager;
   }
}

