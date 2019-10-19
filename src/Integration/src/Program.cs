using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using mrHelper.CommonTools;

namespace mrHelper.Integration
{
   class Program
   {
      private static readonly string logfilename = "mrHelper.integration.log";

      static void Main(string[] args)
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         Trace.Listeners.Add(new CustomTraceListener(Path.Combine(getFullLogPath(), logfilename),
            String.Format("Merge Request Helper Integration Tool {0} started", Application.ProductVersion)));

         if (args.Length < 1)
         {
            Console.WriteLine("Usage: mrHelper.Integration InstallationDir");
            return;
         }

         try
         {
            char[] charsToTrim = { '"' };
            string path = args[0].Trim(charsToTrim);
            RegisterCustomProtocol(path);
         }
         catch (Exception ex)
         {
            HandleUnhandledException(ex);
         }
      }

      private static string getFullLogPath()
      {
         string logFolderName = "mrHelper";
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, logFolderName);
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

         CustomProtocol protocol = new CustomProtocol(mrHelper.Common.Constants.Constants.CustomProtocolName,
            "Merge Request Helper for GitLab link protocol",
            new Dictionary<string, string>{ { "open", commandString } }, defaultIconString);
         protocol.RegisterInRegistry();
      }
   }
}

