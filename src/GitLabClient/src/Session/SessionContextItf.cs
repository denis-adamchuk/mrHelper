using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   public interface ISessionContext { }

   public class ProjectBasedContext : ISessionContext
   {
      public IEnumerable<ProjectKey> Projects;
   }

   public class LabelBasedContext : ISessionContext
   {
      public object Search;
   }
}

