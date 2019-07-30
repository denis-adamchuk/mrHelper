using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using mrCore;

namespace mrHelperUI
{
   internal static class Program
   {
     static string mutex1_guid = "{5e9e9467-835f-497d-83de-77bdf4cfc2f1}";
     static string mutex2_guid = "{08c448dc-8635-42d0-89bd-75c14837aaa1}";

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         var arguments = Environment.GetCommandLineArgs();
         try
         {
            if (arguments.Length < 2)
            {
               using(Mutex mutex = new Mutex(false, "Global\\" + mutex1_guid))
               {
                  if(!mutex.WaitOne(0, false))
                  {
                     return;
                  }

                  Trace.Listeners.Add(new TextWriterTraceListener("mrHelper.main.log") { TraceOutputOptions = TraceOptions.Timestamp });
                  Trace.AutoFlush = true;

                  Application.Run(new mrHelperForm());
               }
            }
            else if (arguments[1] == "diff")
            {
               using(Mutex mutex = new Mutex(false, "Global\\" + mutex2_guid))
               {
                  if(!mutex.WaitOne(0, false))
                  {
                     return;
                  }

                  Trace.Listeners.Add(new TextWriterTraceListener("mrHelper.diff.log") { TraceOutputOptions = TraceOptions.Timestamp });
                  Trace.AutoFlush = true;

                  DiffArgumentsParser argumentsParser = new DiffArgumentsParser(arguments);
                  DiffToolInfo diffToolInfo = argumentsParser.Parse();

                  InterprocessSnapshot snapshot;
                  InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();

                  try
                  {
                     snapshot = serializer.DeserializeFromDisk();
                  }
                  catch (System.IO.IOException ex)
                  {
                     ExceptionHandlers.Handle(ex, "Cannot de-serialize snapshot", false);
                     MessageBox.Show("Cannot create a discussion. Make sure that timer is started in the main application.");
                     return;
                  }
                  Application.Run(new NewDiscussionForm(snapshot, diffToolInfo));
               }
            }
            else
            {
               throw new ArgumentException("Unexpected argument");
            }
         }
         catch (Exception ex)
         {
            ExceptionHandlers.HandleUnhandled(ex);
            MessageBox.Show("Fatal error occurred, see details in log file", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}

