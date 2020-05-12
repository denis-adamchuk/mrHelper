using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;

namespace mrHelper.Client.Session
{
   internal static class MergeRequestListLoaderFactory
   {
      internal static IMergeRequestListLoader CreateMergeRequestListLoader(
         GitLabClientContext clientContext, SessionOperator op,
         ISessionContext context, InternalCacheUpdater cache)
      {
         IVersionLoader versionLoader = new VersionLoader(op, cache);

         IMergeRequestListLoader listLoader = null;
         if (context is ProjectBasedContext)
         {
            listLoader = new ProjectBasedMergeRequestLoader(clientContext, op, versionLoader, cache);
         }
         else if (context is LabelBasedContext)
         {
            listLoader = new LabelBasedMergeRequestLoader(clientContext, op, versionLoader, cache);
         }
         return listLoader;
      }
   }
}

