using mrCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace mrHelperUI
{
   public partial class NewDiscussionForm : Form
   {
      public NewDiscussionForm(InterprocessSnapshot snapshot, DiffToolInfo difftoolInfo)
      {
         _interprocessSnapshot = snapshot;
         _difftoolInfo = difftoolInfo;
         _gitRepository = new GitRepository(Path.Combine(snapshot.TempFolder, snapshot.Project));
         _matcher = new RefsToLinesMatcher(_gitRepository);

         InitializeComponent();
      }

      private void NewDiscussionForm_Load(object sender, EventArgs e)
      {
         onApplicationStarted();
      }

      private void onApplicationStarted()
      {
         this.ActiveControl = textBoxDiscussionBody;

         _position = _matcher.Match(_interprocessSnapshot.Refs, _difftoolInfo);
         if (_position.HasValue)
         {
            Debug.Assert(false); // matching failed

            checkBoxIncludeContext.Checked = false;
            checkBoxIncludeContext.Enabled = false;
            textBoxContext.Text = "N/A";
            textBoxFileName.Text = "N/A";
            return;
         }

         PlainContextMaker textContextMaker = new PlainContextMaker(_gitRepository);
         DiffContext context = textContextMaker.GetContext(_position.Value, 4);
         showDiscussionContext(textBoxContext, textBoxFileName, context);
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
         parameters.Body = textBoxDiscussionBody.Text;
         parameters.Position = checkBoxIncludeContext.Checked ? _position : null;

         GitLabClient client = new GitLabClient(_interprocessSnapshot.Host, _interprocessSnapshot.AccessToken);
         try
         {
            client.CreateNewMergeRequestDiscussion(
               _interprocessSnapshot.Project, _interprocessSnapshot.Id, parameters);
         }
         catch (System.Net.WebException ex)
         {
            Debug.Assert(false);

            if (((System.Net.HttpWebResponse)ex.Response).StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
               Debug.Assert(checkBoxIncludeContext.Checked); // otherwise we could not get an error...
               parameters.Body = getFallbackInfo() + "<br>" + parameters.Body;
               parameters.Position = null;
               client.CreateNewMergeRequestDiscussion(
                  _interprocessSnapshot.Project, _interprocessSnapshot.Id, parameters);
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

      private string getFallbackInfo()
      {
         return "<b>" + _difftoolInfo.LeftSideFileNameBrief + "</b>"
            + " (line " + _difftoolInfo.LeftSideLineNumber.ToString() + ") <i>vs</i> "
            + "<b>" + _difftoolInfo.RightSideFileNameBrief + "</b>"
            + " (line " + _difftoolInfo.RightSideLineNumber.ToString() + ")";
      }

      static private void showDiscussionContext(RichTextBox textbox, TextBox tbFileName, DiffContext context)
      {
         //foreach (var line in context.Lines)
         //{
         //   string text;
         //   if (line.NumberLeft.HasValue && line.NumberRight.HasValue)
         //   {
         //      text = line.NumberLeft.Value.ToString() + "(" + line.NumberRight.Value.ToString() + ") " + line.Text;
         //   }
         //   else if (line.NumberLeft.HasValue)
         //   {
         //      text = line.NumberLeft.Value.ToString() + " - " + line.Text;
         //   }
         //   else if (line.NumberRight.HasValue)
         //   {
         //      text = line.NumberRight.Value.ToString() + " + " + line.Text;
         //   }
         //   textbox.AppendText(line.Text + "\r\n");
         //}

         tbFileName.Text = context.FileName;
      }

      private readonly InterprocessSnapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly RefsToLinesMatcher _matcher;

      private Position? _position;
      private GitRepository _gitRepository;
   }
}
