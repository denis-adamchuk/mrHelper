using System;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Updates attached LocalGitRepository object
   /// </summary>
   public interface ILocalGitRepositoryUpdater
   {
      Task ForceUpdate(IInstantProjectChecker instantChecker, Action<string> onProgressChange);
      Task CancelUpdate();
   }
}

