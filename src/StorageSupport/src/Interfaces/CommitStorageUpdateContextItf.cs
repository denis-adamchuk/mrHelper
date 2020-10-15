using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.StorageSupport
{
   public struct BaseToHeadsCollection
   {
      public struct CommitInfo
      {
         public CommitInfo(string sha)
         {
            Sha = sha;
         }

         public string Sha { get; }
      }

      public struct RelativeFileInfo
      {
         public RelativeFileInfo(string oldPath, string newPath)
         {
            OldPath = oldPath;
            NewPath = newPath;
         }

         public string OldPath { get; }
         public string NewPath { get; }
      }

      public struct RelativeCommitInfo
      {
         public RelativeCommitInfo(string sha, IEnumerable<RelativeFileInfo> files)
         {
            Sha = sha;
            Files = files;
         }

         public string Sha { get; }
         public IEnumerable<RelativeFileInfo> Files { get; }
      }

      public BaseToHeadsCollection(Dictionary<CommitInfo, IEnumerable<RelativeCommitInfo>> data)
      {
         Data = data;
      }

      public Dictionary<CommitInfo, IEnumerable<RelativeCommitInfo>> Data { get; }

      public struct FlatBaseToHeadInfo
      {
         public FlatBaseToHeadInfo(CommitInfo @base, CommitInfo head, IEnumerable<RelativeFileInfo> files)
         {
            Base = @base;
            Head = head;
            Files = files;
         }

         public CommitInfo Base;
         public CommitInfo Head;
         public IEnumerable<RelativeFileInfo> Files;
      }

      public IEnumerable<FlatBaseToHeadInfo> Flatten()
      {
         return Data
            .SelectMany(
               (x) => x.Value,
               (kv, head) => new FlatBaseToHeadInfo(kv.Key, new CommitInfo(head.Sha), head.Files))
            .ToList();
      }
   }

   public abstract class CommitStorageUpdateContext
   {
      public CommitStorageUpdateContext(DateTime? latestChange, BaseToHeadsCollection baseToHeads)
      {
         LatestChange = latestChange;
         BaseToHeads = baseToHeads;
      }

      public DateTime? LatestChange { get; }
      public BaseToHeadsCollection BaseToHeads { get; }
   }

   public class FullUpdateContext : CommitStorageUpdateContext
   {
      public FullUpdateContext(DateTime latestChange, BaseToHeadsCollection baseToHeads)
         : base(latestChange, baseToHeads) { }
   }

   public class PartialUpdateContext : CommitStorageUpdateContext
   {
      public PartialUpdateContext(BaseToHeadsCollection baseToHeads) : base(null, baseToHeads) { }
   }

   public interface ICommitStorageUpdateContextProvider
   {
      CommitStorageUpdateContext GetContext();
   }
}

