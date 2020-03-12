namespace mrHelper.GitClient
{
   public interface ILocalGitRepositoryOperations
   {
      ILocalGitRepositoryOperation CreateOperation(string name);
   }
}

