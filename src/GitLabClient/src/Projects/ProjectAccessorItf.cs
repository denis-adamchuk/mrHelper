using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Projects
{
   public interface IProjectAccessor
   {
      Task<IEnumerable<Project>> LoadProjects();

      ISingleProjectAccessor GetSingleProjectAccessor(string projectName);
   }
}

