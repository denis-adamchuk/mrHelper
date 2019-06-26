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
               // Launch from diff tool
               DiffDetails diffDetails;
               diffDetails.FilenameCurrentPane = arguments[2];   // %F1
               diffDetails.LineNumberCurrentPane = arguments[3]; // %l1
               diffDetails.FilenameNextPane = arguments[4];      // %F2
               diffDetails.LineNumberNextPane = arguments[5];    // %l2

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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      static MergeRequestDetails? getMergeRequestDetails()
      {
         string snapshotPath = Environment.GetEnvironmentVariable("TEMP");
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath, mrHelperForm.InterprocessSnapshotFilename);
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
   }
}
