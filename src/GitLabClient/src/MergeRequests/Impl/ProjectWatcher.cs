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
         ProjectUpdate allProjectSnapshots = new ProjectUpdate();

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
            if (!allProjectSnapshots.Any(x => x.Equals(projectKey)))
            {
               allProjectSnapshots.Add(projectKey, new ProjectSnapshot());
            }

            ProjectSnapshot singleProjectSnapshot = allProjectSnapshots[update.FullMergeRequestKey.ProjectKey];

            DateTime versionsTimestamp = update.NewVersions.OrderBy(x => x.Created_At).Last().Created_At;
            singleProjectSnapshot.LatestChange = versionsTimestamp > singleProjectSnapshot.LatestChange
               ? versionsTimestamp : singleProjectSnapshot.LatestChange;

            foreach (GitLabSharp.Entities.Version version in update.NewVersions)
            {
               singleProjectSnapshot.Sha.Add(version.Head_Commit_SHA);
               singleProjectSnapshot.Sha.Add(version.Base_Commit_SHA);
            }
         }

         return allProjectSnapshots;
      }
   }
}

