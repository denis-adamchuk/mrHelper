using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

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
      internal Workflow(UserDefinedSettings settings)
      {
         Settings = settings;
         Settings.PropertyChanged += async (sender, property) =>
         {
            if (property.PropertyName == "ShowPublicOnly")
            {
               // emulate host change to reload project list
               try
               {
                  await switchHostAsync(State.HostName);
               }
               catch (WorkflowException)
               {
                  // just do nothing
               }
            }
            else if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
               // emulate project change to reload merge request list
               try
               {
                  await switchProjectAsync(State.Project.Path_With_Namespace);
               }
               catch (WorkflowException)
               {
                  // just do nothing
               }
            }
         };
         _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
      }

      async public Task SwitchHostAsync(string hostName)
      {
         await switchHostAsync(hostName);
      }

      async public Task SwitchProjectAsync(string projectName)
      {
         await switchProjectAsync(projectName);
      }

      async public Task SwitchMergeRequestAsync(int mergeRequestIId)
      {
         await switchMergeRequestAsync(mergeRequestIId);
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

      public WorkflowState State { get; private set; } = new WorkflowState();

      async private Task switchHostAsync(string hostName)
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

         Operator = new WorkflowDataOperator(hostName, Tools.Tools.GetAccessToken(hostName, Settings), Settings);

         User currentUser;
         List<Project> projects;
         try
         {
            currentUser = await Operator.GetCurrentUserAsync();
            projects = await Operator.GetProjectsAsync(hostName, Settings.ShowPublicOnly);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               return; // silent return
            }
            FailedSwitchHost?.Invoke();
            throw new WorkflowException(String.Format("Cannot load projects from host {0}", hostName));
         }

         State.CurrentUser = currentUser;

         PostSwitchHost?.Invoke(State, projects);

         string projectName = selectProjectFromList(projects);
         if (projectName != String.Empty)
         {
            await switchProjectAsync(projectName);
         }
      }

      async private Task switchProjectAsync(string projectName)
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
            mergeRequests = await Operator.GetMergeRequestsAsync(
               project.Path_With_Namespace, Settings.CheckedLabelsFilter ? _cachedLabels : null);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               return; // silent return
            }
            FailedSwitchProject?.Invoke();
            throw new WorkflowException(String.Format("Cannot load project {0}", projectName));
         }

         State.Project = project;

         _lastProjectsByHosts[State.HostName] = State.Project.Path_With_Namespace;

         PostSwitchProject?.Invoke(State, mergeRequests);

         int? iid = selectMergeRequestFromList(mergeRequests);
         if (iid.HasValue)
         {
            await switchMergeRequestAsync(iid.Value);
         }
      }

      async private Task switchMergeRequestAsync(int mergeRequestIId)
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
               return; // silent return
            }
            FailedSwitchMergeRequest?.Invoke();
            throw new WorkflowException(String.Format("Cannot load merge request with IId {0}", mergeRequestIId));
         }

         State.MergeRequest = mergeRequest;

         HostAndProjectId key = new HostAndProjectId { Host = State.HostName, ProjectId = State.Project.Id };
         _lastMergeRequestsByProjects[key] = mergeRequestIId;

         PostSwitchMergeRequest?.Invoke(State);

         if (!await loadCommitsAsync())
         {
            return; // silent return
         }
         await loadSystemNotesAsync();
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
               return false; // silent return
            }
            FailedLoadCommits?.Invoke();
            throw new WorkflowException(String.Format(
               "Cannot load commits for merge request with IId {0}", State.MergeRequest.IId));
         }
         PostLoadCommits?.Invoke(State, commits);
         return true;
      }

      async private Task loadSystemNotesAsync()
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
               return; // silent return
            }
            FailedLoadSystemNotes?.Invoke();
            throw new WorkflowException(String.Format(
               "Cannot load system notes for merge request with IId {0}", State.MergeRequest.IId));
         }
         PostLoadSystemNotes?.Invoke(State, notes);
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

         foreach (var project in projects)
         {
            if (project.Path_With_Namespace == Settings.LastSelectedProject)
            {
               return project.Path_With_Namespace;
            }
         }

         return projects.Count > 0 ? projects[0].Path_With_Namespace : String.Empty;
      }

      private int? selectMergeRequestFromList(List<MergeRequest> mergeRequests)
      {
         HostAndProjectId key = new HostAndProjectId { Host = State.HostName, ProjectId = State.Project.Id };
         // if we remember MR selected for the given host/project before...
         if (_lastMergeRequestsByProjects.ContainsKey(key)
            // ... and if such MR still exists in a list of MRs
            && mergeRequests.Any((x) => x.IId == _lastMergeRequestsByProjects[key]))
         {
            return _lastMergeRequestsByProjects[key];
         }

         return mergeRequests.Count > 0 ? mergeRequests[0].IId : new Nullable<int>();
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

      private List<string> _cachedLabels = null;

      private readonly Dictionary<string, string> _lastProjectsByHosts = new Dictionary<string, string>();
      private struct HostAndProjectId
      {
         public string Host;
         public int ProjectId;
      }
      private readonly Dictionary<HostAndProjectId, int> _lastMergeRequestsByProjects = new Dictionary<HostAndProjectId, int>();
   }
}

