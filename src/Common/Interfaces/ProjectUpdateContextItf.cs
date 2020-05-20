using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IProjectUpdateContext {}

   public class FullUpdateContext : IProjectUpdateContext
   {
      public FullUpdateContext(DateTime latestChange, IEnumerable<string> sha)
      {
         LatestChange = latestChange;
         Sha = sha;
      }

      public DateTime LatestChange { get; }
      public IEnumerable<string> Sha { get; }
   }

   public class PartialUpdateContext : IProjectUpdateContext
   {
      public PartialUpdateContext(IEnumerable<string> sha)
      {
         Sha = sha;
      }

      public IEnumerable<string> Sha { get; }
   }

   public interface IProjectUpdateContextProvider
   {
      Task<IProjectUpdateContext> GetContext();
   }
}

