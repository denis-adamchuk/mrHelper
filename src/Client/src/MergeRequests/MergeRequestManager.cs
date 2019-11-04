using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;

namespace mrHelper.Client.MergeRequests
{
   public class MergeRequestManager : IDisposable
   {
      public event Action<Common.UserEvents.MergeRequestEvent> OnEvent;

      public MergeRequestManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke, UserDefinedSettings settings)
      {
         _settings = settings;

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
               _updateManager = new UpdateManager(synchronizeInvoke, settings, _hostname, projects, _cache);
               _updateManager.OnUpdate += onUpdate;

               Trace.TraceInformation(String.Format(
                  "[MergeRequestManager] Set hostname for updates to {0}, will trace updates in {1} projects",
                  hostname, projects.Count));
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

      public MergeRequest GetMergeRequest(MergeRequestKey mrk)
      {
         return _cache.Details.GetMergeRequests(mrk.ProjectKey).Find(x => x.IId == mrk.IId);
      }

      public IUpdateManager GetUpdateManager()
      {
         return _updateManager;
      }

      private void onUpdate(List<UpdatedMergeRequest> updates)
      {
         foreach (UpdatedMergeRequest mergeRequest in updates)
         {
            UserEvents.MergeRequestEvent.Type type;
            switch (mergeRequest.UpdateKind)
            {
               case UpdateKind.New:
                  type = UserEvents.MergeRequestEvent.Type.NewMergeRequest;
                  break;

               case UpdateKind.Closed:
                  type = UserEvents.MergeRequestEvent.Type.ClosedMergeRequest;
                  break;

               case UpdateKind.LabelsUpdated:
               case UpdateKind.CommitsUpdated:
               case UpdateKind.CommitsAndLabelsUpdated:
                  type = UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest;
                  break;

               default:
                  Debug.Assert(false);
                  return;
            }

            OnEvent?.Invoke(new UserEvents.MergeRequestEvent
            {
               MergeRequestKey = mergeRequest.MergeRequestKey,
               EventType = type,
               Details = null
            });
         }
      }

      private string _hostname;
      private WorkflowDetailsCache _cache = new WorkflowDetailsCache();
      private readonly ProjectWatcher _projectWatcher = new ProjectWatcher();
      private UpdateManager _updateManager;
      private readonly UserDefinedSettings _settings;
   }
}

