using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
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

      public Task Load()
      {
         IEnumerable<ProjectKey> projectKeys = loadProjects();
         _cacheUpdater.UpdateProjects(projectKeys);
         return Task.CompletedTask;
      }

      private IEnumerable<ProjectKey> loadProjects()
      {
         ProjectBasedContext pbc = (ProjectBasedContext)_dataCacheConnectionContext.CustomData;
         return pbc.Projects;
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

