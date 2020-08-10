using System;
using System.Collections.Generic;
using System.Linq;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
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
      }

      public IEnumerable<ProjectKey> GetProjects()
      {
         // lazy load
         IEnumerable<ProjectKey> projects = _cacheUpdater.Cache.GetAllProjects();
         if (projects == null && !_requestedList)
         {
            _requestedList = true;
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
         return projects == null ? Array.Empty<ProjectKey>() : projects;
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly IProjectListLoader _projectListLoader;
      private readonly DataCacheContext _context;
      private bool _requestedList;
   }
}

