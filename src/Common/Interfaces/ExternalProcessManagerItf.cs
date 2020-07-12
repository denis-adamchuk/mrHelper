using System;
using System.Threading.Tasks;
using mrHelper.Common.Tools;

namespace mrHelper.Common.Interfaces
{
   public interface IExternalProcessManager
   {
      ExternalProcess.AsyncTaskDescriptor CreateDescriptor(
         string name, string arguments, string path, Action<string> onProgressChange, int[] successCodes);

      Task Wait(ExternalProcess.AsyncTaskDescriptor descriptor);

      void Cancel(ExternalProcess.AsyncTaskDescriptor descriptor);
   }
}

