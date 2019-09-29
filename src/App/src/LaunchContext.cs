﻿using mrHelper.CommonTools;
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
      private readonly Process[] AllProcesses;

      public IntPtr GetWindowByCaption(string caption, bool startsWith)
      {
         foreach (Process process in AllProcesses)
         {
            IEnumerable<IntPtr> handles;
            try
            {
               handles = Win32Tools.EnumerateProcessWindowHandles(process.Id);
            }
            catch (Exception ex)
            {
               // Check if we could not obtain windows from a process
               if (!(ex is ArgumentException) || (ex is InvalidOperationException))
               {
                  throw;
               }

               continue;
            }

            StringBuilder strbTitle = new StringBuilder(255);
            foreach (IntPtr window in handles)
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
