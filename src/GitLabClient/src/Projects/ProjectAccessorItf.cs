using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   public interface IProjectAccessor
   {
      Task<IEnumerable<Project>> GetProjects();
      ISingleProjectAccessor GetSingleProjectAccessor(string projectName);
   }
}

