using mrHelper.Common.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IExternalProcessManager
   {
      ExternalProcess.AsyncTaskDescriptor CreateDescriptor(
         string name, string arguments, string path, Action<string> onProgressChange);

      Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor);

      Task Join(ExternalProcess.AsyncTaskDescriptor descriptor, Action<string> onProgressChange);

      Task Cancel(ExternalProcess.AsyncTaskDescriptor descriptor);

      Task CancelAll();
   }
}

