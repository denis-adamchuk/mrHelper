namespace mrHelper.Client
{
   public class GitLabModelState
   {
      public struct Access
      {
         string Host;
         string AccessToken;
      }

      public Access GitLab
      {
         get;
         set
         {
            Access = value;
            Projects = null;
            Project = null;
            MergeRequest = null;
         }
      }

      public List<Project> Projects
      {
         get;
         set
         {
            Projects = value;
            Project = null;
            MergeRequest = null;
         }
      }

      public Project Project
      {
         get;
         set
         {
            Project = value;
            MergeRequest = null;
         }
      }

      public MergeRequest MergeRequest
      {
         get;
         set;
      }
   }

   public class GitLabModel
   {
      public GitLabModel(UserDefinedSettings settings)
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

      public EventHandler<GitLabState> ProjectsLoaded;
      public EventHandler<GitLabState> MergeRequestsLoaded;
      public EventHandler<GitLabState> MergeRequestLoaded;
      public EventHandler<GitLabState> VersionsLoaded;

      async private Task switchHostAsync(string hostName, string accessToken)
      {
         State = new GitLabState();
         State.Access = new { Host = hostName, AccessToken = accessToken };

         List<Project> projects = await loadProjectsAsync();
         State.Projects = projects;
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

      async private Task<List<Project>> loadProjectsAsync()
      {
         Debug.WriteLine("Loading projects asynchronously for host " + hostName);

         List<Project> projects = Tools.LoadProjectsFromFile();
         if (projects != null && projects.Count != 0)
         {
            _glTaskManager.CancelAll(GitLabTaskType.Projects);
            return projects;
         }

         GitLab gl = new GitLab(State.HostName, State.AccessToken);
         var task  = _glTaskManager.CreateTask<List<Project>>(
            gl.Projects.LoadAllTaskAsync(
               new ProjectsFilter
               {
                  PublicOnly = publicOnly
               }), GitLabTaskType.Projects);

         try
         {
            return await _glTaskManager.RunAsync(task);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab");
         }
         return null;
      }

      async private Task<List<MergeRequest>> loadMergeRequestsAsync(string hostName, string projectName)
      {
         Debug.WriteLine("Loading project merge requests asynchronously for host "
            + GetCurrentHostName() + " and project " + GetCurrentProjectName());

         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         var task = _glTaskManager.CreateTask<List<MergeRequest>>(
            gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.LoadAllTaskAsync(
               new MergeRequestsFilter()), GitLabTaskType.MergeRequests);

         try
         {
            return await _glTaskManager.RunAsync(task);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
         }
         return null;
      }

      async private Task<MergeRequest?> loadMergeRequestAsync(int iid)
      {
         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         var task = _glTaskManager.CreateTask(
            gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.
               Get(iid).LoadTaskAsync(), GitLabTaskType.MergeRequest);

         try
         {
            var result = await _glTaskManager.RunAsync(task);
            return result.Equals(default(MergeRequest)) ? null : new Nullable<MergeRequest>(result);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
         }
         return null;
      }

      async private Task<List<Version>> loadVersionsAsync()
      {
         GitLab gl = new GitLab(State.Access.Host, State.Access.AccessToken);
         var task = _glTaskManager.CreateTask<List<Version>>(
            gl.Projects.Get(State.Project.Path_With_Namespace).MergeRequests.Get(State.MergeRequest.IId).
               Versions.LoadAllTaskAsync(), GitLabTaskType.Versions);

         try
         {
            return await _glTaskManager.RunAsync(task);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request versions from GitLab");
         }
         return null;
      }

      public GitLabState State { get; private set; } = new GitLabState();
      private UserDefinedSettings Settings { get; }
   }
}

