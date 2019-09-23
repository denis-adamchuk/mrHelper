using mrHelper.Common.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App
{
   public class LaunchContext
   {
      public LaunchContext()
      {
         CurrentProcess = Process.GetCurrentProcess();
         Arguments = Environment.GetCommandLineArgs();
         MainInstance = getMainInstance();
      }

      public bool IsRunningMainInstance()
      {
         return CurrentProcess.Id == MainInstance.Id;
      }

      public Process CurrentProcess;
      public Process MainInstance;
      public string[] Arguments;

      public IntPtr GetMainWindowOfMainInstance()
      {
         StringBuilder strbTitle = new StringBuilder(255);
         foreach (IntPtr window in Win32Tools.EnumerateProcessWindowHandles(MainInstance.Id))
         {
            int nLength = NativeMethods.GetWindowText(window, strbTitle, strbTitle.Capacity + 1);
            string strTitle = strbTitle.ToString();
            if (strTitle.StartsWith(Common.Constants.Constants.MainWindowCaption))
            {
               return window;
            }
         }
         return IntPtr.Zero;
      }

      private Process getMainInstance()
      {
         Process[] processes = Process.GetProcessesByName(CurrentProcess.ProcessName);
         if (processes.Length == 1)
         {
            return CurrentProcess;
         }
         else if (processes.Length == 2)
         {
            return processes[0].Id == CurrentProcess.Id ? processes[1] : processes[0];
         }

         Debug.Assert(false);
         return null;
      }
   };
}

