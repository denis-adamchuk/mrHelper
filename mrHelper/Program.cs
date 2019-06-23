using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Diagnostics;

namespace mrHelper
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         var arguments = Environment.GetCommandLineArgs();
         try
         {
            if (arguments.Length < 2)
            {
               // Ordinary launch
               Application.Run(new mrHelperForm());
            }
            else if (arguments[1] == "diff" && arguments.Length == 6)
            {
               Thread.Sleep(10000);
               // Launch from diff tool
               DiffDetails diffDetails;
               diffDetails.FilenameLeft = arguments[2];
               diffDetails.FilenameRight = arguments[3];
               diffDetails.LineNumberLeft = arguments[4];
               diffDetails.LineNumberRight = arguments[5];

               var connectedMergeRequestDetails = getMergeRequestDetails();
               if (!connectedMergeRequestDetails.HasValue)
               {
                  throw new ArgumentException("To create a discussion you need to start tracking time");
               }
               Application.Run(new NewDiscussionForm(connectedMergeRequestDetails.Value, diffDetails));
            }
            else
            {
               throw new ArgumentException("Unexpected argument");
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
         }
      }

      static MergeRequestDetails? getMergeRequestDetails()
      {
         string fullSnapshotName = getSnapshotPath() + mrHelperForm.InterprocessSnapshotFilename;
         if (!System.IO.File.Exists(fullSnapshotName))
         {
            return null;
         }

         string jsonStr = System.IO.File.ReadAllText(fullSnapshotName);

         JavaScriptSerializer serializer = new JavaScriptSerializer();
         dynamic json = serializer.DeserializeObject(jsonStr);

         MergeRequestDetails details;
         details.Host = json["Host"];
         details.AccessToken = json["AccessToken"];
         details.Project = json["Project"];
         details.Id = json["Id"];
         details.BaseSHA = json["BaseSHA"];
         details.StartSHA = json["StartSHA"];
         details.HeadSHA = json["HeadSHA"];
         details.TempFolder = json["TempFolder"];
         return details;
      }

      private static string getSnapshotPath()
      {
         Process[] processList = Process.GetProcesses();
         foreach (Process process in processList)
         {
            if (process.ProcessName.StartsWith(Process.GetCurrentProcess().ProcessName) &&
                process.Id != Process.GetCurrentProcess().Id)
            {
               return process.MainModule.FileName.Substring(
                  0, process.MainModule.FileName.Length - process.ProcessName.Length - 4 /* ".exe" */);
            }
         }

         return "";
      }
   }
}
