using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   /// <summary>
   /// Implements Workflow-related interaction with GitLab
   /// </summary>
   internal class WorkflowDataOperator : IDisposable
   {
      internal WorkflowDataOperator(string host, string token, UserDefinedSettings settings)
      {
         Client = new GitLabClient(host, token);
         Settings = settings;
      }

      public void Dispose()
      {
         Client.Dispose();
      }

      async internal Task<User> GetCurrentUser()
      {
         try
         {
            return (User)(await Client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync() ));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load current user from GitLab");
            throw new OperatorException(ex);
         }
      }

      async internal Task<List<Project>> GetProjectsAsync(string hostName, bool publicOnly)
      {
         List<Project> projects = Tools.Tools.LoadProjectsFromFile(hostName);
         if (projects != null && projects.Count != 0)
         {
            await Client.CancelAsync();
            Debug.WriteLine("Project list is read from file");
            return projects;
         }

         Debug.WriteLine("Loading projects asynchronously for host " + hostName);

         try
         {
            return (List<Project>(await Client.RunAsync(async (gl) =>
               await gl.Projects.LoadAllTaskAsync(new ProjectsFilter{ PublicOnly = publicOnly })));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab");
            throw new OperatorException(ex);
         }
      }

      async internal Task<List<MergeRequest>> GetMergeRequestsAsync(string projectName, bool filterLabels)
      {
         Debug.WriteLine("Loading project merge requests asynchronously for project " + projectName);

         List<MergeRequest> mergeRequests = null;
         try
         {
            mergeRequests = (List<MergeRequest>(await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.LoadAllTaskAsync(
                  new MergeRequestsFilter())));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
            throw new OperatorException(ex);
         }

         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            if (filterLabels && !_cachedLabels.Intersect(mergeRequest.Labels))
            {
               mergeRequests.RemoveAt(iMergeRequest);
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
               await gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(iid).LoadTaskAsync(ct));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
            throw new OperatorException(ex);
         }
      }

      async internal Task<List<GitLabSharp.Entities.Version>> GetVersionsAsync()
      {
         Debug.WriteLine("Loading versions asynchronously");

         try
         {
            return await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(State.MergeRequest.IId).
                  Versions.LoadAllTaskAsync(ct));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request versions from GitLab");
            throw new OperatorException(ex);
         }
      }

      public Task CancelAsync()
      {
         return Client.CancelAsync();
      }

      private GitLabClient Client { get; }
      private UserDefinedSettings Settings { get; }
   }
}

