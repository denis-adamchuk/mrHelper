namespace mrHelper.Client
{
   public class WorkflowManager
   {
      public WorkflowManager(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      async public Task SwitchHostAsync(string hostName, string accessToken)
      {
         await switchHostAsync(hostName, accessToken);
      }

      async public Task SwitchProjectAsync(string projectName)
      {
         await switchProjectAsync(projectName);
      }

      async public Task SwitchMergeRequestAsync(int mergeRequestIId)
      {
         await switchMergeRequestAsync(mergeRequestIId);
      }

      public EventHandler<GitLabState> HostSwitched;
      public EventHandler<GitLabState> ProjectSwitched;
      public EventHandler<GitLabState> MergeRequestSwitched;

      async private Task switchHostAsync(string hostName, string accessToken)
      {
         State = new WorkflowState();
         State.Host = new WorkflowState.Host { Name = hostName, AccessToken = accessToken, Projects = null };

         List<Project> projects = await loadProjectsAsync();
         State.Host.Projects = projects;
         ProjectsLoaded?.Invoke(State);

         string projectName = selectProjectFromList();
         if (projectName != null)
         {
            await switchProjectAsync(projectName);
         }
      }

      async private Task<Project> switchProjectAsync(string projectName)
      {
         Project project = await loadProjectAsync(projectName);
         State.Project = project;

         List<MergeRequest> mergeRequests = await loadMergeRequestsAsync(project);
         State.MergeRequests = mergeRequests;
         MergeRequestsLoaded?.Invoke(State);

         int? iid = selectMergeRequestFromList();
         if (iid.HasValue)
         {
            await switchMergeRequestAsync(iid.Value);
         }
      }

      async private Task<MergeRequest> switchMergeRequestAsync(int mergeRequestIId)
      {
         MergeRequest mergeRequest = await loadMergeRequestAsync(mergeRequestIId);
         State.MergeRequest = mergeRequest;
         MergeRequestLoaded?.Invoke(State);

         List<Version> versions = await loadVersionsAsync(mergeRequestIId);
         VersionsLoaded?.Invoke(State);
      }

      async private string selectProjectFromList()
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

      async private int? selectMergeRequestFromList()
      {
         return State.MergeRequests.Count > 0 ? State.MergeRequests[0] : null;
      }

      private delegate object command();

      async private Task<List<Project>> loadProjectsAsync()
      {
         await waitForCancel();

         List<Project> projects = Tools.LoadProjectsFromFile();
         if (projects != null && projects.Count != 0)
         {
            Debug.WriteLine("Project list is read from file");
            return projects;
         }

         Debug.WriteLine("Loading projects asynchronously for host " + hostName);

         GitLab gl = new GitLab(State.HostName, State.AccessToken);
         try
         {
            return (Task<List<Project>>)waitForComplete(() =>
               return gl.Projects.LoadAllTaskAsync(
                  new ProjectsFilter
                  {
                     PublicOnly = publicOnly
                  });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab");
         }
         return null;
      }

      async private Task<List<MergeRequest>> loadMergeRequestsAsync(string hostName, string projectName)
      {
         await waitForCancel();

         Debug.WriteLine("Loading project merge requests asynchronously for host "
            + GetCurrentHostName() + " and project " + GetCurrentProjectName());

         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete(() =>
               return gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.LoadAllTaskAsync(
                  new MergeRequestsFilter()));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
         }
         return null;
      }

      async private Task<MergeRequest?> loadMergeRequestAsync(int iid)
      {
         await waitForCancel();

         Debug.WriteLine("Loading merge request asynchronously for host ");

         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete(() =>
               return gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(iid).LoadTaskAsync());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
         }
         return null;
      }

      async private Task<List<Version>> loadVersionsAsync()
      {
         await waitForCancel();

         Debug.WriteLine("Loading versions asynchronously for host ");

         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete(() =>
               return gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(State.MergeRequest.IId).
                  Versions.LoadAllTaskAsync(), GitLabTaskType.Versions);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request versions from GitLab");
         }
         return null;
      }

      async private Task waitForCancel()
      {
         if (CurrentCancellationTokenSource == null)
         {
            return;
         }

         Debug.Assert(CurrentTask != null);
         CurrentCancellationTokenSource.Cancel();

         Debug.WriteLine("Waiting for current task cancellation ");
         try
         {
            await CurrentTask;
         }
         catch (OperationCanceledException)
         {
         }
         finally
         {
            CurrentTask = null;
            CurrentCancellationTokenSource.Dispose();
            CurrentCancellationTokenSource = null;
         }
      }

      async private Task<object> waitForComplete(command cmd)
      {
         Debug.Assert(CurrentCancellationTokenSource == null);
         Debug.Assert(CurrentTask == null);

         CurrentTask = task;
         CurrentCancellationTokenSource = new CancellationTokenSource();

         try
         {
            return await cmd();
         }
         catch (OperationCanceledException)
         {
            Debug.Assert(false);
         }
         catch (GitLabRequestException)
         {
            throw;
         }
         finally
         {
            CurrentTask = null;
            CurrentCancellationTokenSource.Dispose();
            CurrentCancellationTokenSource = null;
         }
         return null;
      }

      public WorkflowState State { get; private set; } = new WorkflowState();

      private UserDefinedSettings Settings { get; }
      private Task<object> CurrentTask = null;
      private CancellationTokenSource CurrentCancellationTokenSource = null;
   }
}

