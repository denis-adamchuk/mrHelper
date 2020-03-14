using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : ExceptionEx
   {
      internal WorkflowException(string message, Exception innerException)
         : base(message, innerException) {}

      public string UserMessage
      {
         get
         {
            if (InnerException is OperatorException ox)
            {
               if (ox.InnerException is GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
                     if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                        return wx.Message + " Check your access token!";
                     }
                     return wx.Message;
                  }
               }
            }
            return OriginalMessage;
         }
      }
   }

   /// <summary>
   /// Provides access to main GitLab workflow actions: load user, load merge requests etc
   /// Supports chains of actions (loading a merge request also loads its versions and commits)
   /// Each action toggles Pre-{Action}-Event and either Post-{Action}-Event or Failed-{Action}-Event
   /// </summary>
   public class WorkflowManager
   {
      public WorkflowManager(IHostProperties settings)
      {
         _settings = settings;
      }

      async public Task<bool> LoadCurrentUserAsync(string hostname)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         return await loadCurrentUserAsync(hostname);
      }

      async public Task<bool> LoadAllMergeRequestsAsync(string hostname, string search, int maxResults)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         Dictionary<Project, IEnumerable<MergeRequest>> mergeRequests =
            await loadMergeRequestsAsync(hostname, search, maxResults);
         if (mergeRequests == null)
         {
            return false; // cancelled
         }

         return true;
      }

      async public Task<bool> LoadAllMergeRequestsAsync(string hostname, Project project)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         IEnumerable<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(hostname, project);
         if (mergeRequests == null)
         {
            return false; // cancelled
         }

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            if (!await loadLatestVersionAsync(hostname, project.Path_With_Namespace, mergeRequest))
            {
               return false; // cancelled
            }
         }

         return true;
      }

      async public Task<bool> LoadMergeRequestAsync(string hostname, string projectname, int mergeRequestIId)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         return await loadMergeRequestAsync(hostname, projectname, mergeRequestIId);
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      public event Action<string> PreLoadCurrentUser;
      public event Action<string, User> PostLoadCurrentUser;
      public event Action FailedLoadCurrentUser;

      public event Action<Project> PreLoadProjectMergeRequests;
      public event Action<string, Project, IEnumerable<MergeRequest>> PostLoadProjectMergeRequests;
      public event Action FailedLoadProjectMergeRequests;

      public event Action<int> PreLoadSingleMergeRequest;
      public event Action<string, string, MergeRequest> PostLoadSingleMergeRequest;
      public event Action FailedLoadSingleMergeRequest;

      public event Action PreLoadCommits;
      public event Action<string, string, MergeRequest, IEnumerable<Commit>> PostLoadCommits;
      public event Action FailedLoadCommits;

      public event Action PreLoadLatestVersion;
      public event Action<string, string, MergeRequest, Version> PostLoadLatestVersion;
      public event Action FailedLoadLatestVersion;

      async private Task<bool> loadCurrentUserAsync(string hostName)
      {
         PreLoadCurrentUser?.Invoke(hostName);

         User currentUser;
         try
         {
            currentUser = await _operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host \"{0}\"", hostName);
            string errorMessage = String.Format("Cannot load user from host \"{0}\"", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadCurrentUser);
            return false;
         }

         PostLoadCurrentUser?.Invoke(hostName, currentUser);
         return true;
      }

      async private Task<IEnumerable<MergeRequest>> loadProjectMergeRequestsAsync(string hostname, Project project)
      {
         PreLoadProjectMergeRequests?.Invoke(project);

         string projectName = project.Path_With_Namespace;

         IEnumerable<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await _operator.GetMergeRequestsAsync(projectName);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading merge requests for project \"{0}\"", projectName);
            string errorMessage = String.Format("Cannot load project \"{0}\"", projectName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadProjectMergeRequests);
            return null;
         }

         PostLoadProjectMergeRequests?.Invoke(hostname, project, mergeRequests);
         return mergeRequests;
      }

      async private Task<Dictionary<Project, IEnumerable<MergeRequest>>> loadMergeRequestsAsync(
         string hostname, string search, int maxResults)
      {
         IEnumerable<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await _operator.SearchMergeRequestsAsync(search, maxResults);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading merge requests with search string \"{0}\"", search);
            string errorMessage = String.Format("Cannot load merge requests with search string \"{0}\"", search);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadProjectMergeRequests);
            return null;
         }

         Dictionary<Project, IEnumerable<MergeRequest>> result = new Dictionary<Project, IEnumerable<MergeRequest>>();
         foreach (KeyValuePair<int, List<MergeRequest>> keyValuePair in groupMergeRequestsByProject(mergeRequests))
         {
            Project? project = await resolveProject(hostname, keyValuePair.Key);
            if (project == null)
            {
               return null;
            }
            result.Add(project.Value, keyValuePair.Value);
         }

         foreach (KeyValuePair<Project, IEnumerable<MergeRequest>> keyValuePair in result)
         {
            PreLoadProjectMergeRequests?.Invoke(keyValuePair.Key);
            PostLoadProjectMergeRequests?.Invoke(hostname, keyValuePair.Key, keyValuePair.Value);
         }

         return result;
      }

      private Dictionary<int, List<MergeRequest>> groupMergeRequestsByProject(IEnumerable<MergeRequest> mergeRequests)
      {
         Dictionary<int, List<MergeRequest>> grouped = new Dictionary<int, List<MergeRequest>>();
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            int projectId = mergeRequest.Project_Id;
            if (!grouped.ContainsKey(projectId))
            {
               grouped[projectId] = new List<MergeRequest>();
            }
            grouped[projectId].Add(mergeRequest);
         }
         return grouped;
      }

      async private Task<Project?> resolveProject(string hostname, int projectId)
      {
         // TODO Add cache
         try
         {
            return await _operator.GetProjectAsync(projectId.ToString());
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled resolving project with Id \"{0}\"", projectId);
            string errorMessage = String.Format("Cannot load project with Id \"{0}\"", projectId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadProjectMergeRequests);
            return null;
         }
      }

      async private Task<bool> loadMergeRequestAsync(string hostname, string projectName, int mergeRequestIId)
      {
         PreLoadSingleMergeRequest?.Invoke(mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            mergeRequest = await _operator.GetMergeRequestAsync(projectName, mergeRequestIId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading MR with IId {0}", mergeRequestIId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mergeRequestIId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadSingleMergeRequest);
            return false;
         }

         PostLoadSingleMergeRequest?.Invoke(hostname, projectName, mergeRequest);

         return await loadLatestVersionAsync(hostname, projectName, mergeRequest)
             && await loadCommitsAsync(hostname, projectName, mergeRequest);
      }

      async private Task<bool> loadCommitsAsync(string hostname, string projectName, MergeRequest mergeRequest)
      {
         PreLoadCommits?.Invoke();
         IEnumerable<Commit> commits;
         try
         {
            commits = await _operator.GetCommitsAsync(projectName, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading commits for merge request with IId {0}",
               mergeRequest.IId);
            string errorMessage = String.Format("Cannot load commits for merge request with IId {0}",
               mergeRequest.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadCommits);
            return false;
         }
         PostLoadCommits?.Invoke(hostname, projectName, mergeRequest, commits);
         return true;
      }

      async private Task<bool> loadLatestVersionAsync(string hostname, string projectname, MergeRequest mergeRequest)
      {
         PreLoadLatestVersion?.Invoke();
         Version latestVersion;
         try
         {
            latestVersion = await _operator.GetLatestVersionAsync(projectname, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading latest version for merge request with IId {0}",
               mergeRequest.IId);
            string errorMessage = String.Format("Cannot load latest version for merge request with IId {0}",
               mergeRequest.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadLatestVersion);
            return false;
         }
         PostLoadLatestVersion?.Invoke(hostname, projectname, mergeRequest, latestVersion);
         return true;
      }

      private void handleOperatorException(OperatorException ex, string cancelMessage, string errorMessage,
         Action failureCallback)
      {
         bool cancelled = ex.InnerException is GitLabClientCancelled;
         if (cancelled)
         {
            Trace.TraceInformation(String.Format("[WorkflowManager] {0}", cancelMessage));
            return;
         }

         failureCallback?.Invoke();

         throw new WorkflowException(errorMessage, ex);
      }

      private readonly IHostProperties _settings;
      private WorkflowDataOperator _operator;
   }
}

