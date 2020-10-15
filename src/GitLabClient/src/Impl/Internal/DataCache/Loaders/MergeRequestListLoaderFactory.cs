using System.Linq;
using System.Diagnostics;
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
         if (context.CustomData is SearchBasedContext sbc)
         {
            if (sbc.SearchCriteria.Criteria.All(criteria => criteria is SearchByProject))
            {
               listLoader = new ProjectBasedMergeRequestLoader(
                  op, versionLoader, cache, context);
            }
            else
            {
               listLoader = new SearchBasedMergeRequestLoader(hostname,
                  op, versionLoader, cache, context);
            }
         }
         else
         {
            Debug.Assert(false);
         }
         return listLoader;
      }
   }
}

