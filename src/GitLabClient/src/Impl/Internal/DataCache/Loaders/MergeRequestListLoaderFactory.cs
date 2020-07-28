using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal static class MergeRequestListLoaderFactory
   {
      internal static IMergeRequestListLoader CreateMergeRequestListLoader(string hostname,
         DataCacheOperator op, DataCacheConnectionContext context, InternalCacheUpdater cache)
      {
         IVersionLoader versionLoader = new VersionLoader(op, cache);

         IMergeRequestListLoader listLoader = null;
         if (context.CustomData is ProjectBasedContext)
         {
            listLoader = new ProjectBasedMergeRequestLoader(
               op, versionLoader, cache, context);
         }
         else if (context.CustomData is SearchBasedContext)
         {
            listLoader = new SearchBasedMergeRequestLoader(hostname,
               op, versionLoader, cache, context);
         }
         return listLoader;
      }
   }
}

