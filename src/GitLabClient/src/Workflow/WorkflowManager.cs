using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Constants;

namespace mrHelper.Client.Workflow
{
   public class WorkflowManager : BaseWorkflowManager, IWorkflowEventNotifier, IMergeRequestListLoader
   {
      public event Action<string> Connecting;
      public event Action<string, User, IEnumerable<Project>> Connected;

      public event Action<Project> PreLoadProjectMergeRequests;
      public event Action<string, Project, IEnumerable<MergeRequest>> PostLoadProjectMergeRequests;
      public event Action FailedLoadProjectMergeRequests;

      public WorkflowManager(IHostProperties settings)
         : base(settings)
      {
      }

      async public Task<bool> LoadAllMergeRequestsAsync(string hostname, IEnumerable<Project> projects,
         Action<string, string> onForbiddenProject, Action<string, string> onNotFoundProject)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         User? currentUser = await loadCurrentUserAsync(hostname);
         if (!currentUser.HasValue)
         {
            return false;
         }

         Exception exception = null;
         bool cancelled = false;
         async Task loadVersionsLocal(Project project, MergeRequest mergeRequest)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               if (!await loadVersionsAsync(hostname, project.Path_With_Namespace, mergeRequest, false))
               {
                  cancelled = true;
               }
            }
            catch (WorkflowException ex)
            {
               exception = ex;
               cancelled = true;
            }
         }

         async Task loadProject(Project project)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               IEnumerable<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(hostname, project);
               if (mergeRequests == null)
               {
                  cancelled = true;
               }
               else
               {
                  await TaskUtils.RunConcurrentFunctionsAsync(mergeRequests, x => loadVersionsLocal(project, x),
                     Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, () => cancelled);
               }
            }
            catch (WorkflowException ex)
            {
               if (isForbiddenProjectException(ex))
               {
                  onForbiddenProject?.Invoke(hostname, project.Path_With_Namespace);
                  return;
               }
               else if (isNotFoundProjectException(ex))
               {
                  onNotFoundProject?.Invoke(hostname, project.Path_With_Namespace);
                  return;
               }
               exception = ex;
               cancelled = true;
            }
         }

         Connecting?.Invoke(hostname);
         await TaskUtils.RunConcurrentFunctionsAsync(projects, x => loadProject(x),
            Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, () => cancelled);
         if (!cancelled)
         {
            Connected?.Invoke(hostname, currentUser.Value, projects);
            return true;
         }

         if (exception != null)
         {
            throw exception;
         }
         return false;
      }

      async private Task<User?> loadCurrentUserAsync(string hostName)
      {
         try
         {
            return await _operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host \"{0}\"", hostName);
            string errorMessage = String.Format("Cannot load user from host \"{0}\"", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage, null);
         }
         return null;
      }

      async private Task<IEnumerable<MergeRequest>> loadProjectMergeRequestsAsync(string hostname, Project project)
      {
         PreLoadProjectMergeRequests?.Invoke(project);

         string projectName = project.Path_With_Namespace;

         IEnumerable<MergeRequest> mergeRequests;
         try
         {
            SearchByProject searchByProject = new SearchByProject { ProjectName = projectName };
            mergeRequests = await _operator.SearchMergeRequestsAsync(searchByProject, null, true);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading merge requests for project \"{0}\"", projectName);
            string errorMessage = String.Format("Cannot load project \"{0}\"", projectName);
            handleOperatorException(ex, cancelMessage, errorMessage, new Action[] { FailedLoadProjectMergeRequests });
            return null;
         }

         PostLoadProjectMergeRequests?.Invoke(hostname, project, mergeRequests);
         return mergeRequests;
      }

      private static bool isForbiddenProjectException(WorkflowException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.Forbidden : false;
      }

      private static bool isNotFoundProjectException(WorkflowException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.NotFound : false;
      }

      private static System.Net.HttpWebResponse getWebResponse(WorkflowException ex)
      {
         if (ex.InnerException?.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response;
            }
         }
         return null;
      }
   }
}

