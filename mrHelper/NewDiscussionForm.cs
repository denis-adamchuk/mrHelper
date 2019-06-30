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
   public partial class NewDiscussionForm : Form
   {
      private struct DiscussionContext
      {
         public IEnumerable<string> lines;
         public string filename;
         public string linenumber;
         public int highlightedIndex;
      }

      static int contextLinesToShow = 4;
 
      public NewDiscussionForm(MergeRequestDetails mrDetails, DiffToolInfo difftoolInfo)
      {
         _mergeRequestDetails = mrDetails;
         _discussionBuilder = new DiscussionBuilder(mrDetails, difftoolInfo);
         InitializeComponent();

         onApplicationStarted(ref difftoolInfo);
      }

      private void onApplicationStarted(ref DiffToolInfo diffToolInfo)
      {
         this.ActiveControl = textBoxDiscussionBody;

         var positionState = _discussionBuilder.GetPositionState();
         var context = loadDiscussionContext(diffToolInfo, positionState);
         showDiscussionContext(textBoxContext, context);
         showDiscussionContextDetails(textBoxFileName, textBoxLineNumber, context);

         if (positionState == DiscussionBuilder.PositionState.Undefined)
         {
            checkBoxIncludeContext.Checked = false;
            checkBoxIncludeContext.Enabled = false;
         }
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
         try
         {
            client.CreateNewMergeRequestDiscussion(
               _mergeRequestDetails.Project, _mergeRequestDetails.Id,
               _discussionBuilder.GetDiscussionParameters(textBoxDiscussionBody.Text, checkBoxIncludeContext.Checked));
         }
         catch (System.Net.WebException ex)
         {
            Debug.Assert(false);
            if (((System.Net.HttpWebResponse)ex.Response).StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
               // Fallback (need to send a discussion body only)
               client.CreateNewMergeRequestDiscussion(
                  _mergeRequestDetails.Project, _mergeRequestDetails.Id,
                  _discussionBuilder.GetDiscussionParameters(textBoxDiscussionBody.Text, false));
            }
            else if (((System.Net.HttpWebResponse)ex.Response).StatusCode == System.Net.HttpStatusCode.InternalServerError)
            { 
               // TODO Implement a fallback here (need to revert a commited discussion) 
            }

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

      private void showDiscussionContextDetails(TextBox tbFilename, TextBox tbLineNumber, DiscussionContext context)
      {
         tbFilename.Text = context.filename;
         tbLineNumber.Text = context.linenumber.ToString();
      }

      static private DiscussionContext loadDiscussionContext(DiffToolInfo diffToolInfo, DiscussionBuilder.PositionState positionState)
      {
         DiscussionContext context = new DiscussionContext();
         switch (positionState)
         {
            case DiscussionBuilder.PositionState.OldLineOnly:
               context.lines = File.ReadLines(diffToolInfo.LeftSideFileNameFull).
                  Skip(diffToolInfo.LeftSideLineNumber - 1).Take(contextLinesToShow);
               context.filename = diffToolInfo.LeftSideFileNameBrief;
               context.linenumber = diffToolInfo.LeftSideLineNumber.ToString();
               break;

            case DiscussionBuilder.PositionState.NewLineOnly:
            case DiscussionBuilder.PositionState.Both:
               context.lines = File.ReadLines(diffToolInfo.RightSideFileNameFull).
                  Skip(diffToolInfo.RightSideLineNumber - 1).Take(contextLinesToShow);
               context.filename = diffToolInfo.RightSideFileNameBrief;
               context.linenumber = diffToolInfo.RightSideLineNumber.ToString();
               break;

            case DiscussionBuilder.PositionState.Undefined:
               List<string> strings = new List<string>();
               strings.Add("N/A");
               context.lines = strings;
               context.filename = "N/A";
               context.linenumber = "N/A";
               break;
         }
         context.highlightedIndex = 0;
         return context;
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiscussionBuilder _discussionBuilder;
   }
}
