using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Tools;
using mrHelper.Common;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : Exception
   {
      internal WorkflowException(string message) : base(message) { }
   }

   public class UnknownHostException : WorkflowException
   {
      internal UnknownHostException(string hostname): base(
         String.Format("Cannot find access token for host {0}", hostname)) {}
   }

   public class NoProjectsException : WorkflowException
   {
      internal NoProjectsException(string hostname): base(
         String.Format("Project list for hostname {0} is empty", hostname)) {}
   }

   public class NotEnabledProjectException : WorkflowException
   {
      internal NotEnabledProjectException(string projectname): base(
         String.Format("Project {0} is not in the list of enabled projects", projectname)) {}
   }

   /// <summary>
   /// Client workflow related to Hosts/Projects/Merge Requests
   /// </summary>
   public class Workflow
   {
      public Workflow(UserDefinedSettings settings)
      {
         _settings = settings;
      }

      async public Task<bool> LoadCurrentUserAsync(string hostname)
      {
         return checkParameters(hostname) && await loadCurrentUserAsync(hostname);
      }

      async public Task<bool> LoadAllMergeRequestsAsync(string hostname, Action<string> onNonFatalError)
      {
         if (!checkParameters(hostname))
         {
            return false;
         }

         PreLoadAllMergeRequests?.Invoke();

         List<Project> projects = loadHostProjects(hostname);
         if (projects == null)
         {
            return false; // cancelled
         }

         foreach (Project project in projects)
         {
            try
            {
               List<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(hostname, project);
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
            }
            catch (WorkflowException ex)
            {
               onNonFatalError?.Invoke(ex.Message);
            }
         }

         PostLoadAllMergeRequests?.Invoke(hostname, projects);
         return true;
      }

      async public Task<bool> LoadMergeRequestAsync(string hostname, string projectname, int mergeRequestIId)
      {
         if (mergeRequestIId == 0)
         {
            PreLoadSingleMergeRequest?.Invoke(0);
            _operator?.CancelAsync();
            return false;
         }

         return checkParameters(hostname, projectname)
             && await loadMergeRequestAsync(hostname, projectname, mergeRequestIId);
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      public event Action<string> PreLoadCurrentUser;
      public event Action<User> PostLoadCurrentUser;
      public event Action FailedLoadCurrentUser;

      public event Action<string> PreLoadHostProjects;
      public event Action<string, List<Project>> PostLoadHostProjects;

      public event Action PreLoadAllMergeRequests;

      public event Action<Project> PreLoadProjectMergeRequests;
      public event Action<string, Project, List<MergeRequest>> PostLoadProjectMergeRequests;
      public event Action FailedLoadProjectMergeRequests;

      public event Action<string, List<Project>> PostLoadAllMergeRequests;

      public event Action<int> PreLoadSingleMergeRequest;
      public event Action<string, string, MergeRequest> PostLoadSingleMergeRequest;
      public event Action FailedLoadSingleMergeRequest;

      public event Action PreLoadCommits;
      public event Action<string, string, MergeRequest, List<Commit>> PostLoadCommits;
      public event Action FailedLoadCommits;

      public event Action PreLoadLatestVersion;
      public event Action<string, string, MergeRequest, Version> PostLoadLatestVersion;
      public event Action FailedLoadLatestVersion;

      private bool checkParameters(string hostname, string projectname = "")
      {
         _operator?.CancelAsync();

         if (hostname == String.Empty)
         {
            return false;
         }

         string token = ConfigurationHelper.GetAccessToken(hostname, _settings);
         if (token == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         _operator = new WorkflowDataOperator(hostname, token);

         List<Project> enabledProjects = getEnabledProjects(hostname);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;
         if (!hasEnabledProjects)
         {
            throw new NoProjectsException(hostname);
         }

         if (projectname != String.Empty &&
            (!enabledProjects.Cast<Project>().Any((x) => (x.Path_With_Namespace == projectname))))
         {
            throw new NotEnabledProjectException(projectname);
         }

         return true;
      }

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

         PostLoadCurrentUser?.Invoke(currentUser);
         return true;
      }

      private List<Project> loadHostProjects(string hostName)
      {
         PreLoadHostProjects?.Invoke(hostName);

         List<Project> enabledProjects = getEnabledProjects(hostName);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;
         Debug.Assert(hasEnabledProjects); // guaranteed by checkParameters()

         PostLoadHostProjects?.Invoke(hostName, enabledProjects);
         return enabledProjects;
      }

      async private Task<List<MergeRequest>> loadProjectMergeRequestsAsync(string hostname, Project project)
      {
         PreLoadProjectMergeRequests?.Invoke(project);

         string projectName = project.Path_With_Namespace;

         List<MergeRequest> mergeRequests;
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
         List<Commit> commits;
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
         bool cancelled = ex.InternalException is GitLabClientCancelled;
         if (cancelled)
         {
            Trace.TraceInformation(String.Format("[Workflow] {0}", cancelMessage));
            return;
         }

         failureCallback?.Invoke();

         string details = String.Empty;
         if (ex.InternalException is GitLabSharp.Accessors.GitLabRequestException internalEx)
         {
            details = internalEx.WebException.Message;
         }

         string message = String.Format("{0}. {1}", errorMessage, details);
         Trace.TraceError(String.Format("[Workflow] {0}", message));
         throw new WorkflowException(message);
      }

      private List<Project> getEnabledProjects(string hostname)
      {
         return ConfigurationHelper.GetEnabledProjectsForHost(hostname, _settings)
            .Select(x => new Project{ Path_With_Namespace = x })
            .ToList();
      }

      private readonly UserDefinedSettings _settings;
      private WorkflowDataOperator _operator;
   }
}

