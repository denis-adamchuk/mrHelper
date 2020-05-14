using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;

namespace mrHelper.Client.Session
{
   internal static class MergeRequestListLoaderFactory
   {
      internal static IMergeRequestListLoader CreateMergeRequestListLoader(
         GitLabClientContext clientContext, SessionOperator op,
         ISessionContext context, InternalCacheUpdater cache, bool needRaiseCallbacks)
      {
         IVersionLoader versionLoader = new VersionLoader(op, cache);

         IMergeRequestListLoader listLoader = null;
         if (context is ProjectBasedContext)
         {
            listLoader = new ProjectBasedMergeRequestLoader(
               clientContext, op, versionLoader, cache, needRaiseCallbacks);
         }
         else if (context is SearchBasedContext)
         {
            listLoader = new SearchBasedMergeRequestLoader(
               clientContext, op, versionLoader, cache);
         }
         return listLoader;
      }
   }
}

