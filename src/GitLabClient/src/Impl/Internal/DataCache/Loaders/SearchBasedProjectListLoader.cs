using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
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
         IEnumerable<ProjectKey> projects = await loadProjectsAsync();
         _cacheUpdater.UpdateProjects(projects);
      }

      async private Task<IEnumerable<ProjectKey>> loadProjectsAsync()
      {
         IEnumerable<Project> allProjects = await call(
            () => _operator.GetProjects(), "Cancelled loading projects", "Cannot load projects");
         return allProjects.Select(x => new ProjectKey(_hostname, x.Path_With_Namespace));
      }

      private readonly string _hostname;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

