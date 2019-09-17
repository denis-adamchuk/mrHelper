using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Types;

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
      internal void ProcessUpdates(MergeRequestUpdates updates, string hostname, IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();
         projectUpdates.AddRange(getProjectUpdates(updates.NewMergeRequests, hostname, details));
         projectUpdates.AddRange(getProjectUpdates(updates.UpdatedMergeRequests, hostname, details));

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
      private List<ProjectUpdate> getProjectUpdates(List<MergeRequest> mergeRequests, string hostname,
         IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         // Check all the updated merge request to figure out the latest change among them
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectId = mergeRequest.Project_Id };
            string projectName = details.GetProjectName(key);

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == projectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            updateTimestamp = details.GetLatestChangeTimestamp(mergeRequest.Id) > updateTimestamp ?
               details.GetLatestChangeTimestamp(mergeRequest.Id) : updateTimestamp;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = hostname,
                  ProjectName = projectName,
                  Timestamp = updateTimestamp
               });
         }

         return projectUpdates;
      }
   }
}



