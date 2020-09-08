using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class UserListLoader : BaseDataCacheLoader, IUserListLoader
   {
      public UserListLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
      }

      async public Task Load()
      {
         IEnumerable<User> users = await loadUsersAsync();
         _cacheUpdater.UpdateUsers(users);
      }

      async private Task<IEnumerable<User>> loadUsersAsync()
      {
         return await call(() => _operator.GetUsers(), "Cancelled loading users", "Cannot load users");
      }

      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

