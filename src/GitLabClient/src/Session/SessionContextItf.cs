using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   public interface ISessionContext { }

   public class ProjectBasedContext : ISessionContext
   {
      public IEnumerable<ProjectKey> Projects;
   }

   public class SearchBasedContext : ISessionContext
   {
      public object SearchCriteria;
      public int MaxSearchResults;
      public bool OnlyOpen;
   }
}

