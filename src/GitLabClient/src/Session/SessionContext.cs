using System;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   public class SessionCallbacks
   {
      public SessionCallbacks(Action<ProjectKey> onForbiddenProject, Action<ProjectKey> onNotFoundProject)
      {
         OnForbiddenProject = onForbiddenProject;
         OnNotFoundProject = onNotFoundProject;
      }

      public Action<ProjectKey> OnForbiddenProject { get; }
      public Action<ProjectKey> OnNotFoundProject { get; }
   }

   public class SessionUpdateRules
   {
      public SessionUpdateRules(bool updateDiscussions, bool updateMergeRequests)
      {
         UpdateDiscussions = updateDiscussions;
         UpdateMergeRequests = updateMergeRequests;
      }

      public bool UpdateDiscussions { get; }
      public bool UpdateMergeRequests { get; }
   }

   public class SessionContext
   {
      public SessionContext(SessionCallbacks callbacks, SessionUpdateRules updateRules, object customData)
      {
         Callbacks = callbacks;
         UpdateRules = updateRules;
         CustomData = customData;
      }

      public SessionCallbacks Callbacks { get; }
      public SessionUpdateRules UpdateRules { get; }

      public object CustomData { get; }
   }

   public class ProjectBasedContext
   {
      public IEnumerable<ProjectKey> Projects;
   }

   public class SearchBasedContext
   {
      public object SearchCriteria;
      public int? MaxSearchResults;
      public bool OnlyOpen;
   }
}

