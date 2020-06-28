using System;
using System.Threading.Tasks;

namespace mrHelper.StorageSupport
{
   public class LoadFromDiskFailedException : GitDataException
   {
      public LoadFromDiskFailedException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   public interface ILocalGitCommitStorageData : IGitCommitStorageData
   {
      Task LoadFromDisk(GitDiffArguments arguments);
      Task LoadFromDisk(GitShowRevisionArguments arguments);
   }
}

