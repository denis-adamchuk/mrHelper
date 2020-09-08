using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IUserCache
   {
      IEnumerable<User> GetUsers();
   }
}

