using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.Integration
{
   class Program
   {
      private static readonly string logfilename = "mrHelper.integration.log";

      static void Main(string[] args)
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         try
         {
            Trace.Listeners.Add(new CustomTraceListener(Path.Combine(getApplicationDataPath(), logfilename),
               String.Format("Merge Request Helper Integration Tool {0} started. PID {1}",
                  Application.ProductVersion, Process.GetCurrentProcess().Id)));
         }
         catch (ArgumentException)
         {
            return;
         }

         if (args.Length < 1)
         {
            Console.WriteLine("Usage: mrHelper.Integration [InstallationDir|-x]");
            return;
         }

         try
         {
            if (args[0] == "-x")
            {
               UnregisterCustomProtocol();
               return;
            }

            char[] charsToTrim = { '"' };
            string path = args[0].Trim(charsToTrim);
            RegisterCustomProtocol(path);
         }
         catch (Exception ex)
         {
            HandleUnhandledException(ex);
         }
      }

      private static string getApplicationDataPath()
      {
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, Constants.ApplicationDataFolderName);
      }

      private static void HandleUnhandledException(Exception ex)
      {
         MessageBox.Show("Fatal error occurred on attempt to integrate Merge Request Helper, see details in logs",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
         Application.Exit();
      }

      static private void RegisterCustomProtocol(string directory)
      {
         string binaryFileName = "mrHelper.exe";
         string binaryFilePath = System.IO.Path.Combine(directory, binaryFileName);
         string defaultIconString = String.Format("\"{0}\", 0", binaryFilePath);
         string commandString = String.Format("\"{0}\" \"%1\"", binaryFilePath);

         CustomProtocol protocol = new CustomProtocol(Constants.CustomProtocolName);
         protocol.RegisterInRegistry("Merge Request Helper for GitLab link protocol",
            new Dictionary<string, string>{ { "open", commandString } }, defaultIconString);
      }

      static private void UnregisterCustomProtocol()
      {
         CustomProtocol protocol = new CustomProtocol(Constants.CustomProtocolName);
         protocol.RemoveFromRegistry();
      }
   }
}

