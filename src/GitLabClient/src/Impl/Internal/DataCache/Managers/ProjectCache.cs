using System;
using System.Collections.Generic;
using System.Linq;
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
         if (!_cacheUpdater.Cache.GetAllProjects().Any() && !_requestedList)
         {
            _requestedList = true;
            _context.SynchronizeInvoke.BeginInvoke(new Action(async () => await _projectListLoader.Load()), null);
         }
         return _cacheUpdater.Cache.GetAllProjects();
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly IProjectListLoader _projectListLoader;
      private readonly DataCacheContext _context;
      private bool _requestedList;
   }
}

