using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   internal class LocalGitRepositoryState : ILocalGitRepositoryState
   {
      internal LocalGitRepositoryState(string path, ISynchronizeInvoke synchronizeInvoke)
      {
         _path = path;
         _synchronizeInvoke = synchronizeInvoke;
      }

      async public Task<LocalGitRepositoryStateData> SaveState(Action<string> onGitStatusChange)
      {
         IEnumerable<string> revParseOutput = ExternalProcess.Start("git", "rev-parse HEAD", true, _path).StdOut;
         string sha = revParseOutput.Any() ? revParseOutput.First() : String.Empty;

         IEnumerable<string> revParseOutput2 = ExternalProcess.Start(
            "git", "rev-parse --abbrev-ref HEAD", true, _path).StdOut;
         string branch = revParseOutput2.Any() ? revParseOutput2.First() : String.Empty;

         await ExternalProcess.StartAsync("git", "stash push -u", _path, onGitStatusChange, _synchronizeInvoke).Task;
         onGitStatusChange(String.Empty);

         return new LocalGitRepositoryStateData
         {
            Branch = branch,
            Sha = sha
         };
      }

      async public Task RestoreState(LocalGitRepositoryStateData value, Action<string> onGitStatusChange)
      {
         // Checkout a branch that we had before
         if (value.Branch == "HEAD") // detached HEAD
         {
            await ExternalProcess.StartAsync("git", "checkout --progress origin", _path,
               onGitStatusChange, _synchronizeInvoke).Task;
            string resetArgs = String.Format("reset --soft {0}", value.Sha);
            ExternalProcess.Start("git", resetArgs, true, _path);
         }
         else
         {
            string checkoutArgs = String.Format("checkout --progress -B {0} {1}", value.Branch, value.Sha);
            await ExternalProcess.StartAsync("git", checkoutArgs, _path, onGitStatusChange, _synchronizeInvoke).Task;
         }

         await ExternalProcess.StartAsync("git", "stash pop", _path, onGitStatusChange, _synchronizeInvoke).Task;

         onGitStatusChange(String.Empty);
      }

      private readonly string _path;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
   }
}
