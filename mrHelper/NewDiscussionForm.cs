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

   enum DiffToolLineState
   {
      Ambigiuos,
      AddedOrModified,
      Deleted,
      Unaffected
   }

   enum FinalLineState
   {
      AddedOrModified,
      Deleted,
      Unaffected
   }

   public partial class NewDiscussionForm : Form
   {
      static Regex diffSectionRe = new Regex(@"\@\@\s-(?'left_start'\d+)(,(?'left_len'\d+))?\s\+(?'right_start'\d+)(,(?'right_len'\d+))?\s\@\@", RegexOptions.Compiled);

      static int contextLineCount = 4;
 
      public NewDiscussionForm(MergeRequestDetails mrDetails, DiffToolInfo difftoolInfo)
      {
         _mergeRequestDetails = mrDetails;
         _difftoolInfo = difftoolInfo;
         _lineState = getCurrentLineState();

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

         if (_lineState == DiffToolLineState.Ambigiuos)
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

         bool needPosition = checkBoxIncludeContext.Checked;
         DiscussionParameters parameters = getDiscussionParameters(getFinalLineState(), needPosition);

         try
         {
            gitlabClient client = new gitlabClient(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
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

      private DiscussionParameters getDiscussionParameters(FinalLineState state, bool needPosition)
      {
         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = getDiscussionBody(!needPosition);
         parameters.Position = createPositionDetails(state);
         return parameters;
      }

      private string getDiscussionBody(bool addHeader)
      {
         string header = addHeader ? (getDiscussionHeader() + "<br>") : "";
         string body = textBoxDiscussionBody.Text.Replace("\r\n", "<br>").Replace("\n", "<br>");
         return header + body;
      }

      private DiscussionParameters.PositionDetails createPositionDetails(FinalLineState state)
      {
         DiscussionParameters.PositionDetails details = new DiscussionParameters.PositionDetails();
         details.BaseSHA = _mergeRequestDetails.BaseSHA;
         details.HeadSHA = _mergeRequestDetails.HeadSHA;
         details.StartSHA = _mergeRequestDetails.StartSHA;

         switch (state)
         {
            case FinalLineState.AddedOrModified:
               details.OldPath = _difftoolInfo.NextFileName;
               details.OldLine = null;
               details.NewPath = _difftoolInfo.CurrentFileNameBrief;
               details.NewLine = _difftoolInfo.CurrentLineNumber;
               break;

            case FinalLineState.Deleted:
               details.OldPath = _difftoolInfo.CurrentFileNameBrief;
               details.OldLine = _difftoolInfo.CurrentLineNumber;
               details.NewPath = _difftoolInfo.NextFileName;
               details.NewLine = null;
               break;

            case FinalLineState.Unaffected:
               details.OldPath = _difftoolInfo.NextFileName;
               details.OldLine = _difftoolInfo.NextLineNumber;
               details.NewPath = _difftoolInfo.CurrentFileNameBrief;
               details.NewLine = _difftoolInfo.CurrentLineNumber;
               break;
         }

         return details;
      }

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

      FinalLineState getFinalLineState()
      {
         switch (_lineState)
         {
            case DiffToolLineState.Ambigiuos:
               return radioContext1.Checked ? FinalLineState.Deleted : FinalLineState.AddedOrModified;
            case DiffToolLineState.AddedOrModified:
               return FinalLineState.AddedOrModified;
            case DiffToolLineState.Deleted:
               return FinalLineState.Deleted;
            case DiffToolLineState.Unaffected:
               return FinalLineState.Unaffected;
         }
         return FinalLineState.Unaffected;
      }

      private DiffToolLineState getCurrentLineState()
      {
         string filename = _difftoolInfo.CurrentFileNameBrief;
         int currentLineNumber = int.Parse(_difftoolInfo.CurrentLineNumber);
         var sections = getDiffSections(filename);

         bool addedOrModified = false;
         foreach (var section in sections)
         {
            if (currentLineNumber >= section.RightSectionStart && currentLineNumber < section.RightSectionEnd)
            {
               addedOrModified = true;
               break;
            }
         }

         bool deleted = false;
         foreach (var section in sections)
         {
            if (currentLineNumber >= section.LeftSectionStart && currentLineNumber < section.LeftSectionEnd)
            {
               deleted = true;
               break;
            }
         }

         if (addedOrModified && deleted)
         {
            return DiffToolLineState.Ambigiuos;
         }
         else if (addedOrModified)
         {
            return DiffToolLineState.AddedOrModified;
         }
         else if (deleted)
         {
            return DiffToolLineState.Deleted;
         }

         return DiffToolLineState.Unaffected;
      }
 
      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly DiffToolLineState _lineState;
   }
}
