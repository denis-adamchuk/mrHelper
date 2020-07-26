using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   internal class ProjectAccessor : IProjectAccessor
   {
      internal ProjectAccessor(IHostProperties settings, string hostname,
         ModificationNotifier modificationNotifier)
      {
         _settings = settings;
         _hostname = hostname;
         _modificationNotifier = modificationNotifier;
      }

      public Task<IEnumerable<Project>> LoadProjects()
      {
         // TODO Project list changes very rarely and must be cached
         ProjectOperator projectOperator = new ProjectOperator(_hostname, _settings);
         return projectOperator.GetProjects();
      }

      public Task<Project> SearchProjectAsync(string projectname)
      {
         ProjectOperator projectOperator = new ProjectOperator(_hostname, _settings);
         try
         {
            return projectOperator.SearchProjectAsync(projectname);
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      public ISingleProjectAccessor GetSingleProjectAccessor(string projectName)
      {
         return new SingleProjectAccessor(new ProjectKey(_hostname, projectName), _settings, _modificationNotifier);
      }

      private readonly IHostProperties _settings;
      private readonly string _hostname;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

