using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Loaders.Cache;

namespace mrHelper.GitLabClient.Managers
{
   internal class UserCache : IUserCache
   {
      internal UserCache(InternalCacheUpdater cacheUpdater,
         IUserListLoader userListLoader, DataCacheContext context)
      {
         _cacheUpdater = cacheUpdater;
         _userListLoader = userListLoader;
         _context = context;

         _context.SynchronizeInvoke.BeginInvoke(new Action(
            async () =>
         {
            try
            {
               await _userListLoader.Load();
            }
            catch (BaseLoaderException ex)
            {
               if (ex is BaseLoaderCancelledException)
               {
                  return;
               }
               ExceptionHandlers.Handle("Cannot load list of users", ex);
            }
         }), null);
      }

      public IEnumerable<User> GetUsers()
      {
         return _cacheUpdater.Cache.GetAllUsers() ?? Array.Empty<User>();
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly IUserListLoader _userListLoader;
      private readonly DataCacheContext _context;
   }
}

