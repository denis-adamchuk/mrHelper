using System;
using System.ComponentModel;
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
      Task<LocalGitRepositoryStateData> SaveState(Action<string> onProgressChange);
      Task RestoreState(LocalGitRepositoryStateData value, Action<string> onProgressChange);
   }
}

