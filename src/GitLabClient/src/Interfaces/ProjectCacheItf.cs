using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public interface IProjectCache
   {
      IEnumerable<ProjectKey> GetProjects();
   }
}

