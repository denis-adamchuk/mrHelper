using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class ProjectBasedProjectListLoader : BaseDataCacheLoader, IProjectListLoader
   {
      public ProjectBasedProjectListLoader(DataCacheOperator op,
         InternalCacheUpdater cacheUpdater, DataCacheConnectionContext dataCacheConnectionContext)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _dataCacheConnectionContext = dataCacheConnectionContext;
         Debug.Assert(_dataCacheConnectionContext.CustomData is ProjectBasedContext);
      }

      async public Task Load()
      {
         IEnumerable<Project> projects = await loadProjectsAsync();
         _cacheUpdater.UpdateProjects(projects);
      }

      async private Task<IEnumerable<Project>> loadProjectsAsync()
      {
         ProjectBasedContext pbc = (ProjectBasedContext)_dataCacheConnectionContext.CustomData;

         List<Project> projects = new List<Project>();

         Exception exception = null;
         async Task loadProject(string projectName)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               Project project = await call(() => _operator.GetProjectAsync(projectName),
                  String.Format("Cancelled loading project \"{0}\"", projectName),
                  String.Format("Cannot load project \"{0}\"", projectName));
               projects.Add(project);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(pbc.Projects, projectKey => loadProject(projectKey.ProjectName),
            () => Constants.ProjectListLoaderBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }

         return projects;
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

