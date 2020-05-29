using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;

namespace mrHelper.Client.Session
{
   internal static class MergeRequestListLoaderFactory
   {
      internal static IMergeRequestListLoader CreateMergeRequestListLoader(string hostname,
         SessionOperator op, SessionContext context, InternalCacheUpdater cache)
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

