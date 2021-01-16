using System;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class DataCacheCallbacks
   {
      public DataCacheCallbacks(Action<ProjectKey> onForbiddenProject,
                                Action<ProjectKey> onNotFoundProject)
      {
         OnForbiddenProject = onForbiddenProject;
         OnNotFoundProject = onNotFoundProject;
      }

      public Action<ProjectKey> OnForbiddenProject { get; }
      public Action<ProjectKey> OnNotFoundProject { get; }
   }

   public class DataCacheUpdateRules
   {
      public DataCacheUpdateRules(
         int? updateDiscussionsPeriod,
         int? updateMergeRequestsPeriod,
         bool updateOnlyOpenedMergeRequests)
      {
         UpdateDiscussionsPeriod = updateDiscussionsPeriod;
         UpdateMergeRequestsPeriod = updateMergeRequestsPeriod;
         UpdateOnlyOpenedMergeRequests = updateOnlyOpenedMergeRequests;
      }

      public int? UpdateDiscussionsPeriod { get; }
      public int? UpdateMergeRequestsPeriod { get; }
      public bool UpdateOnlyOpenedMergeRequests { get; }
   }

   public class DataCacheContext
   {
      public DataCacheContext(
         ISynchronizeInvoke synchronizeInvoke,
         IMergeRequestFilterChecker mergeRequestFilterChecker,
         IEnumerable<string> discussionKeywords,
         bool updateManagerExtendedLogging,
         string tagForLogging,
         DataCacheCallbacks callbacks,
         DataCacheUpdateRules updateRules)
      {
         SynchronizeInvoke = synchronizeInvoke;
         MergeRequestFilterChecker = mergeRequestFilterChecker;
         DiscussionKeywords = discussionKeywords;
         UpdateManagerExtendedLogging = updateManagerExtendedLogging;
         TagForLogging = tagForLogging;
         Callbacks = callbacks;
         UpdateRules = updateRules;
      }

      public ISynchronizeInvoke SynchronizeInvoke { get; }
      public IMergeRequestFilterChecker MergeRequestFilterChecker { get; }
      public IEnumerable<string> DiscussionKeywords { get; }
      public bool UpdateManagerExtendedLogging { get; }
      public string TagForLogging { get; }

      public DataCacheCallbacks Callbacks { get; }
      public DataCacheUpdateRules UpdateRules { get; }
   }
}

