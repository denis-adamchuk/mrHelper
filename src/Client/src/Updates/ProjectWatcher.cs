using System;
using System.ComponentModel;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Updates
{
   internal class ProjectWatcher : IProjectWatcher
   {
      internal ProjectWatcher(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      public event Action<List<ProjectUpdate>> OnProjectUpdate;

      internal void ProcessUpdates(MergeRequestUpdates updates)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();
         projectUpdates.AddRange(getProjectUpdates(state.HostName, updates.NewMergeRequests));
         projectUpdates.AddRange(getProjectUpdates(state.HostName, updates.UpdatedMergeRequests));

         Debug.WriteLine(String.Format("[ProjectWatcher] Updating {0} projects", projectUpdates.Count));
         if (projectUpdates.Count > 0)
         {
            foreach (ProjectUpdate projectUpdate in projectUpdates)
            {
               Trace.TraceInformation(String.Format(
                  "[ProjectWatcher] Updating project: Host {0}, Name {1}, TimeStamp {2}",
                  projectUpdate.HostName, projectUpdate.ProjectName,
                  projectUpdate.LatestChange.ToLocalTime().ToString()));
            }
            OnProjectUpdate?.Invoke(projectUpdates);
         }
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates(string hostname, List<MergeRequest> mergeRequests)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         DateTime latestChange = DateTime.MinValue;
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            string projectName = getMergeRequestProjectName(mergeRequest);
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == projectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            latestChange = _cachedCommits[mergeRequest.Id] > latestChange ?
               _cachedCommits[mergeRequest.Id] : latestChange;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = hostname,
                  ProjectName = getMergeRequestProjectName(mergeRequest),
                  LatestChange = latestChange
               });
         }

         return projectUpdates;
      }

      private UserDefinedSettings Settings { get; }
   }
}



