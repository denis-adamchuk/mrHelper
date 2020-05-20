using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   ///<summary>
   /// Creates LocalGitRepository objects.
   ///<summary>
   public interface ILocalGitRepositoryFactory
   {
      string ParentFolder { get; }

      /// <summary>
      /// Create a LocalGitRepository object or return it if already cached.
      /// </summary>
      ILocalGitRepository GetRepository(ProjectKey projectKey);
   }
}

