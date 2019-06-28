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

   public partial class NewDiscussionForm : Form
   {
      private struct DiscussionContext
      {
         public IEnumerable<string> lines;
         public int highlightedIndex;
      }

      static int contextLineCount = 4;
 
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

         string currentFileNameFull =
            diffToolInfo.IsLeftSideCurrent ? diffToolInfo.LeftSideFileNameFull : diffToolInfo.RightSideFileNameFull;
         string currentFileName =
            diffToolInfo.IsLeftSideCurrent ? diffToolInfo.LeftSideFileNameBrief : diffToolInfo.RightSideFileNameBrief;
         int currentLineNumber =
            diffToolInfo.IsLeftSideCurrent ? diffToolInfo.LeftSideLineNumber : diffToolInfo.RightSideLineNumber;

         // convert one-based line number to a zero-based number
         var context = loadDiscussionContext(currentFileNameFull, currentLineNumber - 1);
         showDiscussionContext(textBoxContext, context);
         showDiscussionContextDetails(textBoxFileName, textBoxLineNumber, currentFileName, currentLineNumber);
      }

      private void ButtonOK_Click(object sender, EventArgs e)
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion body cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         bool includeDiffToolContext = checkBoxIncludeContext.Checked;

         try
         {
            gitlabClient client = new gitlabClient(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
            client.CreateNewMergeRequestDiscussion(
               _mergeRequestDetails.Project, _mergeRequestDetails.Id,
               _discussionBuilder.GetDiscussionParameters(textBoxDiscussionBody.Text, checkBoxIncludeContext.Checked));
         }
         catch (System.Net.WebException ex)
         {
            if (((System.Net.HttpWebResponse)ex.Response).StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
               // TODO Implement a fallback here (need to send a discussion body only)
               Debug.Assert(false);
            }
            else if (((System.Net.HttpWebResponse)ex.Response).StatusCode == System.Net.HttpStatusCode.InternalServerError)
            { 
               // TODO Implement a fallback here (need to revert a commited discussion) 
               Debug.Assert(false);
            }
            else
            {
               Debug.Assert(false);
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

      private void showDiscussionContextDetails(TextBox tbFilename, TextBox tbLineNumber,
         string filename, int linenumber)
      {
         tbFilename.Text = filename;
         tbLineNumber.Text = linenumber.ToString();
      }

      static private DiscussionContext loadDiscussionContext(string filename, int zeroBasedLinenumber)
      {
         DiscussionContext context;
         context.lines = File.ReadLines(filename).Skip(zeroBasedLinenumber).Take(contextLineCount);
         context.highlightedIndex = 0;
         return context;
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiscussionBuilder _discussionBuilder;
   }
}
