using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IProjectUpdateContext {}

   public class FullUpdateContext : IProjectUpdateContext
   {
      public DateTime LatestChange;
      public IEnumerable<string> Sha;
   }

   public class PartialUpdateContext : IProjectUpdateContext
   {
      public IEnumerable<string> Sha;
   }

   public interface IProjectUpdateContextProvider
   {
      Task<IProjectUpdateContext> GetContext();
   }
}

