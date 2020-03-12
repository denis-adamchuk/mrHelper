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

      public ILocalGitRepositoryOperation CreateOperation(string name)
      {
         if (name == "CreateBranchFromPatch")
         {
            return new CreateBranchFromPatchOperation(_path, _operationManager);
         }
         return null;
      }

      private string _path;
      private readonly IExternalProcessManager _operationManager;
   }
}
