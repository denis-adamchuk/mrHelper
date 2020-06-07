using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public abstract class ProjectUpdateContext
   {
      public ProjectUpdateContext(DateTime? latestChange, IEnumerable<string> sha)
      {
         LatestChange = latestChange;
         Sha = sha;
      }

      public DateTime? LatestChange { get; }
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

   public class FullUpdateContext : ProjectUpdateContext
   {
      public FullUpdateContext(DateTime latestChange, IEnumerable<string> sha) : base(latestChange, sha) { }
   }

   public class PartialUpdateContext : ProjectUpdateContext
   {
      public PartialUpdateContext(IEnumerable<string> sha) : base(null, sha) { }
   }

   public interface IProjectUpdateContextProvider
   {
      ProjectUpdateContext GetContext();
   }
}

