using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using System.Diagnostics;

namespace mrHelper.GitClient
{
   public class GitOperationManager : IExternalProcessManager
   {
      public GitOperationManager(ISynchronizeInvoke synchronizeInvoke, string path)
      {
         _path = path;
         _externalProcessManager = new ExternalProcessManager(synchronizeInvoke);
      }

      public ExternalProcess.AsyncTaskDescriptor CreateDescriptor(
         string name, string arguments, string path, Action<string> onProgressChange)
      {
         traceOperationStatus(arguments, "start");
         try
         {
            return _externalProcessManager.CreateDescriptor(name, arguments, path, onProgressChange);
         }
         catch (ExternalProcessSystemException ex)
         {
            throw new SystemException(ex);
         }
      }

      async public Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            await _externalProcessManager.Wait(descriptor);
            checkStandardError(descriptor.StdErr);
            traceOperationStatus(descriptor.Process.StartInfo.Arguments, "end");
         }
         catch (ExternalProcessFailureException ex)
         {
            handleException("wait", ex);
         }
         catch (CancellAllInProgressException)
         {
            throw new OperationCancelledException();
         }
      }

      async public Task Join(ExternalProcess.AsyncTaskDescriptor descriptor, Action<string> onProgressChange)
      {
         traceOperationStatus("join", "start");
         try
         {
            await _externalProcessManager.Join(descriptor, onProgressChange);
            traceOperationStatus("join", "end");
         }
         catch (ExternalProcessFailureException ex)
         {
            handleException("join", ex);
         }
      }

      public Task Cancel(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         return _externalProcessManager.Cancel(descriptor);
      }

      public Task CancelAll()
      {
         return _externalProcessManager.CancelAll();
      }

      public void checkStandardError(IEnumerable<string> stdErr)
      {
         if (stdErr.Any() && stdErr.First().StartsWith("fatal:"))
         {
            string reasons =
               "Possible reasons:\n"
               + "-Git repository is not up-to-date\n"
               + "-Given commit is no longer in the repository (force push?)";
            string message = String.Format("git returned \"{0}\". {1}", stdErr.First(), reasons);
            throw new BadObjectException(message);
         }
      }

      private void handleException(string operation, ExternalProcessFailureException ex)
      {
         bool cancelled = ex.ExitCode == cancellationExitCode || ex.ExitCode == altCancellationExitCode;

         string status = cancelled ? "cancel" : "error";
         traceOperationStatus(operation, status);

         if (cancelled)
         {
            throw new OperationCancelledException();
         }
         throw new GitCallFailedException(ex);
      }

      private void traceOperationStatus(string operation, string status)
      {
         string message = String.Format("[ExternalProcessManager] async operation -- {0} -- {1} for {2}",
            status, operation, _path);
         Debug.WriteLine(message);
      }

      private static readonly int cancellationExitCode = 130;
      private static readonly int altCancellationExitCode = -1073741510;

      private readonly string _path;
      private readonly IExternalProcessManager _externalProcessManager;
   }
}

