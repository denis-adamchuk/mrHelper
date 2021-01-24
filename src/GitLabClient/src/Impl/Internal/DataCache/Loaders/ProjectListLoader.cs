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
            if (projects != null)
            {
               GlobalCache.SetProjects(_hostname, projects);
            }
         }
      }

      async private Task<IEnumerable<Project>> loadProjectsAsync()
      {
         if (!_loading.Add(_hostname))
         {
            return null;
         }
         try
         {
            return await call(() => _operator.GetProjects(), "Cancelled loading projects", "Cannot load projects");
         }
         finally
         {
            _loading.Remove(_hostname);
         }
      }

      private readonly string _hostname;
      private static HashSet<string> _loading = new HashSet<string>();
   }
}

