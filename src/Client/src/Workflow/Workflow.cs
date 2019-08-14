using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : Exception {}

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
               await switchHostAsync(State.HostName);
            }
            else if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
               // emulate project change to reload merge request list
               await switchProjectAsync(State.Project.Path_With_Namespace);
            }
         };
         _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
      }

      async public Task SwitchHostAsync(string hostName)
      {
         await switchHostAsync(hostName);
      }

      async public Task<User> GetCurrentUser()
      {
         try
         {
            return await Operator?.GetCurrentUser();
         }
         catch (OperatorException)
         {
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

      public EventHandler<WorkflowState> HostSwitched;
      public EventHandler<WorkflowState> ProjectSwitched;
      public EventHandler<WorkflowState> MergeRequestSwitched;

      public WorkflowState State { get; private set; } = new WorkflowState();

      async private Task switchHostAsync(string hostName)
      {
         if (hostName == String.Empty)
         {
            return;
         }

         Operator = new WorkflowDataOperator(hostName, Tools.GetAccessToken(hostName, Settings));

         try
         {
            List<Project> projects = await Operator.GetProjectsAsync(hostName, Settings.ShowPublicOnly);
         }
         catch (OperatorException)
         {
            throw new WorkflowException();
         }

         State = new WorkflowState();
         State.HostName = hostName;
         State.Projects = projects;
         HostSwitched?.Invoke(State);

         string projectName = selectProjectFromList();
         if (projectName != null)
         {
            await switchProjectAsync(projectName);
         }
      }

      async private Task<Project> switchProjectAsync(string projectName)
      {
         if (projectName == String.Empty)
         {
            return;
         }

         try
         {
            Project project = await Operator.GetProjectAsync(projectName);
            List<MergeRequest> mergeRequests = await Operator.GetMergeRequestsAsync(project, Settings.CheckedLabelsFilter);
         }
         catch (OperatorException)
         {
            throw new WorkflowException();
         }

         State.Project = project;
         State.MergeRequests = mergeRequests;
         ProjectSwitched?.Invoke(State);

         int? iid = selectMergeRequestFromList();
         if (iid.HasValue)
         {
            await switchMergeRequestAsync(iid.Value);
         }
      }

      async private Task<MergeRequest> switchMergeRequestAsync(int mergeRequestIId)
      {
         try
         {
            MergeRequest mergeRequest = await Operator.GetMergeRequestAsync(mergeRequestIId);
            List<Version> versions = await Operator.GetVersionsAsync(mergeRequestIId);
         }
         catch (OperatorException)
         {
            throw new WorkflowException();
         }

         State.MergeRequest = mergeRequest;
         State.Versions = versions;
         MergeRequestSwitched?.Invoke(State);
      }

      private string selectProjectFromList()
      {
         foreach (var project in State.Projects)
         {
            if (project.Path_With_Namespace == _settings.LastSelectedProject)
            {
               return project.Path_With_Namespace;
            }
         }
         return State.Projects.Count > 0 ? State.Projects[0] : null;
      }

      private int? selectMergeRequestFromList()
      {
         // TODO We may remember IID of a MR on Project switch and then restore it here
         return State.MergeRequests.Count > 0 ? State.MergeRequests[0] : null;
      }

      private UserDefinedSettings Settings { get; }
      private WorkflowDataOperator Operator{ get; }

      private List<string> _cachedLabels = null;
   }
}

