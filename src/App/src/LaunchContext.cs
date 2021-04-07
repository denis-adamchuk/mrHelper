using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using mrHelper.CommonNative;

namespace mrHelper.App
{
   public class LaunchContext : IDisposable
   {
      public LaunchContext()
      {
         Arguments = Environment.GetCommandLineArgs();

         CurrentProcess = Process.GetCurrentProcess();
         AllProcesses = Process.GetProcessesByName(CurrentProcess.ProcessName);

         IsRunningSingleInstance = AllProcesses.Length == 1;
      }

      public Process CurrentProcess { get; private set; }
      public bool IsRunningSingleInstance { get; private set; }
      public string[] Arguments { get; private set; }
      private readonly Process[] AllProcesses;

      public void Dispose()
      {
         CurrentProcess?.Dispose();
         CurrentProcess = null;
      }

      public IntPtr GetWindowByCaption(string caption, bool startsWith)
      {
         foreach (Process process in AllProcesses)
         {
            IEnumerable<IntPtr> handles;
            try
            {
               handles = Win32Tools.EnumerateProcessWindowHandles(process.Id);
            }
            catch (ArgumentException)
            {
               continue;
            }
            catch (InvalidOperationException)
            {
               continue;
            }

            StringBuilder strbTitle = new StringBuilder(255);
            foreach (IntPtr window in handles)
            {
               NativeMethods.GetWindowText(window, strbTitle, strbTitle.Capacity + 1);
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

