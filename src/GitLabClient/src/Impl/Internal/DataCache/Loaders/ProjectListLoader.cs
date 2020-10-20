using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class ProjectListLoader : BaseDataCacheLoader, IProjectListLoader
   {
      internal ProjectListLoader(string hostname, DataCacheOperator op)
         : base(op)
      {
         _hostname = hostname;
      }

      async public Task Load()
      {
         if (GlobalCache.GetProjects(_hostname) == null)
         {
            IEnumerable<Project> projects = await loadProjectsAsync();
            GlobalCache.SetProjects(_hostname, projects);
         }
      }

      async private Task<IEnumerable<Project>> loadProjectsAsync()
      {
         return await call(() => _operator.GetProjects(), "Cancelled loading projects", "Cannot load projects");
      }

      private readonly string _hostname;
   }
}

