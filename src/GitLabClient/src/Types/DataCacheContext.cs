using System.Collections.Generic;
using System.ComponentModel;

namespace mrHelper.GitLabClient
{
   public class DataCacheContext
   {
      public DataCacheContext(
         ISynchronizeInvoke synchronizeInvoke,
         IMergeRequestFilterChecker mergeRequestFilterChecker,
         IEnumerable<string> discussionKeywords)
      {
         SynchronizeInvoke = synchronizeInvoke;
         MergeRequestFilterChecker = mergeRequestFilterChecker;
         DiscussionKeywords = discussionKeywords;
      }

      public ISynchronizeInvoke SynchronizeInvoke { get; }
      public IMergeRequestFilterChecker MergeRequestFilterChecker { get; }
      public IEnumerable<string> DiscussionKeywords { get; }
   }
}

