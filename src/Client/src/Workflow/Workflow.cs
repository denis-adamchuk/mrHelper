using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Common.Types;
using mrHelper.Client.Tools;
using mrHelper.Client.Persistence;
using Version = GitLabSharp.Entities.Version;

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
         Settings.PropertyChanged += async (sender, property) =>
         {
            if (property.PropertyName == "ShowPublicOnly")
            {
               // emulate host change to reload project list
               try
               {
                  await SwitchHostAsync(State.HostName);
               }
               catch (WorkflowException)
               {
                  // just do nothing
               }
            }
            else if (property.PropertyName == "LastUsedLabels")
            {
               // emulate project change to reload merge request list
               try
               {
                  await SwitchProjectAsync(State.Project.Path_With_Namespace);
               }
               catch (WorkflowException)
               {
                  // just do nothing
               }
            }
         };

         persistentStorage.OnSerialize += (writer) => onPersistentStorageSerialize(writer);
         persistentStorage.OnDeserialize += (reader) => onPersistentStorageDeserialize(reader);
      }

      async public Task InitializeAsync(string hostname)
      {
         await SwitchHostAsync(hostname);
      }

      async public Task InitializeAsync(string hostname, string projectname, int mergeRequestIId)
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

         List<Project> projects = await loadHostAsync(hostname, token);
         projects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         enabledProjects.Sort((x, y) => x.Id == y.Id ? 0 : (x.Id < y.Id ? -1 : 1));
         Debug.Assert(projects.Count == enabledProjects.Count);
         for (int iProject = 0; iProject < Math.Min(projects.Count, enabledProjects.Count); ++iProject)
         {
            Debug.Assert(projects[iProject].Id == enabledProjects[iProject].Id);
         }

         List<MergeRequest> mergeRequests = await loadProjectAsync(projectname);
         Debug.Assert(mergeRequests != null);

         if (!mergeRequests.Cast<MergeRequest>().Any((x) => x.IId == mergeRequestIId))
         {
            throw new NotAvailableMergeRequest(mergeRequestIId);
         }
         await loadMergeRequestAsync(mergeRequestIId);
      }

      async public Task SwitchHostAsync(string hostName)
      {
         string token = Tools.Tools.GetAccessToken(hostName, Settings);
         if (token == Tools.Tools.UnknownHostToken)
         {
            throw new UnknownHostException(hostName);
         }

         List<Project> projects = await loadHostAsync(hostName, token);
         if (projects != null)
         {
            string projectName = selectProjectFromList(projects);
            if (projectName != String.Empty)
            {
               await SwitchProjectAsync(projectName);
            }
         }
      }

      async public Task SwitchProjectAsync(string projectName)
      {
         if (State == null)
         {
            // not initialized
            Debug.Assert(false);
            return;
         }

         List<MergeRequest> mergeRequests = await loadProjectAsync(projectName);
         if (mergeRequests != null)
         {
            int? iid = selectMergeRequestFromList(mergeRequests);
            if (iid.HasValue)
            {
               await loadMergeRequestAsync(iid.Value);
            }
         }
      }

      async public Task SwitchMergeRequestAsync(int mergeRequestIId)
      {
         if (State == null)
         {
            // not initialized
            Debug.Assert(false);
            return;
         }

         await loadMergeRequestAsync(mergeRequestIId);
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
      public List<Project> GetProjectsToUpdate()
      {
         if (State == null)
         {
            // not initialized
            Debug.Assert(false);
            return null;
         }

         List<Project> enabledProjects = getEnabledProjects(State.HostName);
         if ((enabledProjects?.Count ?? 0) != 0)
         {
            return enabledProjects;
         }

         return State.Project.Id != default(Project).Id ? new List<Project>{ State.Project } : new List<Project>();
      }

      public event Action<string> PreSwitchHost;
      public event Action<WorkflowState, List<Project>> PostSwitchHost;
      public event Action FailedSwitchHost;

      public event Action<string> PreSwitchProject;
      public event Action<WorkflowState, List<MergeRequest>> PostSwitchProject;
      public event Action FailedSwitchProject;

      public event Action<int> PreSwitchMergeRequest;
      public event Action<WorkflowState> PostSwitchMergeRequest;
      public event Action FailedSwitchMergeRequest;

      public event Action PreLoadCommits;
      public event Action<WorkflowState, List<Commit>> PostLoadCommits;
      public event Action FailedLoadCommits;

      public event Action PreLoadSystemNotes;
      public event Action<WorkflowState, List<Note>> PostLoadSystemNotes;
      public event Action FailedLoadSystemNotes;

      public event Action PreLoadLatestVersion;
      public event Action<WorkflowState, Version> PostLoadLatestVersion;
      public event Action FailedLoadLatestVersion;

      public WorkflowState State { get; private set; }

      async private Task<List<Project>> loadHostAsync(string hostName, string token)
      {
         PreSwitchHost?.Invoke(hostName);

         State = new WorkflowState
         {
            HostName = hostName
         };

         Operator?.CancelAsync();
         Operator = new WorkflowDataOperator(hostName, token);

         List<Project> enabledProjects = getEnabledProjects(hostName);
         bool hasEnabledProjects = (enabledProjects?.Count ?? 0) != 0;

         User currentUser;
         List<Project> projects;
         try
         {
            currentUser = await Operator.GetCurrentUserAsync();
            projects = hasEnabledProjects ?
               enabledProjects : await Operator.GetProjectsAsync(hostName, Settings.ShowPublicOnly);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled switch host to {0}", hostName));
               return null; // silent return
            }
            FailedSwitchHost?.Invoke();
            throw new WorkflowException(String.Format("Cannot load projects from host {0}", hostName));
         }

         State.CurrentUser = currentUser;

         PostSwitchHost?.Invoke(State, projects);
         return projects;
      }

      async private Task<List<MergeRequest>> loadProjectAsync(string projectName)
      {
         PreSwitchProject?.Invoke(projectName);

         Project project = new Project();
         List<MergeRequest> mergeRequests;
         try
         {
            project = await Operator.GetProjectAsync(projectName);
            mergeRequests = await Operator.GetMergeRequestsAsync(project.Path_With_Namespace);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled switch project to {0}", projectName));
               return null; // silent return
            }
            FailedSwitchProject?.Invoke();
            throw new WorkflowException(String.Format("Cannot load project {0}", projectName));
         }

         State.Project = project;

         _lastProjectsByHosts[State.HostName] = State.Project.Path_With_Namespace;

         PostSwitchProject?.Invoke(State, mergeRequests);
         return mergeRequests;
      }

      async private Task loadMergeRequestAsync(int mergeRequestIId)
      {
         PreSwitchMergeRequest?.Invoke(mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            mergeRequest = await Operator.GetMergeRequestAsync(State.Project.Path_With_Namespace, mergeRequestIId);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled switch MR IID to {0}",
                  mergeRequestIId.ToString()));
               return; // silent return
            }
            FailedSwitchMergeRequest?.Invoke();
            throw new WorkflowException(String.Format("Cannot load merge request with IId {0}", mergeRequestIId));
         }

         State.MergeRequest = mergeRequest;

         ProjectKey key = new ProjectKey { HostName = State.HostName, ProjectId = State.Project.Id };
         _lastMergeRequestsByProjects[key] = mergeRequestIId;

         PostSwitchMergeRequest?.Invoke(State);

         if (!await loadCommitsAsync() || !await loadSystemNotesAsync())
         {
            return;
         }
         await loadLatestVersionAsync();
      }

      async private Task<bool> loadCommitsAsync()
      {
         PreLoadCommits?.Invoke();
         List<Commit> commits;
         try
         {
            commits = await Operator.GetCommitsAsync(State.Project.Path_With_Namespace, State.MergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled loading commits"));
               return false; // silent return
            }
            FailedLoadCommits?.Invoke();
            throw new WorkflowException(String.Format(
               "Cannot load commits for merge request with IId {0}", State.MergeRequest.IId));
         }
         PostLoadCommits?.Invoke(State, commits);
         return true;
      }

      async private Task<bool> loadSystemNotesAsync()
      {
         PreLoadSystemNotes?.Invoke();
         List<Note> notes;
         try
         {
            notes = await Operator.GetSystemNotesAsync(State.Project.Path_With_Namespace, State.MergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled loading system notes"));
               return false; // silent return
            }
            FailedLoadSystemNotes?.Invoke();
            throw new WorkflowException(String.Format(
               "Cannot load system notes for merge request with IId {0}", State.MergeRequest.IId));
         }
         PostLoadSystemNotes?.Invoke(State, notes);
         return true;
      }

      async private Task<bool> loadLatestVersionAsync()
      {
         PreLoadLatestVersion?.Invoke();
         Version latestVersion;
         try
         {
            latestVersion = await Operator.GetLatestVersionAsync(
               State.Project.Path_With_Namespace, State.MergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[Workflow] Cancelled loading latest version"));
               return false; // silent return
            }
            FailedLoadLatestVersion?.Invoke();
            throw new WorkflowException(String.Format(
               "Cannot load latest version for merge request with IId {0}", State.MergeRequest.IId));
         }
         PostLoadLatestVersion?.Invoke(State, latestVersion);
         return true;
      }

      private string selectProjectFromList(List<Project> projects)
      {
         string key = State.HostName;
         // if we remember a project selected for the given host before...
         if (_lastProjectsByHosts.ContainsKey(key)
            // ... and if such project still exists in a list of Projects
            && projects.Any((x) => x.Path_With_Namespace == _lastProjectsByHosts[key]))
         {
            return _lastProjectsByHosts[key];
         }

         return projects.Count > 0 ? projects[0].Path_With_Namespace : String.Empty;
      }

      private int? selectMergeRequestFromList(List<MergeRequest> mergeRequests)
      {
         mergeRequests = Tools.Tools.FilterMergeRequests(mergeRequests, Settings);

         ProjectKey key = new ProjectKey { HostName = State.HostName, ProjectId = State.Project.Id };
         // if we remember MR selected for the given host/project before...
         if (_lastMergeRequestsByProjects.ContainsKey(key)
            // ... and if such MR still exists in a list of MRs
            && mergeRequests.Any((x) => x.IId == _lastMergeRequestsByProjects[key]))
         {
            return _lastMergeRequestsByProjects[key];
         }

         return mergeRequests.Count > 0 ? mergeRequests[0].IId : new Nullable<int>();
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
                  return new ProjectKey { HostName = host, ProjectId = int.Parse(projectId) };
               },
               item => (int)item.Value);
         }
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

      private Dictionary<string, string> _lastProjectsByHosts = new Dictionary<string, string>();
      private Dictionary<ProjectKey, int> _lastMergeRequestsByProjects = new Dictionary<ProjectKey, int>();
   }
}

