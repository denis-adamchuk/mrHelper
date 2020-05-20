using System;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   public class GitLabClientContext
   {
      public GitLabClientContext(
         ISynchronizeInvoke synchronizeInvoke,
         IHostProperties hostProperties,
         IMergeRequestFilterChecker mergeRequestFilterChecker,
         IEnumerable<string> discussionKeywords,
         int autoUpdatePeriodMs)
      {
         SynchronizeInvoke = synchronizeInvoke;
         HostProperties = hostProperties;
         MergeRequestFilterChecker = mergeRequestFilterChecker;
         DiscussionKeywords = discussionKeywords;
         AutoUpdatePeriodMs = autoUpdatePeriodMs;
      }

      public ISynchronizeInvoke SynchronizeInvoke { get; }
      public IHostProperties HostProperties { get; }
      public IMergeRequestFilterChecker MergeRequestFilterChecker { get; }
      public IEnumerable<string> DiscussionKeywords { get; }
      public int AutoUpdatePeriodMs { get; }
   }
}

