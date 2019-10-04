using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Persistence;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Tools;

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
      internal Workflow(UserDefinedSettings settings, PersistentStorage persistentStorage)
      {
         Settings = settings;

         persistentStorage.OnSerialize += (writer) => onPersistentStorageSerialize(writer);
         persistentStorage.OnDeserialize += (reader) => onPersistentStorageDeserialize(reader);
      }

      async public Task StartAsync(string hostname, string projectname, int mergeRequestIId)
      {
         string token = Tools.Tools.GetAccessToken(hostname, Settings);
         if (token == Tools.Tools.UnknownHostToken)
         {
            throw new UnknownHostException(hostname);
         }

         List<Project> enabledProjects = getEnabledProjects(hostname);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;

         if (!hasEnabledProjects || !enabledProjects.Cast<Project>().Any((x) => (x.Path_With_Namespace == projectname)))
         {
            throw new NotEnabledProjectException(projectname);
         }

         if (!await switchHostAsync(hostname, token))
         {
            return; // cancelled
         }

         List<Project> projects = await loadHostProjectsAsync(hostname);
         if (projects == null)
         {
            return; // cancelled
         }

         projects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         enabledProjects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         Debug.Assert(projects.Count == enabledProjects.Count);
         for (int iProject = 0; iProject < Math.Min(projects.Count, enabledProjects.Count); ++iProject)
         {
            Debug.Assert(projects[iProject].Id == enabledProjects[iProject].Id);
         }

         foreach (Project project in projects)
         {
            List<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(hostname, project);
            if (mergeRequests == null)
            {
               return; // cancelled
            }
         }

         await loadMergeRequestAsync(hostname, projects.Find((x) => x.Path_With_Namespace == projectname), mergeRequestIId);
      }

      async public Task StartAsync(string hostName)
      {
         string token = Tools.Tools.GetAccessToken(hostName, Settings);
         if (token == Tools.Tools.UnknownHostToken)
         {
            throw new UnknownHostException(hostName);
         }

         if (!await switchHostAsync(hostName, token))
         {
            return; // cancelled
         }

         List<Project> projects = await loadHostProjectsAsync(hostName);
         if (projects != null)
         {
            Dictionary<Project, List<MergeRequest>> projectMergeRequests = new Dictionary<Project, List<MergeRequest>>();
            foreach (Project singleProject in projects)
            {
               projectMergeRequests[singleProject] = await loadProjectMergeRequestsAsync(hostName, singleProject);
            }

            Project project = selectProjectFromList(hostName, projects, projectMergeRequests);
            if (projectMergeRequests.ContainsKey(project))
            {
               int? iid = selectMergeRequestFromList(hostName, project, projectMergeRequests[project]);
               if (iid.HasValue)
               {
                  await loadMergeRequestAsync(hostName, project, iid.Value);
               }
            }
         }
      }
	  
      async public Task LoadMergeRequestAsync(string hostname, Project project, int mergeRequestIId)
      {
         await loadMergeRequestAsync(hostname, project, mergeRequestIId);
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

      public event Action<string> PreSwitchHost;
      public event Action<User> PostSwitchHost;
      public event Action FailedSwitchHost;

      public event Action<string> PreLoadHostProjects;
      public event Action<string, List<Project>> PostLoadHostProjects;
      public event Action FailedLoadHostProjects;

      public event Action<Project> PreLoadProjectMergeRequests;
      public event Action<string, Project, List<MergeRequest>> PostLoadProjectMergeRequests;
      public event Action FailedLoadProjectMergeRequests;

      public event Action<int> PrelLoadSingleMergeRequest;
      public event Action<Project, MergeRequest> PostLoadSingleMergeRequest;
      public event Action FailedLoadSingleMergeRequest;

      public event Action PreLoadCommits;
      public event Action<MergeRequest, List<Commit>> PostLoadCommits;
      public event Action FailedLoadCommits;

      public event Action PreLoadSystemNotes;
      public event Action<string, Project, MergeRequest, List<Note>> PostLoadSystemNotes;
      public event Action FailedLoadSystemNotes;

      public event Action PreLoadLatestVersion;
      public event Action<string, Project, MergeRequest, Version> PostLoadLatestVersion;
      public event Action FailedLoadLatestVersion;

      async private Task<bool> switchHostAsync(string hostName, string token)
      {
         PreSwitchHost?.Invoke(hostName);

         Operator?.CancelAsync();
         Operator = new WorkflowDataOperator(hostName, token);

         User currentUser;
         try
         {
            currentUser = await Operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled switch host to {0}", hostName);
            string errorMessage = String.Format("Cannot load user from host {0}", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedSwitchHost);
            return false;
         }

         PostSwitchHost?.Invoke(currentUser);
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
            string cancelMessage = String.Format("Cancelled switch project to {0}", projectName);
            string errorMessage = String.Format("Cannot load project {0}", projectName);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadProjectMergeRequests);
            return null;
         }

         PostLoadProjectMergeRequests?.Invoke(hostname, project, mergeRequests);
         return mergeRequests;
      }

      async private Task loadMergeRequestAsync(string hostname, Project project, int mergeRequestIId)
      {
         PrelLoadSingleMergeRequest?.Invoke(mergeRequestIId);

         string projectName = project.Path_With_Namespace;

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            mergeRequest = await Operator.GetMergeRequestAsync(projectName, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled switch MR to MR with IId {0}", mergeRequestIId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mergeRequestIId);
            handleOperatorException(ex, cancelMessage, errorMessage, FailedLoadSingleMergeRequest);
            return;
         }

         _lastProjectsByHosts[hostname] = projectName;

         OldProjectKey key = new OldProjectKey { HostName = hostname, ProjectId = project.Id };
         _lastMergeRequestsByProjects[key] = mergeRequestIId;

         PostLoadSingleMergeRequest?.Invoke(project, mergeRequest);

         if (!await loadLatestVersionAsync(hostname, project, mergeRequest)
          || !await loadSystemNotesAsync(hostname, project, mergeRequest))
         {
            return;
         }
         await loadCommitsAsync(projectName, mergeRequest);
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

      async private Task<bool> loadSystemNotesAsync(string hostname, Project project, MergeRequest mergeRequest)
      {
         PreLoadSystemNotes?.Invoke();
         List<Note> notes;
         try
         {
            notes = await Operator.GetSystemNotesAsync(project.Path_With_Namespace, mergeRequest.IId);
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
         PostLoadSystemNotes?.Invoke(hostname, project, mergeRequest, notes);
         return true;
      }

      async private Task<bool> loadLatestVersionAsync(string hostname, Project project, MergeRequest mergeRequest)
      {
         PreLoadLatestVersion?.Invoke();
         Version latestVersion;
         try
         {
            latestVersion = await Operator.GetLatestVersionAsync(project.Path_With_Namespace, mergeRequest.IId);
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
         PostLoadLatestVersion?.Invoke(hostname, project, mergeRequest, latestVersion);
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

      private Project selectProjectFromList(string hostname, List<Project> projects,
         Dictionary<Project, List<MergeRequest>> projectMergeRequests)
      {
         string key = hostname;
         // if we remember a project selected for the given host before...
         if (_lastProjectsByHosts.ContainsKey(key)
            // ... and if such project still exists in a list of Projects
            && projects.Any((x) => x.Path_With_Namespace == _lastProjectsByHosts[key]))
         {
            foreach (KeyValuePair<Project, List<MergeRequest>> mergeRequests in projectMergeRequests)
            {
               if (mergeRequests.Key.Path_With_Namespace == _lastProjectsByHosts[key])
               {
                  return mergeRequests.Key;
               }
            }
         }

         return projects.Count > 0 ? projects[0] : default(Project);
      }

      private int? selectMergeRequestFromList(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         mergeRequests = Tools.Tools.FilterMergeRequests(mergeRequests, Settings);

         OldProjectKey key = new OldProjectKey { HostName = hostname, ProjectId = project.Id };

         // if we remember MR selected for the given host/project before...
         if (_lastMergeRequestsByProjects.ContainsKey(key)
            // ... and if such MR still exists in a list of MRs
            && mergeRequests.Any((x) => x.IId == _lastMergeRequestsByProjects[key]))
         {
            return _lastMergeRequestsByProjects[key];
         }

         return mergeRequests.Count > 0 ? mergeRequests[0].Id : new Nullable<int>();
      }

      private List<Project> getEnabledProjects(string hostname)
      {
         return Tools.Tools.LoadProjectsFromFile(hostname);
      }

      private void onPersistentStorageSerialize(IPersistentStateSetter writer)
      {
         writer.Set("ProjectsByHosts", _lastProjectsByHosts);

         Dictionary<string, int> mergeRequestsByProjects = _lastMergeRequestsByProjects.ToDictionary(
               item => item.Key.HostName + "|" + item.Key.ProjectId.ToString(),
               item => item.Value);
         writer.Set("MergeRequestsByProjects", mergeRequestsByProjects);
      }

      private void onPersistentStorageDeserialize(IPersistentStateGetter reader)
      {
         Dictionary<string, object> projectsByHosts = (Dictionary<string, object>)reader.Get("ProjectsByHosts");
         if (projectsByHosts != null)
         {
            _lastProjectsByHosts = projectsByHosts.ToDictionary(item => item.Key, item => item.Value.ToString());
         }

         Dictionary<string, object> mergeRequestsByProjects =
            (Dictionary<string, object>)reader.Get("MergeRequestsByProjects");
         if (mergeRequestsByProjects != null)
         {
            _lastMergeRequestsByProjects = mergeRequestsByProjects.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');

                  Debug.Assert(splitted.Length == 2);

                  string host = splitted[0];
                  string projectId = splitted[1];
                  return new OldProjectKey { HostName = host, ProjectId = int.Parse(projectId) };
               },
               item => (int)item.Value);
         }
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

      private Dictionary<string, string> _lastProjectsByHosts = new Dictionary<string, string>();
      private Dictionary<OldProjectKey, int> _lastMergeRequestsByProjects = new Dictionary<OldProjectKey, int>();
   }
}

