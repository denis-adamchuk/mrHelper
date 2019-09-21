using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.Core.Interprocess;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Matching;

namespace mrHelper.App
{
   internal static class Program
   {
      static readonly string mutex_guid = "{5e9e9467-835f-497d-83de-77bdf4cfc2f1}";

      private static void HandleUnhandledException(Exception ex)
      {
         MessageBox.Show("Fatal error occurred, see details in logs",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
         Application.Exit();
      }

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         var arguments = Environment.GetCommandLineArgs();
         if (arguments.Length < 2)
         {
            using (Mutex mutex = new Mutex(false, "Global\\" + mutex_guid))
            {
               if (!mutex.WaitOne(0, false))
               {
                  return;
               }

               Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
               setupTraceListener("mrHelper.main.log");

               try
               {
                  Application.Run(new MainForm());
               }
               catch (Exception ex) // whatever unhandled exception
               {
                  HandleUnhandledException(ex);
               }
            }
         }
         else if (arguments[1] == "diff")
         {
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
            setupTraceListener("mrHelper.diff.log");

            int gitPID = mrHelper.Core.Interprocess.Helpers.GetGitParentProcessId(Process.GetCurrentProcess().Id);
            string[] argumentsEx = new string[arguments.Length + 1];
            Array.Copy(arguments, 0, argumentsEx, 0, arguments.Length);
            argumentsEx[argumentsEx.Length - 1] = gitPID.ToString();

            int mainInstancePID = ParentProcessUtilities.GetParentProcess(gitPID).Id;
            if (mainInstancePID == -1)
            {
               Debug.Assert(false);
               MessageBox.Show("Merge Request Helper is not running. Discussion cannot be created", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               return;
            }

            string message = String.Join("|", argumentsEx);
            Win32Tools.SendMessageToProcess(mainInstancePID, message);
         }
         else if (arguments[1] == "open")
         {
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
            setupTraceListener("mrHelper.open.log");

            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length == 1)
            {
               // Main instance not started yet
               throw new NotImplementedException("TODO Launch main instance");
            }
            else
            {
               Debug.Assert(processes.Length == 2);

               int mainInstancePID = processes[0].Id == currentProcess.Id ? processes[1].Id : processes[0].Id;
               if (mainInstancePID == -1)
               {
                  Debug.Assert(false);
                  MessageBox.Show("Merge Request Helper is not running. Discussion cannot be created", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               string message = String.Join("|", arguments);
               Win32Tools.SendMessageToProcess(mainInstancePID, message);
            }
         }
      }

      private static void setupTraceListener(string logfilename)
      {
         string logFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mrHelper", logfilename);
         Trace.Listeners.Add(new CustomTraceListener(logFilePath));
         Trace.AutoFlush = true;
      }
   }
}

