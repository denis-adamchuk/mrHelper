using System;
using System.Collections.Generic;

namespace mrHelper.Client
{
   internal class WorkflowDataOperatorException : Exception {}

   internal class WorkflowDataOperator : IDisposable
   {
      internal WorkflowDataOperator(string host, string token)
      {
         Client = new GitLabClient(host, token);
      }

      async internal Task<List<Project>> GetProjectsAsync()
      {
         List<Project> projects = Tools.LoadProjectsFromFile();
         if (projects != null && projects.Count != 0)
         {
            Client.CancelAsync();
            Debug.WriteLine("Project list is read from file");
            return projects;
         }

         Debug.WriteLine("Loading projects asynchronously for host " + hostName);

         try
         {
            return await Client.RunAsync(async (gl) =>
            {
               return await gl.Projects.LoadAllTaskAsync(new ProjectsFilter{ PublicOnly = Settings.ShowPublicOnly });
            });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab", false);
            throw new WorkflowDataOperatorException();
         }
         return null;
      }

      async internal Task<List<MergeRequest>> GetMergeRequestsAsync(string hostName, string projectName)
      {
         Debug.WriteLine("Loading project merge requests asynchronously for host "
            + State.HostName + " and project " + State.Project);

         List<MergeRequest> mergeRequests = null;
         try
         {
            mergeRequests = Client.RunAsync(async (gl) =>
            {
               mergeRequests = await gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.LoadAllTaskAsync(
                  new MergeRequestsFilter()));
            });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab", false);
            throw new WorkflowDataOperatorException();
         }

         if (mergeRequests != null)
         {
            for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --i)
            {
               if (Settings.CheckedLabelsFilter && !_cachedLabels.Intersect(mergeRequest.Labels))
               {
                  mergeRequests.RemoveAt(iMergeRequest);
               }
            }
         }

         return mergeRequests;
      }

      async internal Task<MergeRequest?> GetMergeRequestAsync(int iid)
      {
         Debug.WriteLine("Loading merge request asynchronously");

         try
         {
            return await Client.RunAsync(async (gl) =>
            {
               return await gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(iid).LoadTaskAsync(ct));
            });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab", false);
            throw new WorkflowDataOperatorException();
         }
         return null;
      }

      async internal Task<List<Version>> GetVersionsAsync()
      {
         Debug.WriteLine("Loading versions asynchronously");

         try
         {
            return await Client.RunAsync(async (gl) =>
            {
               return await gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(State.MergeRequest.IId).
                  Versions.LoadAllTaskAsync(ct));
            });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request versions from GitLab", false);
            throw new WorkflowDataOperatorException();
         }
         return null;
      }

      async public void CancelAsync()
      {
         await Client?.CancelAsync();
      }

      internal void Dispose()
      {
         Client?.Dispose();
      }

      private GitLabClient Client { get; }
   }
}

