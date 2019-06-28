using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

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
               DiffArgumentsParser argumentsParser = new DiffArgumentsParser(arguments);
               DiffToolInfo diffToolInfo = argumentsParser.Parse();

               var connectedMergeRequestDetails = getMergeRequestDetails();
               if (!connectedMergeRequestDetails.HasValue)
               {
                  throw new ArgumentException("To create a discussion you need to start tracking time");
               }
               Application.Run(new NewDiscussionForm(connectedMergeRequestDetails.Value, diffToolInfo));
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
