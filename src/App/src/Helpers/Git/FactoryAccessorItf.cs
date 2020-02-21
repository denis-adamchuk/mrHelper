using System;
using System.Threading.Tasks;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   public interface ILocalGitRepositoryFactoryAccessor
   {
      Task<ILocalGitRepositoryFactory> GetFactory();
   }
}

