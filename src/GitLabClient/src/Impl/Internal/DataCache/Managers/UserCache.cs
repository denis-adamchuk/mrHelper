﻿using System;
using System.Collections.Generic;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Managers
{
   internal class UserCache : IUserCache
   {
      internal UserCache(IUserListLoader userListLoader, DataCacheContext context, string hostname)
      {
         _userListLoader = userListLoader;
         _context = context;
         _hostname = hostname;

         _context.SynchronizeInvoke.BeginInvoke(new Action(
            async () =>
         {
            try
            {
               if (!GlobalCache.GetUsers(_hostname)?.Any() ?? false)
               {
                  await _userListLoader.Load();
               }
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
         return GlobalCache.GetUsers(_hostname) ?? Array.Empty<User>();
      }

      private readonly string _hostname;
      private readonly IUserListLoader _userListLoader;
      private readonly DataCacheContext _context;
   }
}

