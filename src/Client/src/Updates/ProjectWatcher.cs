using System;
using System.ComponentModel;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Converts MergeRequestUpdates into ProjectUpdates and notifies subscribers
   /// </summary>
   internal class ProjectWatcher : IProjectWatcher
   {
      internal ProjectWatcher(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      public event Action<List<ProjectUpdate>> OnProjectUpdate;

      /// <summary>
      /// Convert passed updates to ProjectUpdates and notify subscribers
      /// </summary>
      internal void ProcessUpdates(string MergeRequestUpdates updates, string hostname, WorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();
         projectUpdates.AddRange(getProjectUpdates(updates.NewMergeRequests, hostname, details));
         projectUpdates.AddRange(getProjectUpdates(updates.UpdatedMergeRequests, hostname, details));

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
      private List<ProjectUpdate> getProjectUpdates(List<MergeRequest> mergeRequests, string hostname,
         WorkflowDetails details)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         DateTime latestChange = DateTime.MinValue;
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            string projectName = details.GetProjectName(mergeRequest.Project.Id);

            // Excluding duplicates
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == projectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            latestChange = details.GetLatestCommitAsync(mergeRequest.Id) > latestChange ?
               details.GetLatestCommitAsync(mergeRequest.Id) : latestChange;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = hostname,
                  ProjectName = projectName,
                  LatestChange = latestChange
               });
         }

         return projectUpdates;
      }

      private UserDefinedSettings Settings { get; }
   }
}



