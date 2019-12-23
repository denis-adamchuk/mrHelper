using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;

namespace mrHelper.Client.MergeRequests
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
      internal void ProcessUpdates(List<UserEvents.MergeRequestEvent> updates, IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = getProjectUpdates(updates, details);

         if (projectUpdates.Count > 0)
         {
            foreach (ProjectUpdate projectUpdate in projectUpdates)
            {
               Trace.TraceInformation(
                  String.Format("[ProjectWatcher] Updating project: Host {0}, Name {1}, TimeStamp {2}",
                     projectUpdate.ProjectKey.HostName, projectUpdate.ProjectKey.ProjectName,
                     projectUpdate.Timestamp.ToLocalTime().ToString()));
            }
            OnProjectUpdate?.Invoke(projectUpdates);
         }
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates(List<UserEvents.MergeRequestEvent> updates,
         IWorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         // Check all the updated merge request to figure out the latest change among them
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (UserEvents.MergeRequestEvent update in updates)
         {
            bool mayCauseProjectChange = update.New || update.Commits;
            if (!mayCauseProjectChange)
            {
               continue;
            }

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectKey.ProjectName == update.FullMergeRequestKey.ProjectKey.ProjectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            MergeRequestKey mrk = new MergeRequestKey
            {
               ProjectKey = update.FullMergeRequestKey.ProjectKey,
               IId = update.FullMergeRequestKey.MergeRequest.IId
            };

            updateTimestamp = details.GetLatestChangeTimestamp(mrk) > updateTimestamp ?
               details.GetLatestChangeTimestamp(mrk) : updateTimestamp;

            projectUpdates.Add(new ProjectUpdate { ProjectKey = mrk.ProjectKey, Timestamp = updateTimestamp });
         }

         return projectUpdates;
      }
   }
}

