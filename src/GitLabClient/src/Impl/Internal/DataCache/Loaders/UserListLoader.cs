using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class UserListLoader : BaseDataCacheLoader, IUserListLoader, IDisposable
   {
      public UserListLoader(string hostname, DataCacheOperator op, AvatarLoader avatarLoader)
         : base(op)
      {
         _hostname = hostname;
         _avatarLoader = avatarLoader;
      }

      public void Dispose()
      {
         _avatarLoader?.Dispose();
      }

      async public Task Load()
      {
         if (GlobalCache.GetUsers(_hostname) == null)
         {
            IEnumerable<User> users = await loadUsersAsync();
            if (users != null)
            {
               GlobalCache.SetUsers(_hostname, users);
               await _avatarLoader.LoadAvatars(users);
            }
         }
      }

      async private Task<IEnumerable<User>> loadUsersAsync()
      {
         if (!_loading.Add(_hostname))
         {
            return null;
         }
         try
         {
            return await call(() => _operator.GetUsers(), "Cancelled loading users", "Cannot load users");
         }
         finally
         {
            _loading.Remove(_hostname);
         }
      }

      private readonly string _hostname;
      private AvatarLoader _avatarLoader;
      private static readonly HashSet<string> _loading = new HashSet<string>();
   }
}

