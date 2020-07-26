using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Projects
{
   public interface IProjectAccessor
   {
      Task<IEnumerable<Project>> LoadProjects();

      Task<Project> SearchProjectAsync(string projectname);

      ISingleProjectAccessor GetSingleProjectAccessor(string projectName);
   }
}

