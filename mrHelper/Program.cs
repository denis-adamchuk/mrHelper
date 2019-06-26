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
      static Regex trimmedFilenameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);

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
               DiffToolInfo diffToolInfo;
               diffToolInfo.CurrentFileName = arguments[2];   // %F1
               diffToolInfo.CurrentFileNameBrief = convertToGitlabFilename(diffToolInfo.CurrentFileName);
               diffToolInfo.CurrentLineNumber = arguments[3]; // %l1
               diffToolInfo.NextFileName = arguments[4];      // %F2
               diffToolInfo.NextFileNameBrief = convertToGitlabFilename(diffToolInfo.NextFileName);
               diffToolInfo.NextLineNumber = arguments[5];    // %l2

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

      static private string convertToGitlabFilename(string fullFilename)
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");
         string trimmedFilename = fullFilename
            .Substring(tempFolder.Length, fullFilename.Length - tempFolder.Length)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

         Match m = trimmedFilenameRe.Match(trimmedFilename);
         if (!m.Success || m.Groups.Count < 3 || !m.Groups[2].Success)
         {
            throw new ApplicationException("Cannot parse a path obtained from difftool");
         }

         return m.Groups[2].Value;
      }
   }
}
