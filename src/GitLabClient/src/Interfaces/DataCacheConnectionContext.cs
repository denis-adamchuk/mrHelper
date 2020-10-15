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
         int? updateDiscussionsPeriod, int? updateMergeRequestsPeriod)
      {
         UpdateDiscussionsPeriod = updateDiscussionsPeriod;
         UpdateMergeRequestsPeriod = updateMergeRequestsPeriod;
      }

      public int? UpdateDiscussionsPeriod { get; }
      public int? UpdateMergeRequestsPeriod { get; }
   }

   public class DataCacheConnectionContext
   {
      public DataCacheConnectionContext(DataCacheCallbacks callbacks,
         DataCacheUpdateRules updateRules, object customData)
      {
         Callbacks = callbacks;
         UpdateRules = updateRules;
         CustomData = customData;
      }

      public DataCacheCallbacks Callbacks { get; }
      public DataCacheUpdateRules UpdateRules { get; }

      public object CustomData { get; }
   }

   public class SearchBasedContext
   {
      public SearchBasedContext(SearchCriteria searchCriteria, int? maxSearchResults)
      {
         SearchCriteria = searchCriteria;
         MaxSearchResults = maxSearchResults;
      }

      public SearchCriteria SearchCriteria { get; }
      public int? MaxSearchResults { get; }
   }
}

