using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Interfaces
{
   public class LocalGitCommitStorageUpdaterException : ExceptionEx
   {
      public LocalGitCommitStorageUpdaterException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class LocalGitCommitStorageUpdaterFailedException : LocalGitCommitStorageUpdaterException
   {
      public LocalGitCommitStorageUpdaterFailedException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class LocalGitCommitStorageUpdaterCancelledException : Exception {}

   /// <summary>
   /// Updates attached LocalGitRepository object
   /// </summary>
   public interface ILocalGitCommitStorageUpdater
   {
      /// <summary>
      /// Async version of Update.
      /// - Can report progress change
      /// - Can clone
      /// - Processes context in a single chunk
      /// </summary>
      Task StartUpdate(ICommitStorageUpdateContextProvider contextProvider, Action<string> onProgressChange,
         Action onUpdateStateChange);
      void StopUpdate();
      bool CanBeStopped();

      /// <summary>
      /// Non-blocking version of Update.
      /// - Cannot report progress change
      /// - Cannot clone
      /// - May split passed context in chunks and process them with delays
      /// </summary>
      void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished);
   }
}

