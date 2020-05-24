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

      public override bool Equals(object obj)
      {
         return obj is FullUpdateContext context &&
                LatestChange == context.LatestChange &&
                EqualityComparer<IEnumerable<string>>.Default.Equals(Sha, context.Sha);
      }

      public override int GetHashCode()
      {
         int hashCode = -2039341489;
         hashCode = hashCode * -1521134295 + LatestChange.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<string>>.Default.GetHashCode(Sha);
         return hashCode;
      }
   }

   public class PartialUpdateContext : IProjectUpdateContext
   {
      public PartialUpdateContext(IEnumerable<string> sha)
      {
         Sha = sha;
      }

      public IEnumerable<string> Sha { get; }

      public override bool Equals(object obj)
      {
         return obj is PartialUpdateContext context &&
                EqualityComparer<IEnumerable<string>>.Default.Equals(Sha, context.Sha);
      }

      public override int GetHashCode()
      {
         return -1761058603 + EqualityComparer<IEnumerable<string>>.Default.GetHashCode(Sha);
      }
   }

   public interface IProjectUpdateContextProvider
   {
      Task<IProjectUpdateContext> GetContext();
   }
}

