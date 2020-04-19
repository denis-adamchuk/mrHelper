using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Converts MergeRequestUpdates into ProjectUpdates and notifies subscribers
   /// </summary>
   internal class ProjectWatcher : IProjectWatcher
   {
      public event Action<ProjectWatcherUpdateArgs> OnProjectUpdate;

      /// <summary>
      /// Convert passed updates to ProjectUpdates and notify subscribers
      /// </summary>
      internal void ProcessUpdates(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         Dictionary<ProjectKey, FullProjectUpdate> projectUpdates = getProjectUpdates(updates);

         if (projectUpdates.Count() > 0)
         {
            foreach (KeyValuePair<ProjectKey, FullProjectUpdate> projectUpdate in projectUpdates)
            {
               Trace.TraceInformation(
                  String.Format("[ProjectWatcher] Updating project: Host {0}, Name {1}, TimeStamp {2}",
                     projectUpdate.Key.HostName, projectUpdate.Key.ProjectName,
                     projectUpdate.Value.LatestChange.ToLocalTime().ToString()));
               OnProjectUpdate?.Invoke(new ProjectWatcherUpdateArgs
               {
                  ProjectKey = projectUpdate.Key,
                  ProjectUpdate = projectUpdate.Value
               });
            }
         }
      }

      private class FullProjectUpdateInternal
      {
         public DateTime LatestChange;
         public List<string> Sha;
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private Dictionary<ProjectKey, FullProjectUpdate> getProjectUpdates(
         IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         Dictionary<ProjectKey, FullProjectUpdateInternal> resultInternal =
            new Dictionary<ProjectKey, FullProjectUpdateInternal>();

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
            if (!resultInternal.Any(x => x.Equals(projectKey)))
            {
               resultInternal.Add(projectKey, new FullProjectUpdateInternal
               {
                  LatestChange = DateTime.MinValue,
                  Sha = new List<string>()
               });
            }

            FullProjectUpdateInternal projectUpdate = resultInternal[projectKey];

            DateTime versionsTimestamp = update.NewVersions.OrderBy(x => x.Created_At).Last().Created_At;
            projectUpdate.LatestChange = versionsTimestamp > projectUpdate.LatestChange ?
               versionsTimestamp : projectUpdate.LatestChange ;

            foreach (GitLabSharp.Entities.Version version in update.NewVersions)
            {
               projectUpdate.Sha.Add(version.Head_Commit_SHA);
               projectUpdate.Sha.Add(version.Base_Commit_SHA);
            }
         }

         return resultInternal.ToDictionary(
            x => x.Key,
            x => new FullProjectUpdate
            {
               LatestChange = x.Value.LatestChange,
               Sha = x.Value.Sha
            });
      }
   }
}

