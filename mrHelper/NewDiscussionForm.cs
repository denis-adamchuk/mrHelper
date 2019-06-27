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
      public int Id;
      public string Host;
      public string AccessToken;
      public string Project;
      public string BaseSHA;
      public string StartSHA;
      public string HeadSHA;
      public string TempFolder;
   }

   public struct DiffToolInfo
   {
      public string CurrentFileName;
      public string CurrentFileNameBrief;
      public string CurrentLineNumber;
      public string NextFileName;
      public string NextFileNameBrief;
      public string NextLineNumber;
   }

   struct GitDiffSection
   {
      public int LeftSectionStart;
      public int LeftSectionEnd;
      public int RightSectionStart;
      public int RightSectionEnd;
   }

   struct DiscussionContext
   {
      public IEnumerable<string> lines;
      public int highlightedIndex;
   }

   // How a line number can be treated when analyzing 'git diff' output
   enum GitDiffLineState
   {
      Ambigiuos,        // eg Line #1 in @@ -1,2 +1,2 @@
      AddedOrModified,  // eg Line #1 in @@ -1,0 +1,15 @@
      Deleted,          // eg Line #1 in @@ -1,2 +3,3 @@
      PointsAtZero,     // eg Line #1 in @@ -1,0 +5,10 @@
      Unaffected        // eg Lines #1 and #100 in @@ -10,10 +50,2 @@
   }

   // What lines need to be included into Merge Request Discussion Position details
   enum PositionState
   {
      OldLineOnly,
      NewLineOnly,
      Both,
      BothSwapped
   }

   public partial class NewDiscussionForm : Form
   {
      static Regex diffSectionRe = new Regex(@"\@\@\s-(?'left_start'\d+)(,(?'left_len'\d+))?\s\+(?'right_start'\d+)(,(?'right_len'\d+))?\s\@\@", RegexOptions.Compiled);

      static int contextLineCount = 4;
 
      public NewDiscussionForm(MergeRequestDetails mrDetails, DiffToolInfo difftoolInfo)
      {
         _mergeRequestDetails = mrDetails;
         _difftoolInfo = difftoolInfo;
         initializeLineState();
         InitializeComponent();

         onApplicationStarted();
      }

      private void onApplicationStarted()
      {
         this.ActiveControl = textBoxDiscussionBody;

         int linenumber = int.Parse(_difftoolInfo.CurrentLineNumber);
         var context = getDiscussionContext(_difftoolInfo.CurrentFileName, linenumber - 1);
         showDiscussionContext(textBoxContext1, context);
         showDiscussionContextDetails(textBoxFileName1, textBoxLineNumber1, _difftoolInfo.CurrentFileNameBrief, linenumber);

         if (_lineState == GitDiffLineState.Ambigiuos)
         {
            int linenumber2 = int.Parse(_difftoolInfo.NextLineNumber);
            var context2 = getDiscussionContext(_difftoolInfo.NextFileName, linenumber2 - 1);
            showDiscussionContext(textBoxContext2, context2);
            showDiscussionContextDetails(textBoxFileName2, textBoxLineNumber2, _difftoolInfo.NextFileNameBrief, linenumber2);
         }
         else
         {
            hideSecondContext();
         }
      }

      private void hideSecondContext()
      {
         radioContext1.Hide();
         radionContext2.Hide();
         int extraHeight = textBoxContext2.Height + textBoxFileName2.Height + textBoxLineNumber2.Height;
         foreach (Control control in this.Controls)
         {
            if (control.Top > groupBoxContext.Bottom)
            {
               control.Location = new Point(control.Location.X, control.Location.Y - extraHeight);
            }
         }
         groupBoxContext.Size = new Size(groupBoxContext.Width, groupBoxContext.Height - extraHeight);
         this.Size = new Size(this.Width, this.Height - extraHeight);
         textBoxContext2.Hide();
         textBoxFileName2.Hide();
         textBoxLineNumber2.Hide();
      }

      private void ButtonOK_Click(object sender, EventArgs e)
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion body cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = formatDiscussionBody(textBoxDiscussionBody.Text, !checkBoxIncludeContext.Checked);
         if (checkBoxIncludeContext.Checked)
         {
            try
            {
               parameters.Position = createPositionDetails(getPositionState(radioContext1.Checked));
            }
            catch (Exception ex)
            {
               Debug.Assert(false);

               // Fallback case 
               parameters.Body = formatDiscussionBody(textBoxDiscussionBody.Text, true);

               MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
         }

         try
         {
            gitlabClient client = new gitlabClient(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
            client.CreateNewMergeRequestDiscussion(
               _mergeRequestDetails.Project, _mergeRequestDetails.Id, parameters);
         }
         catch (System.Net.WebException ex)
         {
            Debug.Assert(false);

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

      static private void showDiscussionContext(RichTextBox textbox, DiscussionContext context)
      {
         textbox.SelectionFont = new Font(textbox.Font, FontStyle.Regular);
         for (int index = 0; index < context.highlightedIndex; ++index)
         {
            textbox.AppendText(context.lines.ElementAt(index) + "\r\n");
         }

         textbox.SelectionFont = new Font(textbox.Font, FontStyle.Bold);
         textbox.AppendText(context.lines.ElementAt(context.highlightedIndex) + "\r\n");

         textbox.SelectionFont = new Font(textbox.Font, FontStyle.Regular);
         for (int index = context.highlightedIndex + 1; index < context.lines.Count(); ++index)
         {
            textbox.AppendText(context.lines.ElementAt(index) + "\r\n");
         }
      }

      private void showDiscussionContextDetails(TextBox tbFilename, TextBox tbLineNumber,
         string filename, int linenumber)
      {
         tbFilename.Text = filename;
         tbLineNumber.Text = linenumber.ToString();
      }

      // TODO All below move to a separate class(-es)

      private string getDiscussionHeader()
      {
         return "<b>" + _difftoolInfo.CurrentFileNameBrief + "</b>"
              + " (line " + _difftoolInfo.CurrentLineNumber + ") <i>vs</i> "
              + "<b>" + _difftoolInfo.NextFileNameBrief + "</b>"
              + " (line " + _difftoolInfo.NextLineNumber + ")";
      }

      static private DiscussionContext getDiscussionContext(string filename, int zeroBasedLinenumber)
      {
         DiscussionContext context;
         context.lines = File.ReadLines(filename).Skip(zeroBasedLinenumber).Take(contextLineCount);
         context.highlightedIndex = 0;
         return context;
      }

      private string formatDiscussionBody(string userDefinedBody, bool addHeader)
      {
         string header = addHeader ? (getDiscussionHeader() + "<br>") : "";
         string body = userDefinedBody.Replace("\r\n", "<br>").Replace("\n", "<br>");
         return header + body;
      }

      private void initializeLineState()
      {
         var lineState = matchLineToGitDiff(_difftoolInfo.CurrentFileNameBrief, int.Parse(_difftoolInfo.CurrentLineNumber));
         if (lineState == GitDiffLineState.PointsAtZero)
         {
            lineState = matchLineToGitDiff(_difftoolInfo.NextFileNameBrief, int.Parse(_difftoolInfo.NextLineNumber));
            if (lineState == GitDiffLineState.PointsAtZero)
            {
               // TODO Test this case, is it ever possible?
               Debug.Assert(false);
            }
            else
            {
               // TODO Is it correct fallback?
               Debug.Assert(lineState != GitDiffLineState.Unaffected);

               // let's swap 'current' and 'next' and lineState will be ok for it
               swapSidesInDiffToolInfo();
            }
         }
         _lineState = lineState;
      }

      private void swapSidesInDiffToolInfo()
      {
         var tempString = _difftoolInfo.CurrentFileName;
         _difftoolInfo.CurrentFileName = _difftoolInfo.NextFileName;
         _difftoolInfo.NextFileName = tempString;

         tempString = _difftoolInfo.CurrentFileNameBrief;
         _difftoolInfo.CurrentFileNameBrief = _difftoolInfo.NextFileNameBrief;
         _difftoolInfo.NextFileNameBrief = tempString;

         tempString = _difftoolInfo.CurrentLineNumber;
         _difftoolInfo.CurrentLineNumber = _difftoolInfo.NextLineNumber;
         _difftoolInfo.NextLineNumber = tempString;
      }

      private DiscussionParameters.PositionDetails createPositionDetails(PositionState state)
      {
         DiscussionParameters.PositionDetails details = new DiscussionParameters.PositionDetails();
         details.BaseSHA = _mergeRequestDetails.BaseSHA;
         details.HeadSHA = _mergeRequestDetails.HeadSHA;
         details.StartSHA = _mergeRequestDetails.StartSHA;

         switch (state)
         {
            case PositionState.NewLineOnly:
               details.OldPath = _difftoolInfo.NextFileNameBrief;
               details.OldLine = null;
               details.NewPath = _difftoolInfo.CurrentFileNameBrief;
               details.NewLine = _difftoolInfo.CurrentLineNumber;
               break;

            case PositionState.OldLineOnly:
               details.OldPath = _difftoolInfo.CurrentFileNameBrief;
               details.OldLine = _difftoolInfo.CurrentLineNumber;
               details.NewPath = _difftoolInfo.NextFileNameBrief;
               details.NewLine = null;
               break;

            case PositionState.Both:
               details.OldPath = _difftoolInfo.CurrentFileNameBrief;
               details.OldLine = _difftoolInfo.CurrentLineNumber;
               details.NewPath = _difftoolInfo.NextFileNameBrief;
               details.NewLine = _difftoolInfo.NextLineNumber;
               break;

            case PositionState.BothSwapped:
               details.OldPath = _difftoolInfo.NextFileNameBrief;
               details.OldLine = _difftoolInfo.NextLineNumber;
               details.NewPath = _difftoolInfo.CurrentFileNameBrief;
               details.NewLine = _difftoolInfo.CurrentLineNumber;
               break;
         }

         return details;
      }

      PositionState getPositionState(bool resolveAmbigiutyToOld)
      {
         switch (_lineState)
         {
            case GitDiffLineState.Ambigiuos:
               return resolveAmbigiutyToOld ? PositionState.OldLineOnly : PositionState.NewLineOnly;
            case GitDiffLineState.AddedOrModified:
               return PositionState.NewLineOnly;
            case GitDiffLineState.Deleted:
               return PositionState.OldLineOnly;
            case GitDiffLineState.Unaffected:
               {
                  // When a line is not changed, we need to pass two line numbers to gitlab,
                  // but first we need to understand which one of them points to old/new file

                  if (matchDiffToolInfoToFile(_difftoolInfo))
                  {
                     return PositionState.Both;
                  }

                  var difftoolInfo = _difftoolInfo;
                  var tempString = difftoolInfo.CurrentLineNumber;
                  difftoolInfo.CurrentLineNumber = _difftoolInfo.NextLineNumber;
                  difftoolInfo.NextLineNumber = tempString;

                  if (matchDiffToolInfoToFile(difftoolInfo))
                  {
                     return PositionState.BothSwapped;
                  }

                  throw new ApplicationException("Cannot match line numbers to files");
               }
         }
         Debug.Assert(false);
         return PositionState.Both;
      }

      private List<GitDiffSection> getDiffSections(string filename)
      {
         List<GitDiffSection> sections = new List<GitDiffSection>();

         List<string> diff = gitClient.Diff(_mergeRequestDetails.BaseSHA, _mergeRequestDetails.HeadSHA, filename);
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

            // @@ -1 +1 @@ is essentially the same as @@ -1,1 +1,1 @@
            int leftSectionLength = m.Groups["left_len"].Success ? int.Parse(m.Groups["left_len"].Value) : 1;
            int rightSectionLength = m.Groups["right_len"].Success ? int.Parse(m.Groups["right_len"].Value) : 1;

            GitDiffSection section;
            section.LeftSectionStart = int.Parse(m.Groups["left_start"].Value);
            section.LeftSectionEnd = section.LeftSectionStart + leftSectionLength;
            section.RightSectionStart = int.Parse(m.Groups["right_start"].Value);
            section.RightSectionEnd = section.RightSectionStart + rightSectionLength;
            sections.Add(section);
         }

         return sections;
      }

      private GitDiffLineState matchLineToGitDiff(string filename, int linenumber)
      {
         var sections = getDiffSections(filename);

         bool addedOrModified = false;
         foreach (var section in sections)
         {
            if (linenumber >= section.RightSectionStart && linenumber < section.RightSectionEnd)
            {
               addedOrModified = true;
               break;
            }
         }

         bool deleted = false;
         foreach (var section in sections)
         {
            if (linenumber >= section.LeftSectionStart && linenumber < section.LeftSectionEnd)
            {
               deleted = true;
               break;
            }
         }

         if (addedOrModified && deleted)
         {
            return GitDiffLineState.Ambigiuos;
         }
         else if (addedOrModified)
         {
            return GitDiffLineState.AddedOrModified;
         }
         else if (deleted)
         {
            return GitDiffLineState.Deleted;
         }

         bool pointsAtZero = false;
         foreach (var section in sections)
         {
            if ((linenumber == section.RightSectionStart && linenumber == section.RightSectionEnd)
             || (linenumber == section.LeftSectionStart  && linenumber == section.LeftSectionEnd))
            {
               pointsAtZero = true;
               break;
            }
         }

         if (pointsAtZero)
         {
            return GitDiffLineState.PointsAtZero;
         }

         return GitDiffLineState.Unaffected;
      }

      private bool matchDiffToolInfoToFile(DiffToolInfo info)
      {
         string current = File.ReadLines(info.CurrentFileName).Skip(int.Parse(info.CurrentLineNumber)).Take(1).First();
         string next = File.ReadLines(info.NextFileName).Skip(int.Parse(info.NextLineNumber)).Take(1).First();
         return current == next;
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private DiffToolInfo _difftoolInfo;
      private GitDiffLineState _lineState;
   }
}
