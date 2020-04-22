using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   public interface ILocalGitRepositoryFactoryAccessor
   {
      ILocalGitRepositoryFactory GetFactory();
   }
}

