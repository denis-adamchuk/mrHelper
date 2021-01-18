using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class UserListLoader : BaseDataCacheLoader, IUserListLoader
   {
      public UserListLoader(string hostname, DataCacheOperator op)
         : base(op)
      {
         _hostname = hostname;
      }

      async public Task Load()
      {
         if (GlobalCache.GetUsers(_hostname) == null)
         {
            IEnumerable<User> users = await loadUsersAsync();
            if (users != null)
            {
               GlobalCache.SetUsers(_hostname, users);
            }
         }
      }

      async private Task<IEnumerable<User>> loadUsersAsync()
      {
         return await call(() => _operator.GetUsers(), "Cancelled loading users", "Cannot load users");
      }

      private readonly string _hostname;
   }
}

