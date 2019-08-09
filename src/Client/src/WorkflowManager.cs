namespace mrHelper.Client
{
   public class WorkflowManager
   {
      public WorkflowManager(UserDefinedSettings settings)
      {
         Settings = settings;
         Settings.PropertyChange += async (sender, property) =>
         {
            if (property.PropertyName == "ShowPublicOnly")
            {
               // emulate host change to reload project list
               await switchHostAsync(State.HostName);
            }
            else if (property.PropertyName == "LastUsedLabels")
            {
               // emulate project change to reload merge request list
               await switchProjectAsync(State.Project.Name_With_Namespace);
            }
         }
      }

      async public Task InitializeAsync()
      {
         await switchHostAsync(getInitialHost());
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

      public EventHandler<GitLabState> HostSwitched;
      public EventHandler<GitLabState> ProjectSwitched;
      public EventHandler<GitLabState> MergeRequestSwitched;

      async private Task switchHostAsync(string hostName)
      {
         if (hostName == String.Empty)
         {
            return;
         }

         State = new WorkflowState();
         State.HostName = hostName;

         List<Project> projects = await loadProjectsAsync();
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

         Project project = await loadProjectAsync(projectName);
         State.Project = project;

         List<MergeRequest> mergeRequests = await loadMergeRequestsAsync(project);
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
         MergeRequest mergeRequest = await loadMergeRequestAsync(mergeRequestIId);
         State.MergeRequest = mergeRequest;

         List<Version> versions = await loadVersionsAsync(mergeRequestIId);
         MergeRequestSwitched?.Invoke(State);
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
         // TODO We may remember IID of a MR on Project switch and then restore it here
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

         GitLab gl = new GitLab(State.HostName, getAccessToken(State.HostName));
         try
         {
            return (Task<List<Project>>)waitForComplete(() =>
               return gl.Projects.LoadAllTaskAsync(
                  new ProjectsFilter
                  {
                     PublicOnly = _settings.ShowPublicOnly
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
            + State.HostName + " and project " + State.Project);

         GitLab gl = new GitLab(State.HostName, getAccessToken(State.HostName));
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete((CancellationToken ct) =>
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

         Debug.WriteLine("Loading merge request asynchronously");

         GitLab gl = new GitLab(State.HostName, getAccessToken(State.HostName));
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete((CancellationToken ct) =>
               return gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(iid).LoadTaskAsync(ct));
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

         Debug.WriteLine("Loading versions asynchronously");

         GitLab gl = new GitLab(State.HostName, getAccessToken(State.HostName));
         try
         {
            return (Task<List<MergeRequest>>)waitForComplete((CancellationToken ct) =>
               return gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(State.MergeRequest.IId).
                  Versions.LoadAllTaskAsync(ct));
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

         Debug.WriteLine("Waiting for current task cancellation");
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
            return await cmd(CurrentCancellationTokenSource.CancellationToken);
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

      private string getInitialHost()
      {
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            if (_settings.KnownHosts[iKnownHost] == _settings.LastSelectedHost)
            {
               return _settings.LastSelectedHost;
            }
         }
         return _settings.KnownHosts.Count > 0 ? _settings.KnownHosts[0] : String.Empty;
      }

      private string getAccessToken(string host)
      {
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            if (host == _settings.KnownHosts[iKnownHost])
            {
               return _settings.KnownAccessTokens[iKnownHost];
            }
         }
         return String.Empty;
      }

      public WorkflowState State { get; private set; } = new WorkflowState();

      private UserDefinedSettings Settings { get; }
      private Task<object> CurrentTask = null;
      private CancellationTokenSource CurrentCancellationTokenSource = null;
   }
}

