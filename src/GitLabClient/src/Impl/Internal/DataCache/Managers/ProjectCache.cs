using System;
using System.Collections.Generic;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Loaders.Cache;

namespace mrHelper.GitLabClient.Managers
{
   internal class ProjectCache : IProjectCache
   {
      internal ProjectCache(InternalCacheUpdater cacheUpdater,
         IProjectListLoader projectListLoader, DataCacheContext context)
      {
         _cacheUpdater = cacheUpdater;
         _projectListLoader = projectListLoader;
         _context = context;

         _context.SynchronizeInvoke.BeginInvoke(new Action(
            async () =>
         {
            try
            {
               await _projectListLoader.Load();
            }
            catch (BaseLoaderException ex)
            {
               if (ex is BaseLoaderCancelledException)
               {
                  return;
               }
               ExceptionHandlers.Handle("Cannot load list of projects", ex);
            }
         }), null);
      }

      public IEnumerable<Project> GetProjects()
      {
         return _cacheUpdater.Cache.GetAllProjects() ?? Array.Empty<Project>();
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly IProjectListLoader _projectListLoader;
      private readonly DataCacheContext _context;
   }
}

