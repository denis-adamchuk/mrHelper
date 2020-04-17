using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Converts MergeRequestUpdates into ProjectUpdates and notifies subscribers
   /// </summary>
   internal class ProjectWatcher : IProjectWatcher
   {
      public event Action<ProjectUpdate> OnProjectUpdate;

      /// <summary>
      /// Convert passed updates to ProjectUpdates and notify subscribers
      /// </summary>
      internal void ProcessUpdates(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         ProjectUpdate projectUpdates = getProjectUpdates(updates);

         if (projectUpdates.Count() > 0)
         {
            foreach (KeyValuePair<ProjectKey, ProjectSnapshot> projectUpdate in projectUpdates)
            {
               Trace.TraceInformation(
                  String.Format("[ProjectWatcher] Updating project: Host {0}, Name {1}, TimeStamp {2}",
                     projectUpdate.Key.HostName, projectUpdate.Key.ProjectName,
                     projectUpdate.Value.LatestChange.ToLocalTime().ToString()));
            }
            OnProjectUpdate?.Invoke(projectUpdates);
         }
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private ProjectUpdate getProjectUpdates(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         ProjectUpdate projectUpdates = new ProjectUpdate();

         // Check all the updated merge request to figure out the latest change among them
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (UserEvents.MergeRequestEvent update in updates)
         {
            bool mayCauseProjectChange = update.New || update.Commits;
            if (!mayCauseProjectChange)
            {
               continue;
            }

            if (update.NewVersions == null || !update.NewVersions.Any())
            {
               Debug.Assert(false);
               continue;
            }

            ProjectKey projectKey = update.FullMergeRequestKey.ProjectKey;
            MergeRequestKey mrk = new MergeRequestKey
            {
               ProjectKey = projectKey,
               IId = update.FullMergeRequestKey.MergeRequest.IId
            };

            // Excluding duplicates
            if (!projectUpdates.Any(x => x.Equals(projectKey)))
            {
               projectUpdates.Add(projectKey, new ProjectSnapshot());
            }

            ProjectSnapshot projectUpdated = projectUpdates[update.FullMergeRequestKey.ProjectKey];
            foreach (ProjectSnapshot projectUpdate in projectUpdates.Values)
            {
               DateTime versionsTimestamp = update.NewVersions.OrderBy(x => x.Created_At).Last().Created_At;
               updateTimestamp = versionsTimestamp > updateTimestamp ? versionsTimestamp : updateTimestamp;
               projectUpdate.LatestChange = updateTimestamp;

               foreach (GitLabSharp.Entities.Version version in update.NewVersions)
               {
                  projectUpdate.Sha.Add(version.Head_Commit_SHA);
                  projectUpdate.Sha.Add(version.Base_Commit_SHA);
               }
            }
         }

         return projectUpdates;
      }
   }
}

