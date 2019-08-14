using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.Core.Interprocess;
using mrHelper.App.Forms;

namespace mrHelper.App
{
   internal static class Program
   {
      static string mutex1_guid = "{5e9e9467-835f-497d-83de-77bdf4cfc2f1}";
      static string mutex2_guid = "{08c448dc-8635-42d0-89bd-75c14837aaa1}";

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
            using (Mutex mutex = new Mutex(false, "Global\\" + mutex1_guid))
            {
               if (!mutex.WaitOne(0, false))
               {
                  return;
               }

               Application.ThreadException += (sender,e) => HandleUnhandledException(e.Exception);

               Trace.Listeners.Add(new CustomTraceListener("mrHelper.main.log"));
               Trace.AutoFlush = true;
               try
               {
                  Application.Run(new mrHelperForm());
               }
               catch (Exception ex) // whatever unhandled exception
               {
                  HandleUnhandledException(ex);
               }
            }
         }
         else if (arguments[1] == "diff")
         {
            using (Mutex mutex = new Mutex(false, "Global\\" + mutex2_guid))
            {
               if (!mutex.WaitOne(0, false))
               {
                  return;
               }

               Application.ThreadException += (sender,e) => HandleUnhandledException(e.Exception);

               string currentExe = System.Reflection.Assembly.GetEntryAssembly().Location;
               string currentDir = System.IO.Path.GetDirectoryName(currentExe);
               string logfilename = System.IO.Path.Combine(currentDir, "mrHelper.diff.log");

               var listener = new CustomTraceListener(logfilename);
               Trace.Listeners.Add(listener);
               Trace.AutoFlush = true;

               try
               {
                  DiffArgumentParser argumentsParser = new DiffArgumentParser(arguments);
                  DiffToolInfo diffToolInfo = argumentsParser.Parse();

                  SnapshotSerializer serializer = new SnapshotSerializer();
                  Snapshot? snapshot = null;
                  try
                  {
                     snapshot = serializer.DeserializeFromDisk();
                  }
                  catch (System.IO.IOException ex)
                  {
                     ExceptionHandlers.Handle(ex, "Cannot de-serialize snapshot");
                     MessageBox.Show("Cannot create a discussion. Make sure that timer is started in the main application.");
                     return;
                  }

                  Application.Run(new NewDiscussionForm(snapshot.Value, diffToolInfo));
               }
               catch (Exception ex) // whatever unhandled exception
               {
                  HandleUnhandledException(ex);
               }
            }
         }
      }
   }
}

