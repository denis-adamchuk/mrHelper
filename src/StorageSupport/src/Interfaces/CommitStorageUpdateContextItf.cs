using System;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   public abstract class CommitStorageUpdateContext
   {
      public CommitStorageUpdateContext(DateTime? latestChange, Dictionary<string, IEnumerable<string>> baseToHeads)
      {
         LatestChange = latestChange;
         BaseToHeads = baseToHeads;
      }

      public DateTime? LatestChange { get; }
      public Dictionary<string, IEnumerable<string>> BaseToHeads { get; }
   }

   public class FullUpdateContext : CommitStorageUpdateContext
   {
      public FullUpdateContext(DateTime latestChange, Dictionary<string, IEnumerable<string>> baseToHeads)
         : base(latestChange, baseToHeads) { }
   }

   public class PartialUpdateContext : CommitStorageUpdateContext
   {
      public PartialUpdateContext(Dictionary<string, IEnumerable<string>> baseToHeads) : base(null, baseToHeads) { }
   }

   public interface ICommitStorageUpdateContextProvider
   {
      CommitStorageUpdateContext GetContext();
   }
}

