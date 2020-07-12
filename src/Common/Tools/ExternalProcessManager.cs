using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.Common.Tools
{
   public class ExternalProcessManager : IExternalProcessManager, IDisposable
   {
      public ExternalProcessManager(ISynchronizeInvoke synchronizeInvoke)
      {
         _synchronizeInvoke = synchronizeInvoke;
      }

      /// Throws ExternalProcessSystemException
      public ExternalProcess.AsyncTaskDescriptor CreateDescriptor(
         string name, string arguments, string path, Action<string> onProgressChange, int[] successCodes)
      {
         return ExternalProcess.StartAsync(name, arguments, path, onProgressChange, _synchronizeInvoke, successCodes);
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[ExternalProcessManager] Number of operations to cancel: {0}",
            _descriptors.Count));
         _descriptors.ForEach(x => cancelOperation(x));
      }

      /// <summary>
      /// Throws ExternalProcessFailureException
      /// </summary>
      async public Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         _descriptors.Add(descriptor);
         try
         {
            await descriptor.Task;
         }
         finally
         {
            descriptor.Process.Dispose();
            _descriptors.Remove(descriptor);
         }
      }

      public void Cancel(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         cancelOperation(descriptor);
      }

      private void cancelOperation(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         if (descriptor == null)
         {
            return;
         }

         descriptor.Cancelled = true;
         try
         {
            ExternalProcess.Cancel(descriptor.Process);
         }
         catch (InvalidOperationException)
         {
            // process already exited
         }
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly List<ExternalProcess.AsyncTaskDescriptor> _descriptors =
         new List<ExternalProcess.AsyncTaskDescriptor>();
   }
}

