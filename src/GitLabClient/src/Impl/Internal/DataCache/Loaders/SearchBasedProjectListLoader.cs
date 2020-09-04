using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class SearchBasedProjectListLoader : BaseDataCacheLoader, IProjectListLoader
   {
      internal SearchBasedProjectListLoader(string hostname, DataCacheOperator op,
         InternalCacheUpdater cacheUpdater, DataCacheConnectionContext dataCacheConnectionContext)
         : base(op)
      {
         _hostname = hostname;
         _cacheUpdater = cacheUpdater;
         _dataCacheConnectionContext = dataCacheConnectionContext;
         Debug.Assert(_dataCacheConnectionContext.CustomData is SearchBasedContext);
      }

      async public Task Load()
      {
         IEnumerable<Project> projects = await loadProjectsAsync();
         _cacheUpdater.UpdateProjects(projects);
      }

      async private Task<IEnumerable<Project>> loadProjectsAsync()
      {
         return await call(() => _operator.GetProjects(), "Cancelled loading projects", "Cannot load projects");
      }

      private readonly string _hostname;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

