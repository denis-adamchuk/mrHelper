using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : Exception { }

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

      async public Task<User?> GetCurrentUser()
      {
         try
         {
            return await Operator?.GetCurrentUser();
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            if (cancelled)
            {
               return null; // silent return
            }
            throw new WorkflowException();
         }
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

      public EventHandler<string> PreSwitchHost;
      public EventHandler<WorkflowState> PostSwitchHost;
      public EventHandler<bool> FailedSwitchHost;

      public EventHandler<string> PreSwitchProject;
      public EventHandler<WorkflowState> PostSwitchProject;
      public EventHandler<bool> FailedSwitchProject;

      public EventHandler<int> PreSwitchMergeRequest;
      public EventHandler<WorkflowState> PostSwitchMergeRequest;
      public EventHandler<bool> FailedSwitchMergeRequest;

      public WorkflowState State { get; private set; } = new WorkflowState();

      async private Task switchHostAsync(string hostName)
      {
         if (hostName == String.Empty)
         {
            return;
         }

         PreSwitchHost?.Invoke(this, hostName);

         Operator?.CancelAsync();
         Operator = new WorkflowDataOperator(hostName, Tools.Tools.GetAccessToken(hostName, Settings), Settings);

         List<Project> projects = null;
         try
         {
            projects = await Operator.GetProjectsAsync(hostName, Settings.ShowPublicOnly);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            FailedSwitchHost?.Invoke(this, cancelled);
            if (cancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State = new WorkflowState();
         State.HostName = hostName;
         State.Projects = projects;

         PostSwitchHost?.Invoke(this, State);

         string projectName = selectProjectFromList();
         if (projectName != String.Empty)
         {
            await switchProjectAsync(projectName);
         }
      }

      async private Task switchProjectAsync(string projectName)
      {
         if (projectName == String.Empty)
         {
            return;
         }

         PreSwitchProject?.Invoke(this, projectName);

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
            FailedSwitchProject(this, cancelled);
            if (cancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State.Project = project;
         State.MergeRequests = mergeRequests;

         _lastProjectsByHosts[State.HostName] = State.Project.Path_With_Namespace;

         PostSwitchProject?.Invoke(this, State);

         int? iid = selectMergeRequestFromList();
         if (iid.HasValue)
         {
            await switchMergeRequestAsync(iid.Value);
         }
      }

      async private Task switchMergeRequestAsync(int mergeRequestIId)
      {
         PreSwitchMergeRequest?.Invoke(this, mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         List<GitLabSharp.Entities.Version> versions = null;
         try
         {
            mergeRequest = await Operator.GetMergeRequestAsync(State.Project.Path_With_Namespace, mergeRequestIId);
            versions = await Operator.GetVersionsAsync(State.Project.Path_With_Namespace, mergeRequestIId);
         }
         catch (OperatorException ex)
         {
            bool cancelled = ex.InternalException is GitLabClientCancelled;
            FailedSwitchMergeRequest(this, cancelled);
            if (cancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State.MergeRequest = mergeRequest;
         State.Versions = versions;

         HostAndProjectId key = new HostAndProjectId { Host = State.HostName, ProjectId = State.Project.Id };
         _lastMergeRequestsByProjects[key] = mergeRequestIId;

         PostSwitchMergeRequest?.Invoke(this, State);
      }

      private string selectProjectFromList()
      {
         string key = State.HostName;
         // if we remember a project selected for the given host before...
         if (_lastProjectsByHosts.ContainsKey(key)
            // ... and if such project still exists in a list of Projects
            && State.Projects.Any((x) => x.Path_With_Namespace == _lastProjectsByHosts[key]))
         {
            return _lastProjectsByHosts[key];
         }

         foreach (var project in State.Projects)
         {
            if (project.Path_With_Namespace == Settings.LastSelectedProject)
            {
               return project.Path_With_Namespace;
            }
         }

         return State.Projects.Count > 0 ? State.Projects[0].Path_With_Namespace : String.Empty;
      }

      private int? selectMergeRequestFromList()
      {
         HostAndProjectId key = new HostAndProjectId { Host = State.HostName, ProjectId = State.Project.Id };
         // if we remember MR selected for the given host/project before...
         if (_lastMergeRequestsByProjects.ContainsKey(key)
            // ... and if such MR still exists in a list of MRs
            && State.MergeRequests.Any((x) => x.IId == _lastMergeRequestsByProjects[key]))
         {
            return _lastMergeRequestsByProjects[key];
         }

         return State.MergeRequests.Count > 0 ? State.MergeRequests[0].IId : new Nullable<int>();
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

      private List<string> _cachedLabels = null;

      private Dictionary<string, string> _lastProjectsByHosts = new Dictionary<string, string>();
      private struct HostAndProjectId
      {
         public string Host;
         public int ProjectId;
      }
      private Dictionary<HostAndProjectId, int> _lastMergeRequestsByProjects = new Dictionary<HostAndProjectId, int>();
   }
}

