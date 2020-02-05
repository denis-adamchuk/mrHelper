using System.Threading.Tasks;

namespace mrHelper.GitClient
{
   ///<summary>
   /// Creates LocalGitRepository objects.
   /// This factory is helpful because LocalGitRepository objects may have internal state that is expensive to fill up.
   ///<summary>
   public interface ILocalGitRepositoryFactory
   {
      string ParentFolder { get; }

      /// <summary>
      /// Create a LocalGitRepository object or return it if already cached.
      /// Throws if
      /// </summary>
      ILocalGitRepository GetRepository(string hostName, string projectName);
   }
}

