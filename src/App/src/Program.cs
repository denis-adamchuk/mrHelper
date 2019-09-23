using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.Core.Interprocess;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Matching;
using System.Text.RegularExpressions;
using System.IO;

namespace mrHelper.App
{
   internal static class Program
   {
      private static void HandleUnhandledException(Exception ex)
      {
         MessageBox.Show("Fatal error occurred, see details in logs",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
         Application.Exit();
      }

      private static readonly Regex url_re = new Regex( String.Format(
         @"^({0}:\/\/)?(http[s]?:\/\/[^:\/\s]+)\/(api\/v4\/projects\/)?(\w+\/\w+)\/merge_requests\/(\d*)",
            mrHelper.Common.Constants.Constants.CustomProtocolName), RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         LaunchContext context = new LaunchContext();

         if (context.Arguments.Length > 2)
         {
            if (context.Arguments[1] == "diff")
            {
               if (context.IsRunningMainInstance())
               {
                  MessageBox.Show("Merge Request Helper is not running. Discussion cannot be created", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               }
               else
               {
                  onLaunchFromDiffTool(context);
               }
            }
            else
            {
               MessageBox.Show("Invalid arguments", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
         }
         else
         {
            if (context.Arguments.Length == 2)
            {
               Match m = url_re.Match(context.Arguments[1]);
               if (!m.Success)
               {
                  MessageBox.Show("Invalid URL argument", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
            }

            if (context.IsRunningMainInstance())
            {
               // currently running instance is the only one, need to convert it into a main instance
               Directory.SetCurrentDirectory(Path.GetDirectoryName(context.MainInstance.MainModule.FileName));
               onLaunchMainInstace();
            }
            else
            {
               // currently running instance is concurrent to the main instance, need to signal the main one
               onLaunchAnotherInstance(context);
            }
         }
      }

      private static void onLaunchMainInstace()
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         Trace.Listeners.Add(new CustomTraceListener("mrHelper", "mrHelper.main.log"));

         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);

         try
         {
            Application.Run(new MainForm());
         }
         catch (Exception ex) // whatever unhandled exception
         {
            HandleUnhandledException(ex);
         }
      }

      private static void onLaunchAnotherInstance(LaunchContext context)
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         Trace.Listeners.Add(new CustomTraceListener("mrHelper", "mrHelper.another.log"));

         try
         {
            Debug.Assert(!context.IsRunningMainInstance());

            if (context.Arguments.Length > 1)
            {
               string message = String.Join("|", context.Arguments);
               IntPtr mainWindow = context.GetMainWindowOfMainInstance();
               if (mainWindow != IntPtr.Zero)
               {
                  Win32Tools.SendMessageToWindow(mainWindow, message);
                  NativeMethods.SetForegroundWindow(mainWindow);
               }
            }
         }
         catch (Exception ex) // whatever unhandled exception
         {
            HandleUnhandledException(ex);
         }
      }

      private static void onLaunchFromDiffTool(LaunchContext context)
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         Trace.Listeners.Add(new CustomTraceListener("mrHelper", "mrHelper.diff.log"));

         try
         {
            Debug.Assert(!context.IsRunningMainInstance());
            int gitPID = -1;
            try
            {
               gitPID = Common.Tools.Helpers.GetGitParentProcessId(context.CurrentProcess, context.MainInstance);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot find parent git process");
            }

            if (gitPID == -1)
            {
               MessageBox.Show("Cannot find parent git process", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               return;
            }

            string[] argumentsEx = new string[context.Arguments.Length + 1];
            Array.Copy(context.Arguments, 0, argumentsEx, 0, context.Arguments.Length);
            argumentsEx[argumentsEx.Length - 1] = gitPID.ToString();

            string message = String.Join("|", argumentsEx);
            IntPtr mainWindow = context.GetMainWindowOfMainInstance();
            if (mainWindow != IntPtr.Zero)
            {
               Win32Tools.SendMessageToWindow(mainWindow, message);
            }
         }
         catch (Exception ex) // whatever unhandled exception
         {
            HandleUnhandledException(ex);
         }
      }
   }
}

