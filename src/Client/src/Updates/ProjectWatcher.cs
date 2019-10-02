using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
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

      private static int GetId(MergeRequest x) => x.Id;
      private static int GetId(UpdatedMergeRequest x) => GetId(x.MergeRequest);

      private static int GetIId(MergeRequest x) => x.IId;
      private static int GetIId(UpdatedMergeRequest x) => GetIId(x.MergeRequest);

      private static int GetProjectId(MergeRequest x) => x.Project_Id;
      private static int GetProjectId(UpdatedMergeRequest x) => GetProjectId(x.MergeRequest);

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates<T>(List<T> mergeRequests, string hostname,
         IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         // Check all the updated merge request to figure out the latest change among them
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (T mergeRequest in mergeRequests)
         {
            int mergeRequestId = GetId((dynamic)mergeRequest);
            int mergeRequestIId = GetIId((dynamic)mergeRequest);
            int projectId = GetProjectId((dynamic)mergeRequest);

            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectId = projectId };
            string projectName = details.GetProjectName(key);

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == projectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            MergeRequestKey mrk = new MergeRequestKey { ProjectKey = key, IId = mergeRequestIId };

            updateTimestamp = details.GetLatestChangeTimestamp(mrk) > updateTimestamp ?
               details.GetLatestChangeTimestamp(mrk) : updateTimestamp;

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



