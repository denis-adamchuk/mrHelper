using System;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   public interface ISessionContext
   {
      bool AreDiscussionUpdatesEnable();
      bool AreMergeRequestUpdatesEnabled();
   }

   public class ProjectBasedContext : ISessionContext
   {
      public IEnumerable<ProjectKey> Projects;
      public Action<ProjectKey> OnForbiddenProject;
      public Action<ProjectKey> OnNotFoundProject;

      public bool AreDiscussionUpdatesEnable() => true;
      public bool AreMergeRequestUpdatesEnabled() => true;
   }

   public class SearchBasedContext : ISessionContext
   {
      public object SearchCriteria;
      public int? MaxSearchResults;
      public bool OnlyOpen;

      public bool AreDiscussionUpdatesEnable() => false;
      public bool AreMergeRequestUpdatesEnabled() => false;
   }
}

