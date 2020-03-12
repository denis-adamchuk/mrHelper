using System;
using System.Collections.Generic;
using System.Linq;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   internal class LocalGitRepositoryState : ILocalGitRepositoryState
   {
      internal LocalGitRepositoryState(string path)
      {
         _path = path;
      }

      public LocalGitRepositoryStateData SaveState()
      {
         IEnumerable<string> revParseOutput = ExternalProcess.Start(
            "git", "rev-parse HEAD", true, _path).StdOut;
         string sha = revParseOutput.Any() ? revParseOutput.First() : String.Empty;

         IEnumerable<string> revParseOutput2 = ExternalProcess.Start(
            "git", "rev-parse --abbrev-ref HEAD", true, _path).StdOut;
         string branch = revParseOutput2.Any() ? revParseOutput2.First() : String.Empty;

         ExternalProcess.Start("git", "stash push -u", true, _path);
         return new LocalGitRepositoryStateData
         {
            Branch = branch,
            Sha = sha
         };
      }

      public void RestoreState(LocalGitRepositoryStateData value)
      {
         // Checkout a branch that we had before
         if (value.Branch == "HEAD") // detached HEAD
         {
            ExternalProcess.Start("git", "checkout origin", true, _path);
            string resetArgs = String.Format("reset --soft {0}", value.Sha);
            ExternalProcess.Start("git", resetArgs, true, _path);
         }
         else
         {
            string checkoutArgs = String.Format("checkout -B {0} {1}", value.Branch, value.Sha);
            ExternalProcess.Start("git", checkoutArgs, true, _path);
         }

         ExternalProcess.Start("git", "stash pop", true, _path);
      }

      private string _path;
   }
}
