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
      internal NoProjectsException(): base(
         String.Format("Project list is empty")) {}
   }

   public class NotEnabledProjectException : WorkflowException
   {
      internal NotEnabledProjectException(string projectname): base(
         String.Format("Project {0} is not in the list of enabled projects", projectname)) {}
   }

   public class NotAvailableMergeRequest : WorkflowException
   {
      internal NotAvailableMergeRequest(int iid): base(
         String.Format("Merge Request with IId {0} is not available", iid)) {}
   }

   /// <summary>
   /// Client workflow related to Hosts/Projects/Merge Requests
   /// </summary>
   public class Workflow : IDisposable
   {
      public Workflow(UserDefinedSettings settings, Action<string> onNonFatalError)
      {
         Settings = settings;
         _onNonFatalError = onNonFatalError;
      }

      async public Task LoadCurrentUserAsync(string hostname)
      {
         checkParameters(hostname);

         await loadCurrentUserAsync(hostname);
      }

      async public Task LoadAllMergeRequestsAsync(string hostname)
      {
         checkParameters(hostname);

         PreLoadAllMergeRequests?.Invoke();

         List<Project> projects = await loadHostProjectsAsync(hostname);
         if (projects == null)
         {
            return; // cancelled
         }

#if DEBUG
         List<Project> enabledProjects = getEnabledProjects(hostname);
         projects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         enabledProjects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         Debug.Assert(projects.Count == enabledProjects.Count);
         for (int iProject = 0; iProject < Math.Min(projects.Count, enabledProjects.Count); ++iProject)
         {
            Debug.Assert(projects[iProject].Id == enabledProjects[iProject].Id);
         }
#endif

         foreach (Project project in projects)
         {
            try
            {
               List<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(hostname, project);
               if (mergeRequests == null)
               {
                  return; // cancelled
               }
            }
            catch (WorkflowException ex)
            {
               _onNonFatalError?.Invoke(ex.Message);
            }
         }

         PostLoadAllMergeRequests?.Invoke();
      }

      async public Task<bool> LoadMergeRequestAsync(string hostname, string projectname, int mergeRequestIId)
      {
         if (mergeRequestIId == 0)
         {
            PrelLoadSingleMergeRequest?.Invoke(0);
            Operator?.CancelAsync();
            return false;
         }

         checkParameters(hostname, projectname);

         return await loadMergeRequestAsync(hostname, projectname, mergeRequestIId);
      }

      async public Task CancelAsync()
      {
         if (Operator != null)
         {
            await Operator.CancelAsync();
         }
      }

      public void Dispose()
      {
         Operator?.Dispose();
      }

      /// <summary>
      /// Return projects at the current Host that are allowed to be checked for updates
      /// </summary>
      public List<Project> GetProjectsToUpdate(string hostname)
      {
         List<Project> enabledProjects = getEnabledProjects(hostname);
         if ((enabledProjects?.Count ?? 0) != 0)
         {
            return enabledProjects;
         }

         return new List<Project>();
      }

      public event Action<string> PreLoadCurrentUser;
      public event Action<User> PostLoadCurrentUser;
      public event Action FailedLoadCurrentUser;

      public event Action<string> PreLoadHostProjects;
      public event Action<string, List<Project>> PostLoadHostProjects;
      public event Action FailedLoadHostProjects;

      public event Action PreLoadAllMergeRequests;

      public event Action<Project> PreLoadProjectMergeRequests;
      public event Action<string, Project, List<MergeRequest>> PostLoadProjectMergeRequests;
      public event Action FailedLoadProjectMergeRequests;

      public event Action PostLoadAllMergeRequests;

      public event Action<int> PrelLoadSingleMergeRequest;
      public event Action<string, MergeRequest> PostLoadSingleMergeRequest;
      public event Action FailedLoadSingleMergeRequest;

      public event Action PreLoadCommits;
      public event Action<MergeRequest, List<Commit>> PostLoadCommits;
      public event Action FailedLoadCommits;

      public event Action PreLoadSystemNotes;
      public event Action<string, string, MergeRequest, List<Note>> PostLoadSystemNotes;
      public event Action FailedLoadSystemNotes;

      public event Action PreLoadLatestVersion;
      public event Action<string, string, MergeRequest, Version> PostLoadLatestVersion;
      public event Action FailedLoadLatestVersion;

      private void checkParameters(string hostname, string projectname = "")
      {
         string token = Settings.GetAccessToken(hostname);
         if (token == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         Operator?.CancelAsync();
         Operator = new WorkflowDataOperator(hostname, token);

         List<Project> enabledProjects = getEnabledProjects(hostname);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;
         if (!hasEnabledProjects)
         {
            throw new NoProjectsException();
         }

         if (projectname != String.Empty &&
            (!hasEnabledProjects || !enabledProjects.Cast<Project>().Any((x) => (x.Path_With_Namespace == projectname))))
         {
            throw new NotEnabledProjectException(projectname);
         }
      }

      async private Task<bool> loadCurrentUserAsync(string hostName)
      {
         PreLoadCurrentUser?.Invoke(hostName);

         User currentUser;
         try
         {
            currentUser = await Operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host {0}", hostName);
            string errorMessage = String.Format("Cannot load user from host {0}", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadCurrentUser);
            return false;
         }

         PostLoadCurrentUser?.Invoke(currentUser);
         return true;
      }

      async private Task<List<Project>> loadHostProjectsAsync(string hostName)
      {
         PreLoadHostProjects?.Invoke(hostName);

         List<Project> enabledProjects = getEnabledProjects(hostName);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;

         List<Project> projects;
         try
         {
            projects = hasEnabledProjects ?  enabledProjects : await Operator.GetProjectsAsync(Settings.ShowPublicOnly);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading projects from host {0}", hostName);
            string errorMessage = String.Format("Cannot load projects from host {0}", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadHostProjects);
            return null;
         }

         PostLoadHostProjects?.Invoke(hostName, projects);
         return projects;
      }

      async private Task<List<MergeRequest>> loadProjectMergeRequestsAsync(string hostname, Project project)
      {
         PreLoadProjectMergeRequests?.Invoke(project);

         string projectName = project.Path_With_Namespace;

         List<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await Operator.GetMergeRequestsAsync(projectName);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading project merge requests from {0}", projectName);
            string errorMessage = String.Format("Cannot load project {0}", projectName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadProjectMergeRequests);
            return null;
         }

         PostLoadProjectMergeRequests?.Invoke(hostname, project, mergeRequests);
         return mergeRequests;
      }

      async private Task<bool> loadMergeRequestAsync(string hostname, string projectName, int mergeRequestIId)
      {
         PrelLoadSingleMergeRequest?.Invoke(mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            mergeRequest = await Operator.GetMergeRequestAsync(projectName, mergeRequestIId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading MR with IId {0}", mergeRequestIId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mergeRequestIId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadSingleMergeRequest);
            return false;
         }

         PostLoadSingleMergeRequest?.Invoke(projectName, mergeRequest);

         if (!await loadLatestVersionAsync(hostname, projectName, mergeRequest)
          || !await loadSystemNotesAsync(hostname, projectName, mergeRequest))
         {
            return false;
         }
         return await loadCommitsAsync(projectName, mergeRequest);
      }

      async private Task<bool> loadCommitsAsync(string projectName, MergeRequest mergeRequest)
      {
         PreLoadCommits?.Invoke();
         List<Commit> commits;
         try
         {
            commits = await Operator.GetCommitsAsync(projectName, mergeRequest.IId);
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
         PostLoadCommits?.Invoke(mergeRequest, commits);
         return true;
      }

      async private Task<bool> loadSystemNotesAsync(string hostname, string projectname, MergeRequest mergeRequest)
      {
         PreLoadSystemNotes?.Invoke();
         List<Note> notes;
         try
         {
            notes = await Operator.GetSystemNotesAsync(projectname, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading system notes for merge request with IId {0}",
               mergeRequest.IId);
            string errorMessage = String.Format("Cannot load system notes for merge request with IId {0}",
               mergeRequest.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadSystemNotes);
            return false;
         }
         PostLoadSystemNotes?.Invoke(hostname, projectname, mergeRequest, notes);
         return true;
      }

      async private Task<bool> loadLatestVersionAsync(string hostname, string projectname, MergeRequest mergeRequest)
      {
         PreLoadLatestVersion?.Invoke();
         Version latestVersion;
         try
         {
            latestVersion = await Operator.GetLatestVersionAsync(projectname, mergeRequest.IId);
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
         // Check if file exists. If it does not, it is not an error.
         if (System.IO.File.Exists(mrHelper.Common.Constants.Constants.ProjectListFileName))
         {
            try
            {
               List<HostInProjectsFile> hosts =
                  Tools.Tools.LoadListFromFile<HostInProjectsFile>(mrHelper.Common.Constants.Constants.ProjectListFileName);
               return hosts.FirstOrDefault((x) => x.Name == hostname).Projects;
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            }
         }

         return null;
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

#pragma warning disable 0649
      private struct HostInProjectsFile
      {
         public string Name;
         public List<Project> Projects;
      }
#pragma warning restore 0649

      private Action<string> _onNonFatalError;
   }
}

