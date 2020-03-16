using System;
using System.ComponentModel;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   internal class LocalGitRepositoryOperations : ILocalGitRepositoryOperations
   {
      internal LocalGitRepositoryOperations(string path, IExternalProcessManager operationManager)
      {
         _operationManager = operationManager;
         _path = path;
      }

      public ILocalGitRepositoryOperation CreateOperation(string name, Action<string> onGitStatusChange)
      {
         if (name == "CreateBranchFromPatch")
         {
            return new CreateBranchFromPatchOperation(_path, _operationManager, onGitStatusChange);
         }
         return null;
      }

      private string _path;
      private readonly IExternalProcessManager _operationManager;
   }
}
