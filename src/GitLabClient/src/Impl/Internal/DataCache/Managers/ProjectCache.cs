using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Managers
{
   internal class ProjectCache : IProjectCache
   {
      internal ProjectCache(IProjectListLoader projectListLoader, DataCacheContext context, string hostname)
      {
         _projectListLoader = projectListLoader;
         _context = context;
         _hostname = hostname;

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
         return GlobalCache.GetProjects(_hostname) ?? Array.Empty<Project>();
      }

      private readonly string _hostname;
      private readonly IProjectListLoader _projectListLoader;
      private readonly DataCacheContext _context;
   }
}

