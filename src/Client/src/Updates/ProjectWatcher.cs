using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Converts MergeRequestUpdates into ProjectUpdates and notifies subscribers
   /// </summary>
   internal class ProjectWatcher : IProjectWatcher
   {
      public event Action<List<ProjectUpdate>> OnProjectUpdate;

      /// <summary>
      /// Convert passed updates to ProjectUpdates and notify subscribers
      /// </summary>
      internal void ProcessUpdates(List<UpdatedMergeRequest> updates, string hostname, IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = getProjectUpdates(updates, hostname, details);

         if (projectUpdates.Count > 0)
         {
            foreach (ProjectUpdate projectUpdate in projectUpdates)
            {
               Trace.TraceInformation(
                  String.Format("[ProjectWatcher] Updating project: Host {0}, Name {1}, TimeStamp {2}",
                     projectUpdate.HostName, projectUpdate.ProjectName,
                     projectUpdate.Timestamp.ToLocalTime().ToString()));
            }
            OnProjectUpdate?.Invoke(projectUpdates);
         }
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates(List<UpdatedMergeRequest> mergeRequests, string hostname,
         IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         // Check all the updated merge request to figure out the latest change among them
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (UpdatedMergeRequest mergeRequest in mergeRequests)
         {
            bool mayCauseProjectChange = mergeRequest.UpdateKind == UpdateKind.New
                                      || mergeRequest.UpdateKind == UpdateKind.CommitsUpdated;
            if (!mayCauseProjectChange)
            {
               continue;
            }

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == mergeRequest.Project.Path_With_Namespace)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            MergeRequestKey mrk = new MergeRequestKey(hostname,
               mergeRequest.Project.Path_With_Namespace, mergeRequest.MergeRequest.IId);

            updateTimestamp = details.GetLatestChangeTimestamp(mrk) > updateTimestamp ?
               details.GetLatestChangeTimestamp(mrk) : updateTimestamp;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = hostname,
                  ProjectName = mergeRequest.Project.Path_With_Namespace,
                  Timestamp = updateTimestamp
               });
         }

         return projectUpdates;
      }
   }
}



