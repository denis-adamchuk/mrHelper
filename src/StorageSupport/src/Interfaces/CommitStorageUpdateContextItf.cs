using System;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   public struct BaseInfo
   {
      public BaseInfo(string sha)
      {
         Sha = sha;
      }

      public string Sha { get; }
   }

   public struct HeadInfo
   {
      public HeadInfo(string sha, IEnumerable<FileInfo> files)
      {
         Sha = sha;
         Files = files;
      }

      public struct FileInfo
      {
         public FileInfo(string oldPath, string newPath)
         {
            OldPath = oldPath;
            NewPath = newPath;
         }

         public string OldPath { get; }
         public string NewPath { get; }
      }

      public string Sha { get; }
      public IEnumerable<FileInfo> Files { get; }
   }

   public struct BaseToHeadsCollection
   {
      public BaseToHeadsCollection(Dictionary<BaseInfo, IEnumerable<HeadInfo>> data)
      {
         Data = data;
      }

      public Dictionary<BaseInfo, IEnumerable<HeadInfo>> Data { get; }
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

