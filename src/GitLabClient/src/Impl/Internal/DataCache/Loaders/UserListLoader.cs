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
         IEnumerable<User> users = await loadUsersAsync();
         GlobalCache.SetUsers(_hostname, users);
      }

      async private Task<IEnumerable<User>> loadUsersAsync()
      {
         return await call(() => _operator.GetUsers(), "Cancelled loading users", "Cannot load users");
      }

      private readonly string _hostname;
   }
}

