using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper
{
   public struct MergeRequestDetails
   {
      public string Host;
      public string AccessToken;
      public string Project;
      public int Id;
      public string BaseSHA;
      public string StartSHA;
      public string HeadSHA;
      public string TempFolder;
   }

   public struct DiffDetails
   {
      public string FilenameLeft;
      public string FilenameRight;
      public string LineNumberLeft;
      public string LineNumberRight;
   }

   enum LineState
   {
      eDeleted,
      eAddedOrModified,
      eUnchanged
   }

   public partial class NewDiscussionForm : Form
   {
      static Regex trimmedFilenameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);
      static Regex diffSectionRe = new Regex(@"@@\s-(\d*),(\d*)\s\+(\d*),(\d*)\s@@", RegexOptions.Compiled);

      public NewDiscussionForm(MergeRequestDetails mrDetails, DiffDetails diffDetails)
      {
         _mergeRequestDetails = mrDetails;
         _diffDetails = diffDetails;

         InitializeComponent();
         onApplicationStarted();
      }

      private void onApplicationStarted()
      {
         textBoxFileName.Text = _diffDetails.FilenameRight;
         textBoxLineNumber.Text = _diffDetails.LineNumberRight;
         textBoxContext.Text = getDiscussionContext();
      }

      private void ButtonOK_Click(object sender, EventArgs e)
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion body cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         gitlabClient client = new gitlabClient(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
         DiscussionParameters parameters = getDiscussionParameters();

         try
         {
            client.CreateNewMergeRequestDiscussion(
               _mergeRequestDetails.Project, _mergeRequestDetails.Id, parameters);
         }
         catch (System.Net.WebException ex)
         {
            MessageBox.Show(ex.Message +
               "Cannot create a new discussion. Gitlab does not accept passed line numbers.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private DiscussionParameters getDiscussionParameters()
      {
         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = textBoxDiscussionBody.Text;
         if (!checkBoxIncludeContext.Checked)
         {
            return parameters;
         }

         DiscussionParameters.PositionDetails details =
            new DiscussionParameters.PositionDetails();
         details.BaseSHA = _mergeRequestDetails.BaseSHA;
         details.HeadSHA = _mergeRequestDetails.HeadSHA;
         details.StartSHA = _mergeRequestDetails.StartSHA;
         details.OldPath = convertToGitlabFilename(_diffDetails.FilenameLeft);
         details.NewPath = convertToGitlabFilename(_diffDetails.FilenameRight);

         LineState lineState = getLineState(details.NewPath,
            int.Parse(_diffDetails.LineNumberLeft), int.Parse(_diffDetails.LineNumberRight));
         switch (lineState)
         {
            case LineState.eAddedOrModified:
               details.OldLine = null;
               details.NewLine = _diffDetails.LineNumberRight;
               break;
            case LineState.eDeleted:
               details.OldLine = _diffDetails.LineNumberLeft;
               details.NewLine = null;
               break;
            case LineState.eUnchanged:
               details.OldLine = _diffDetails.LineNumberLeft;
               details.NewLine = _diffDetails.LineNumberRight;
               break;
         }

         parameters.Position = details;
         return parameters;
      }

      private string convertToGitlabFilename(string fullFilename)
      {
         string trimmedFilename = fullFilename
            .Substring(_mergeRequestDetails.TempFolder.Length,
               _diffDetails.FilenameLeft.Length - _mergeRequestDetails.TempFolder.Length)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

         Match m = trimmedFilenameRe.Match(trimmedFilename);
         if (!m.Success)
         {
            throw new ApplicationException("Cannot parse a path obtained from difftool");
         }

         return m.Groups[2].Value;
      }

      private void ButtonCancel_Click(object sender, EventArgs e)
      {
         Close();
      }

      private string getDiscussionContext()
      {
         // TODO
         //   return File.ReadLines(filename).Skip(lineNumber - 1).Take(1).First();
         return "";
      }

      private LineState getLineState(string filename, int leftLineNumber, int rightLineNumber)
      {
         List<string> diff = gitClient.Diff(_mergeRequestDetails.StartSHA, _mergeRequestDetails.HeadSHA, filename);
         foreach (string line in diff)
         {
            Match m = diffSectionRe.Match(line);
            if (!m.Success)
            {
               continue;
            }

            int leftSectionStart = int.Parse(m.Groups[1].Value);
            int leftSectionLength = int.Parse(m.Groups[2].Value);
            int rightSectionStart = int.Parse(m.Groups[3].Value);
            int rightSectionLength = int.Parse(m.Groups[4].Value);

            if (rightSectionLength == 0)
            {
               if (leftLineNumber >= leftSectionStart && leftLineNumber < leftSectionStart + leftSectionLength)
               {
                  return LineState.eDeleted;
               }
            }

            if (rightLineNumber >= rightSectionStart && rightLineNumber < rightSectionStart + rightSectionLength)
            {
               return LineState.eAddedOrModified;
            }
         }

         return LineState.eUnchanged;
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiffDetails _diffDetails;
   }
}
