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

   public class SecurityException : RepositoryUpdateException
   {
      public SecurityException(Exception innerException)
         : base(String.Empty, innerException)
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
      Task ForceUpdate(IInstantProjectChecker instantChecker, Action<string> onProgressChange);
      Task CancelUpdate();
   }
}

