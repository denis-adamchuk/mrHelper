using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class ProjectListLoader : BaseDataCacheLoader, IProjectListLoader
   {
      internal ProjectListLoader(string hostname, DataCacheOperator op,
         DataCacheConnectionContext dataCacheConnectionContext)
         : base(op)
      {
         _hostname = hostname;
         _dataCacheConnectionContext = dataCacheConnectionContext;
         Debug.Assert(_dataCacheConnectionContext.CustomData is SearchBasedContext);
      }

      async public Task Load()
      {
         IEnumerable<Project> projects = await loadProjectsAsync();
         GlobalCache.SetProjects(_hostname, projects);
      }

      async private Task<IEnumerable<Project>> loadProjectsAsync()
      {
         return await call(() => _operator.GetProjects(), "Cancelled loading projects", "Cannot load projects");
      }

      private readonly string _hostname;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

