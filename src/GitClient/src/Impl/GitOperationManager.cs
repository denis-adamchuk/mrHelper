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
         return _externalProcessManager.CreateDescriptor(name, arguments, path, onProgressChange);
      }

      async public Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            await _externalProcessManager.Wait(descriptor);
            checkStandardError(descriptor.StdErr);
            traceOperationStatus(descriptor.Process.StartInfo.Arguments, "end");
         }
         catch (ExternalProcessException ex)
         {
            traceOperationStatusOnException("wait", ex);
            throw new GitOperationException(ex.Command, ex.ExitCode, ex.Errors, isCancelled(ex));
         }
         catch (CancellAllInProgressException)
         {
            Debug.Assert(false);
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
         catch (ExternalProcessException ex)
         {
            traceOperationStatusOnException("join", ex);
            throw new GitOperationException(ex.Command, ex.ExitCode, ex.Errors, isCancelled(ex));
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
         if (stdErr.Count() > 0 && stdErr.First().StartsWith("fatal:"))
         {
            string reasons =
               "Possible reasons:\n"
               + "-Git repository is not up-to-date\n"
               + "-Given commit is no longer in the repository (force push?)";
            string message = String.Format("git returned \"{0}\". {1}", stdErr.First(), reasons);
            throw new GitObjectException(message, 0);
         }
      }

      private void traceOperationStatusOnException(string operation, ExternalProcessException ex)
      {
         string status = isCancelled(ex) ? "cancel" : "error";
         traceOperationStatus(operation, status, false);

         string meaning = isCancelled(ex) ? "cancelled" : "failed";
         ExceptionHandlers.Handle(ex, String.Format("Git operation {0}", meaning));
      }

      private void traceOperationStatus(string operation, string status, bool debugOnly = true)
      {
         string message = String.Format("[ExternalProcessManager] async operation -- {0} -- {1} for {2}",
            status, operation, _path);
         if (debugOnly)
         {
            Debug.WriteLine(message);
         }
         else
         {
            Trace.TraceInformation(message);
         }
      }

      private bool isCancelled(ExternalProcessException ex)
      {
         return ex.ExitCode == cancellationExitCode || ex.ExitCode == altCancellationExitCode;
      }

      private static readonly int cancellationExitCode = 130;
      private static readonly int altCancellationExitCode = -1073741510;

      private string _path;
      private IExternalProcessManager _externalProcessManager;
   }
}

