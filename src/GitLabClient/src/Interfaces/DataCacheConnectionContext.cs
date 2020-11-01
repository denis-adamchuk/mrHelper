using System;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class DataCacheCallbacks
   {
      public DataCacheCallbacks(Action<ProjectKey> onForbiddenProject, Action<ProjectKey> onNotFoundProject)
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

   public class DataCacheConnectionContext
   {
      public DataCacheConnectionContext(DataCacheCallbacks callbacks,
         DataCacheUpdateRules updateRules, SearchQueryCollection queryCollection)
      {
         Callbacks = callbacks;
         UpdateRules = updateRules;
         QueryCollection = queryCollection;
      }

      public DataCacheCallbacks Callbacks { get; }
      public DataCacheUpdateRules UpdateRules { get; }
      public SearchQueryCollection QueryCollection { get; }
   }
}

