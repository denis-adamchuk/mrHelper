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
      public string FilenameCurrentPane;
      public string FilenameNextPane;
      public string LineNumberCurrentPane;
      public string LineNumberNextPane;
   }

   public struct PositionDetails
   {
      public string OldFilename;
      public string OldLineNumber;
      public string NewFilename;
      public string NewLineNumber;
      public bool Ambiguous;

      public PositionDetails(string oldFilename, string oldLineNumber, string newFilename, string newLineNumber,
         bool ambiguous)
      {
         OldFilename = oldFilename;
         OldLineNumber = oldLineNumber;
         NewFilename = newFilename;
         NewLineNumber = newLineNumber;
         Ambiguous = ambiguous;
      }
   }

   public partial class NewDiscussionForm : Form
   {
      static Regex trimmedFilenameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);
      static Regex diffSectionRe = new Regex(@"\@\@\s-(?'left_start'\d+)(,(?'left_len'\d+))?\s\+(?'right_start'\d+)(,(?'right_len'\d+))?\s\@\@", RegexOptions.Compiled);

      static int contextLineCount = 6;
 
      public NewDiscussionForm(MergeRequestDetails mrDetails, DiffDetails diffDetails)
      {
         _mergeRequestDetails = mrDetails;
         _diffDetails = diffDetails;

         InitializeComponent();
         onApplicationStarted();
      }

      private void onApplicationStarted()
      {
         textBoxFileName.Text = convertToGitlabFilename(_diffDetails.FilenameCurrentPane);
         textBoxLineNumber.Text = _diffDetails.LineNumberCurrentPane;
         showDiscussionContext();
         this.ActiveControl = textBoxDiscussionBody;
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

         Close();
      }

      private void ButtonCancel_Click(object sender, EventArgs e)
      {
         Close();
      }

      private DiscussionParameters getDiscussionParameters()
      {
         string formattedBody = textBoxDiscussionBody.Text.Replace("\r\n", "<br>").Replace("\n", "<br>");

         DiscussionParameters parameters = new DiscussionParameters();
         string filenameCurrent = convertToGitlabFilename(_diffDetails.FilenameCurrentPane);
         string lineNumberCurrent = _diffDetails.LineNumberCurrentPane;
         string filenameNext = convertToGitlabFilename(_diffDetails.FilenameNextPane);
         string lineNumberNext = _diffDetails.LineNumberNextPane;
         if (!checkBoxIncludeContext.Checked)
         {
            parameters.Body = getDiscussionHeader(filenameCurrent, lineNumberCurrent, filenameNext, lineNumberNext) +
               "<br>" + formattedBody;
            return parameters;
         }

         parameters.Body = formattedBody;

         DiscussionParameters.PositionDetails details =
            new DiscussionParameters.PositionDetails();
         details.BaseSHA = trimRemoteRepositoryName(_mergeRequestDetails.BaseSHA);
         details.HeadSHA = trimRemoteRepositoryName(_mergeRequestDetails.HeadSHA);
         details.StartSHA = trimRemoteRepositoryName(_mergeRequestDetails.StartSHA);

         PositionDetails positionDetails = getPositionDetails(
            filenameCurrent, int.Parse(lineNumberCurrent),
            filenameNext, int.Parse(lineNumberNext));

         details.OldPath = positionDetails.OldFilename;
         details.OldLine = positionDetails.OldLineNumber;
         details.NewPath = positionDetails.NewFilename;
         details.NewLine = positionDetails.NewLineNumber;
         parameters.Position = details;

         return parameters;
      }

      private string getDiscussionHeader(string filenameCurrent, string lineNumberCurrent,
         string filenameNext, string lineNumberNext)
      {
         return "<b>" + filenameCurrent + "</b> (line " + lineNumberCurrent + ") <i>vs</i> "
              + "<b>" + filenameNext + "</b> (line " + lineNumberNext + ")";
      }

      private string convertToGitlabFilename(string fullFilename)
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

      private void showDiscussionContext()
      {
         string filename = _diffDetails.FilenameCurrentPane;
         int currentLineNumber = int.Parse(_diffDetails.LineNumberCurrentPane) - 1; // convert to zero-based index
         int contextFirstLineNumber = Math.Max(0, currentLineNumber - 3);
         var lines = File.ReadLines(filename).Skip(contextFirstLineNumber).Take(contextLineCount);

         textBoxContext.SelectionFont = new Font(textBoxContext.Font, FontStyle.Regular);
         for (int index = contextFirstLineNumber; index < currentLineNumber; ++index)
         {
            textBoxContext.AppendText(lines.ElementAt(index - contextFirstLineNumber) + "\r\n");
         }

         textBoxContext.SelectionFont = new Font(textBoxContext.Font, FontStyle.Bold);
         textBoxContext.AppendText(lines.ElementAt(currentLineNumber - contextFirstLineNumber) + "\r\n");

         textBoxContext.SelectionFont = new Font(textBoxContext.Font, FontStyle.Regular);
         for (int index = currentLineNumber + 1; index < lines.Count() + contextFirstLineNumber; ++index)
         {
            textBoxContext.AppendText(lines.ElementAt(index - contextFirstLineNumber) + "\r\n");
         }
     }

      private List<Tuple<int, int, int, int>> getDiffSections(string filenameCurrentPane, int lineNumberCurrentPane,
         string filenameNextPane, int lineNumberNextPane)
      {
         List<Tuple<int, int, int, int>> sections = new List<Tuple<int, int, int, int>>();

         List<string> diff = gitClient.Diff(_mergeRequestDetails.StartSHA, _mergeRequestDetails.HeadSHA,
            filenameCurrentPane);
         foreach (string line in diff)
         {
            Match m = diffSectionRe.Match(line);
            if (!m.Success || m.Groups.Count < 3)
            {
               continue;
            }

            if (!m.Groups["left_start"].Success || !m.Groups["right_start"].Success)
            {
               continue;
            }

            int leftSectionStart = int.Parse(m.Groups["left_start"].Value);
            int leftSectionLength = m.Groups["left_len"].Success ? int.Parse(m.Groups["left_len"].Value) : 1;
            int leftSectionEnd = leftSectionStart + leftSectionLength;
            int rightSectionStart = int.Parse(m.Groups["right_start"].Value);
            int rightSectionLength = m.Groups["right_len"].Success ? int.Parse(m.Groups["right_len"].Value) : 1;
            int rightSectionEnd = rightSectionStart + rightSectionLength;

            Tuple<int, int, int, int> section =
               new Tuple<int, int, int, int>(leftSectionStart, leftSectionEnd, rightSectionStart, rightSectionEnd);
            sections.Add(section);
         }

         return sections;
      }

      enum LineState
      {
         eFound,
         eFoundZero,
         eNotFound
      }

      struct TwoLinesMatch
      {
         public LineState Line1AtLeft;
         public LineState Line1AtRight;
         public LineState Line2AtLeft;
         public LineState Line2AtRight;
      }

      TwoLinesMatch matchLines(Tuple<int, int, int, int> section, int line1Number, int line2Number)
      {
         int leftSectionStart = section.Item1;
         int leftSectionEnd = section.Item2;
         int rightSectionStart = section.Item3;
         int rightSectionEnd = section.Item4;

         TwoLinesMatch match = new TwoLinesMatch();
         match.Line1AtLeft = match.Line1AtRight = match.Line2AtLeft = match.Line2AtRight = LineState.eNotFound;

         if (leftSectionStart != leftSectionEnd)
         {
            if (line1Number >= leftSectionStart && line1Number < leftSectionEnd)
            {
               match.Line1AtLeft = LineState.eFound;
            }
            if (line2Number >= leftSectionStart && line2Number < leftSectionEnd)
            {
               match.Line2AtLeft = LineState.eFound;
            }
         }
         else // if zero-size
         {
            if (line1Number == leftSectionStart)
            {
               match.Line1AtLeft = LineState.eFoundZero;
            }
            if (line2Number == leftSectionStart)
            {
               match.Line2AtLeft = LineState.eFoundZero;
            }
         }

         if (rightSectionStart != rightSectionEnd)
         {
            if (line1Number >= rightSectionStart && line1Number < rightSectionEnd)
            {
               match.Line1AtRight = LineState.eFound;
            }
            if (line2Number >= rightSectionStart && line2Number < rightSectionEnd)
            {
               match.Line2AtRight = LineState.eFound;
            }
         }
         else // if zero-size
         {
            if (line1Number == rightSectionStart)
            {
               match.Line1AtRight = LineState.eFoundZero;
            }
            if (line2Number == rightSectionStart)
            {
               match.Line2AtRight = LineState.eFoundZero;
            }
         }

         return match;
      }

      private PositionDetails getPositionDetails(string filenameCurrentPane, int lineNumberCurrentPane,
         string filenameNextPane, int lineNumberNextPane)
      {
         var sections = getDiffSections(filenameCurrentPane, lineNumberCurrentPane, filenameNextPane, lineNumberNextPane);

         // Best matches
         foreach (var section in sections)
         {
            TwoLinesMatch match = matchLines(section, lineNumberCurrentPane, lineNumberNextPane);

            if (match.Line1AtLeft == LineState.eFound && match.Line2AtRight == LineState.eFound)
            {
               return new PositionDetails(filenameCurrentPane, lineNumberCurrentPane.ToString(), filenameNextPane, null, false);
            }
            else if (match.Line1AtRight == LineState.eFound && match.Line2AtLeft == LineState.eFound)
            {
               return new PositionDetails(filenameNextPane, null, filenameCurrentPane, lineNumberCurrentPane.ToString(), false);
            }
         }

         // Rest matches
         foreach (var section in sections)
         {
            TwoLinesMatch match = matchLines(section, lineNumberCurrentPane, lineNumberNextPane);

            if (match.Line1AtLeft == LineState.eFound && match.Line2AtRight == LineState.eFoundZero)
            {
               return new PositionDetails(filenameCurrentPane, lineNumberCurrentPane.ToString(), filenameNextPane, null, false);
            }
            else if (match.Line1AtLeft == LineState.eFoundZero && match.Line2AtRight == LineState.eFound)
            {
               return new PositionDetails(filenameNextPane, lineNumberNextPane.ToString(), filenameCurrentPane, null, false);
            }
            else if (match.Line1AtRight == LineState.eFound && match.Line2AtLeft == LineState.eFoundZero)
            {
               return new PositionDetails(filenameNextPane, null, filenameCurrentPane, lineNumberCurrentPane.ToString(), false);
            }
            else if (match.Line1AtRight == LineState.eFoundZero && match.Line2AtLeft == LineState.eFound)
            {
               return new PositionDetails(filenameCurrentPane, null, filenameNextPane, lineNumberNextPane.ToString(), false);
            }
         }

         // No match
         return new PositionDetails(
            filenameNextPane, lineNumberNextPane.ToString(),
            filenameCurrentPane, lineNumberCurrentPane.ToString(), true);
      }
  
      private static string trimRemoteRepositoryName(string sha)
      {
         string remoteRepositoryDefaultName = "origin/";
         if (sha.StartsWith(remoteRepositoryDefaultName))
         {
            sha = sha.Substring(remoteRepositoryDefaultName.Length,
               sha.Length - remoteRepositoryDefaultName.Length);
         }
         return sha;
      }
 
      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiffDetails _diffDetails;
   }
}
