using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitClient
{
   public class LoadFromDiskFailedException : GitDataException
   {
      public LoadFromDiskFailedException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   public interface ILocalGitRepositoryData : IGitRepositoryData
   {
      Task LoadFromDisk(GitDiffArguments arguments);
      Task LoadFromDisk(GitShowRevisionArguments arguments);
   }
}

