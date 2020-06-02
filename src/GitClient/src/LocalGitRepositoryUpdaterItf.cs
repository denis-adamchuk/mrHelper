using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public class RepositoryUpdateException : ExceptionEx
   {
      public RepositoryUpdateException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class SSLVerificationException : RepositoryUpdateException
   {
      public SSLVerificationException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public class AuthenticationFailedException : RepositoryUpdateException
   {
      public AuthenticationFailedException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public class CouldNotReadUsernameException : RepositoryUpdateException
   {
      public CouldNotReadUsernameException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public class NotEmptyDirectoryException : RepositoryUpdateException
   {
      public NotEmptyDirectoryException(string path, Exception innerException)
         : base(path, innerException)
      {
      }
   }

   public class UpdateCancelledException : RepositoryUpdateException
   {
      public UpdateCancelledException()
         : base(String.Empty, null)
      {
      }
   }

   /// <summary>
   /// Updates attached LocalGitRepository object
   /// </summary>
   public interface ILocalGitRepositoryUpdater
   {
      /// <summary>
      /// Async version of Update.
      /// - Can report progress change
      /// - Can clone
      /// - Processes context in a single chunk
      /// </summary>
      Task Update(IProjectUpdateContextProvider contextProvider, Action<string> onProgressChange);

      /// <summary>
      /// Non-blocking version of Update.
      /// - Cannot report progress change
      /// - Cannot clone
      /// - May split passed context in chunks and process them with delays
      /// </summary>
      void RequestUpdate(IProjectUpdateContextProvider contextProvider, Action onFinished);
      void CancelUpdate();
   }
}

