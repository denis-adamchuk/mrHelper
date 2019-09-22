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
                  await switchHostAsync(State.HostName, true);
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
                  await switchProjectAsync(State.Project.Path_With_Namespace, true);
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
         await switchHostAsync(hostname, true);
      }

      async public Task SwitchHostAsync(string hostName, bool recursive)
      {
         await switchHostAsync(hostName, recursive);
      }

      async public Task SwitchProjectAsync(string projectName, bool recursive)
      {
         if (State == null)
         {
            // not initialized
            Debug.Assert(false);
            return;
         }

         await switchProjectAsync(projectName, recursive);
      }

      async public Task SwitchMergeRequestAsync(int mergeRequestIId, bool recursive)
      {
         if (State == null)
         {
            // not initialized
            Debug.Assert(false);
            return;
         }

         await switchMergeRequestAsync(mergeRequestIId, recursive);
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

         List<Project> enabledProjects = getEnabledProjects();
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

      async private Task switchHostAsync(string hostName, bool recursive)
      {
         PreSwitchHost?.Invoke(hostName);

         State = new WorkflowState
         {
            HostName = hostName
         };

         Operator?.CancelAsync();

         if (hostName == String.Empty)
         {
            return;
         }

         Operator = new WorkflowDataOperator(hostName, Tools.Tools.GetAccessToken(hostName, Settings));

         List<Project> enabledProjects = getEnabledProjects();
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
               return; // silent return
            }
            FailedSwitchHost?.Invoke();
            throw new WorkflowException(String.Format("Cannot load projects from host {0}", hostName));
         }

         State.CurrentUser = currentUser;

         PostSwitchHost?.Invoke(State, projects);

         if (recursive)
         {
            string projectName = selectProjectFromList(projects);
            if (projectName != String.Empty)
            {
               await switchProjectAsync(projectName, true);
            }
         }
      }

      async private Task switchProjectAsync(string projectName, bool recursive)
      {
         PreSwitchProject?.Invoke(projectName);

         if (projectName == String.Empty)
         {
            return;
         }

         Project project = new Project();
         List<MergeRequest> mergeRequests = null;
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
               return; // silent return
            }
            FailedSwitchProject?.Invoke();
            throw new WorkflowException(String.Format("Cannot load project {0}", projectName));
         }

         State.Project = project;

         _lastProjectsByHosts[State.HostName] = State.Project.Path_With_Namespace;

         PostSwitchProject?.Invoke(State, mergeRequests);

         if (recursive)
         {
            int? iid = selectMergeRequestFromList(mergeRequests);
            if (iid.HasValue)
            {
               await switchMergeRequestAsync(iid.Value, recursive);
            }
         }
      }

      async private Task switchMergeRequestAsync(int mergeRequestIId, bool recursive)
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

         if (recursive)
         {
            if (!await loadCommitsAsync() || !await loadSystemNotesAsync())
            {
               return;
            }
            await loadLatestVersionAsync();
         }
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

      private List<Project> getEnabledProjects()
      {
         return Tools.Tools.LoadProjectsFromFile(State.HostName);
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

