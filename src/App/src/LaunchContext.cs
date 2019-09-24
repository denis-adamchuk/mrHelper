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
         Arguments = Environment.GetCommandLineArgs();

         CurrentProcess = Process.GetCurrentProcess();
         AllProcesses = Process.GetProcessesByName(CurrentProcess.ProcessName);

         IsRunningSingleInstance = AllProcesses.Length == 1;
      }

      public Process CurrentProcess;
      public bool IsRunningSingleInstance;
      public string[] Arguments;
      private Process[] AllProcesses;

      public IntPtr GetWindowByCaption(string caption, bool startsWith)
      {
         foreach (Process process in AllProcesses)
         {
            StringBuilder strbTitle = new StringBuilder(255);
            foreach (IntPtr window in Win32Tools.EnumerateProcessWindowHandles(process.Id))
            {
               int nLength = NativeMethods.GetWindowText(window, strbTitle, strbTitle.Capacity + 1);
               string strTitle = strbTitle.ToString();
               if ((startsWith && strTitle.StartsWith(caption)) || (!startsWith && strTitle == caption))
               {
                  return window;
               }
            }
         }

         return IntPtr.Zero;
      }
   };
}

