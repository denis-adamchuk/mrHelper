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

      private static int GetId(NewOrClosedMergeRequest x) => x.MergeRequest.Id;
      private static int GetId(UpdatedMergeRequest x) => x.MergeRequest.Id;

      private static int GetIId(NewOrClosedMergeRequest x) => x.MergeRequest.IId;
      private static int GetIId(UpdatedMergeRequest x) => x.MergeRequest.IId;

      private static Project GetProject(NewOrClosedMergeRequest x) => x.Project;
      private static Project GetProject(UpdatedMergeRequest x) => x.Project;

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
            Project project = GetProject((dynamic)mergeRequest);

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == project.Path_With_Namespace)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            MergeRequestKey mrk = new MergeRequestKey(hostname, project.Path_With_Namespace, mergeRequestIId);

            updateTimestamp = details.GetLatestChangeTimestamp(mrk) > updateTimestamp ?
               details.GetLatestChangeTimestamp(mrk) : updateTimestamp;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = hostname,
                  ProjectName = project.Path_With_Namespace,
                  Timestamp = updateTimestamp
               });
         }

         return projectUpdates;
      }
   }
}



