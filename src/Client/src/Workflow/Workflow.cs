using System;
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
            if (ex.InternalException is GitLabClientCancelled)
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
         await Operator?.CancelAsync();
      }

      public void Dispose()
      {
         Operator?.Dispose();
      }

      public EventHandler BeforeSwitchHost;
      public EventHandler<WorkflowState> AfterHostSwitched;
      public EventHandler FailedSwitchHost;

      public EventHandler BeforeSwitchProject;
      public EventHandler<WorkflowState> AfterProjectSwitched;
      public EventHandler FailedSwitchProject;

      public EventHandler<int> BeforeSwitchMergeRequest;
      public EventHandler<WorkflowState> AfterMergeRequestSwitched;
      public EventHandler FailedSwitchMergeRequest;

      public WorkflowState State { get; private set; } = new WorkflowState();

      async private Task switchHostAsync(string hostName)
      {
         if (hostName == String.Empty)
         {
            return;
         }

         BeforeSwitchHost?.Invoke(this, null);

         Operator = new WorkflowDataOperator(hostName, Tools.Tools.GetAccessToken(hostName, Settings), Settings);

         List<Project> projects = null;
         try
         {
            projects = await Operator.GetProjectsAsync(hostName, Settings.ShowPublicOnly);
         }
         catch (OperatorException ex)
         {
            FailedSwitchHost?.Invoke(this, null);
            if (ex.InternalException is GitLabClientCancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State = new WorkflowState();
         State.HostName = hostName;
         State.Projects = projects;

         AfterHostSwitched?.Invoke(this, State);

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

         BeforeSwitchProject?.Invoke(this, null);

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
            FailedSwitchProject(this, null);
            if (ex.InternalException is GitLabClientCancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State.Project = project;
         State.MergeRequests = mergeRequests;

         AfterProjectSwitched?.Invoke(this, State);

         int? iid = selectMergeRequestFromList();
         if (iid.HasValue)
         {
            await switchMergeRequestAsync(iid.Value);
         }
      }

      async private Task switchMergeRequestAsync(int mergeRequestIId)
      {
         BeforeSwitchMergeRequest?.Invoke(this, mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         List<GitLabSharp.Entities.Version> versions = null;
         try
         {
            mergeRequest = await Operator.GetMergeRequestAsync(State.Project.Path_With_Namespace, mergeRequestIId);
            versions = await Operator.GetVersionsAsync(State.Project.Path_With_Namespace, mergeRequestIId);
         }
         catch (OperatorException ex)
         {
            FailedSwitchMergeRequest(this, null);
            if (ex.InternalException is GitLabClientCancelled)
            {
               return; // silent return
            }
            throw new WorkflowException();
         }

         State.MergeRequest = mergeRequest;
         State.Versions = versions;

         AfterMergeRequestSwitched?.Invoke(this, State);
      }

      private string selectProjectFromList()
      {
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
         // TODO We may remember IID of a MR on Project switch and then restore it here
         return State.MergeRequests.Count > 0 ? State.MergeRequests[0].IId : new Nullable<int>();
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator { get; set; }

      private List<string> _cachedLabels = null;
   }
}

