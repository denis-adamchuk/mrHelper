using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   internal class ProjectAccessor : IProjectAccessor
   {
      internal ProjectAccessor(IHostProperties settings, string hostname)
      {
         _settings = settings;
         _hostname = hostname;
      }

      public Task<IEnumerable<Project>> GetProjects()
      {
         // TODO Project list changes very rarely and must be cached
         _operator?.Dispose();
         _operator = new ProjectAccessorOperator(_hostname, _settings);
         return _operator.GetProjects();
      }

      public ISingleProjectAccessor GetSingleProjectAccessor(string projectName)
      {
         return new SingleProjectAccessor(new ProjectKey(_hostname, projectName), _settings);
      }

      private readonly IHostProperties _settings;
      private readonly string _hostname;
      private ProjectAccessorOperator _operator;
   }
}

