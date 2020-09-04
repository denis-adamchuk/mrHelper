using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IProjectCache
   {
      IEnumerable<Project> GetProjects();
   }
}

