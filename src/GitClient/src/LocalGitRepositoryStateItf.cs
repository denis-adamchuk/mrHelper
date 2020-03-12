using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitClient
{
   public struct LocalGitRepositoryStateData
   {
      public string Branch;
      public string Sha;
   }

   public interface ILocalGitRepositoryState
   {
      LocalGitRepositoryStateData SaveState();
      void RestoreState(LocalGitRepositoryStateData value);
   }
}

