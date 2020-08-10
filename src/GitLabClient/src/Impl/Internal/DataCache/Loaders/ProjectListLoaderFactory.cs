using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal static class ProjectListLoaderFactory
   {
      internal static IProjectListLoader CreateProjectListLoader(string hostname,
         DataCacheOperator op, DataCacheConnectionContext context, InternalCacheUpdater cache)
      {
         IProjectListLoader listLoader = null;
         if (context.CustomData is ProjectBasedContext)
         {
            listLoader = new ProjectBasedProjectListLoader(op, cache, context);
         }
         else if (context.CustomData is SearchBasedContext)
         {
            listLoader = new SearchBasedProjectListLoader(hostname, op, cache, context);
         }
         return listLoader;
      }
   }
}

