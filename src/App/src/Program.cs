using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.Core;

namespace mrHelper.App
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
         if (arguments.Length < 2)
         {
            using (Mutex mutex = new Mutex(false, "Global\\" + mutex1_guid))
            {
               if (!mutex.WaitOne(0, false))
               {
                  return;
               }

               Application.ThreadException += (sender,e) => ExceptionHandlers.HandleUnhandled(e.Exception, true);

               Trace.Listeners.Add(new CustomTraceListener("mrHelper.main.log"));
               Trace.AutoFlush = true;
               try
               {
                  Application.Run(new mrHelperForm());
               }
               catch (Exception ex) // whatever unhandled exception
               {
                  ExceptionHandlers.HandleUnhandled(ex, true);
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

               Application.ThreadException += (sender,e) => ExceptionHandlers.HandleUnhandled(e.Exception, false);

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

                  InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();
                  InterprocessSnapshot? snapshot = null;
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

                  Application.Run(new NewDiscussionForm(snapshot.Value, diffToolInfo));
               }
               catch (Exception ex) // whatever unhandled exception
               {
                  ExceptionHandlers.HandleUnhandled(ex, false);
               }
            }
         }
      }
   }
}

