using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace mrHelper.Common.Tools
{
   public class CancellAllInProgressException : Exception
   {
      public CancellAllInProgressException()
         : base(String.Format("Cannot add a new operation while CancelAll() is in progress"))
      {
      }
   }

   public class ExternalProcessManager : IExternalProcessManager
   {
      public ExternalProcessManager(ISynchronizeInvoke synchronizeInvoke)
      {
         _synchronizeInvoke = synchronizeInvoke;
      }

      public ExternalProcess.AsyncTaskDescriptor CreateDescriptor(
         string name, string arguments, string path, Action<string> onProgressChange)
      {
         return ExternalProcess.StartAsync(name, arguments, path, onProgressChange, _synchronizeInvoke);
      }

      /// <summary>
      /// Throws ExternalProcessException and CancellAllInProgressException
      /// </summary>
      async public Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         if (_isCancellingAll)
         {
            throw new CancellAllInProgressException();
         }

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

      /// <summary>
      /// Throws ExternalProcessException
      /// </summary>
      async public Task Join(ExternalProcess.AsyncTaskDescriptor descriptor, Action<string> onProgressChange)
      {
         descriptor.OnProgressChange = onProgressChange;
         await descriptor.Task;
      }

      async public Task Cancel(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         cancelOperation(descriptor);
         while (_descriptors.Contains(descriptor))
         {
            await Task.Delay(50);
         }
      }

      async public Task CancelAll()
      {
         _isCancellingAll = true;
         await cancelRepositoryOperationsAsync();
         _isCancellingAll = false;
      }

      async private Task cancelRepositoryOperationsAsync()
      {
         Trace.TraceInformation(String.Format("[ExternalProcessManager] Number of operations to cancel: {0}",
            _descriptors.Count));

         _descriptors.ForEach(x => cancelOperation(x));
         while (_descriptors.Count > 0)
         {
            await Task.Delay(50);
         }
      }

      private void cancelOperation(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         if (descriptor == null)
         {
            return;
         }

         Process p = descriptor.Process;
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

      private bool _isCancellingAll;
      private ISynchronizeInvoke _synchronizeInvoke;
      private List<ExternalProcess.AsyncTaskDescriptor> _descriptors =
         new List<ExternalProcess.AsyncTaskDescriptor>();
   }
}

