using System.Collections.Generic;
using System.ComponentModel;

namespace mrHelper.GitLabClient
{
   public class DataCacheContext
   {
      public DataCacheContext(
         ISynchronizeInvoke synchronizeInvoke,
         IMergeRequestFilterChecker mergeRequestFilterChecker,
         IEnumerable<string> discussionKeywords,
         bool updateManagerExtendedLogging)
      {
         SynchronizeInvoke = synchronizeInvoke;
         MergeRequestFilterChecker = mergeRequestFilterChecker;
         DiscussionKeywords = discussionKeywords;
         UpdateManagerExtendedLogging = updateManagerExtendedLogging;
      }

      public ISynchronizeInvoke SynchronizeInvoke { get; }
      public IMergeRequestFilterChecker MergeRequestFilterChecker { get; }
      public IEnumerable<string> DiscussionKeywords { get; }
      public bool UpdateManagerExtendedLogging { get; }
   }
}

