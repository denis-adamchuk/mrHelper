using System;
using System.ComponentModel;

namespace mrHelper.GitClient
{
   public interface ILocalGitRepositoryOperations
   {
      ILocalGitRepositoryOperation CreateOperation(string name, Action<string> onGitStatusChange);
   }
}

