using mrCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelperUI
{
   public partial class NewDiscussionForm : Form
   {
      public NewDiscussionForm(InterprocessSnapshot snapshot, DiffToolInfo difftoolInfo)
      {
         _interprocessSnapshot = snapshot;
         _difftoolInfo = difftoolInfo;
         _gitRepository = new GitRepository(Path.Combine(snapshot.TempFolder, snapshot.Project.Split('/')[1]));
         _matcher = new RefsToLinesMatcher(_gitRepository);

         InitializeComponent();
         htmlPanel.BorderStyle = BorderStyle.FixedSingle;
         htmlPanel.Location = new Point(12, 73);
         htmlPanel.Size = new Size(860, 76);
         Controls.Add(htmlPanel);
      }

      private void NewDiscussionForm_Load(object sender, EventArgs e)
      {
         onApplicationStarted();
      }

      private void ButtonOK_Click(object sender, EventArgs e)
      {
         if (submitDiscussion())
         {
            Close();
         }
      }

      private void ButtonCancel_Click(object sender, EventArgs e)
      {
         Close();
      }

      private void TextBoxDiscussionBody_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            if (submitDiscussion())
            {
               Close();
            }
         }
      }

      private void onApplicationStarted()
      {
         this.ActiveControl = textBoxDiscussionBody;

         _position = _matcher.Match(_interprocessSnapshot.Refs, _difftoolInfo);
         if (!_position.HasValue)
         {
            handleMatchingError();
            return;
         }

         showDiscussionContext(htmlPanel, textBoxFileName);
      }

      private bool submitDiscussion()
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion body cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return false;
         }

         DiscussionParameters parameters = prepareDiscussionParameters();
         createDiscussionAtGitlab(parameters);
         return true;
      }

      private DiscussionParameters prepareDiscussionParameters()
      {
         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = textBoxDiscussionBody.Text;
         if (!_position.HasValue)
         {
            parameters.Body = getFallbackInfo() + "<br>" + parameters.Body;
         }
         else
         {
            parameters.Position = checkBoxIncludeContext.Checked ? _position : null;
         }
         return parameters;
      }

      private string getFallbackInfo()
      {
         return "<b>" + _difftoolInfo.LeftSideFileNameBrief + "</b>"
            + " (line " + _difftoolInfo.LeftSideLineNumber.ToString() + ") <i>vs</i> "
            + "<b>" + _difftoolInfo.RightSideFileNameBrief + "</b>"
            + " (line " + _difftoolInfo.RightSideLineNumber.ToString() + ")";
      }

      private void showDiscussionContext(HtmlPanel htmlPanel, TextBox tbFileName)
      {
         ContextDepth depth = new ContextDepth(0, 3);
         ContextMaker textContextMaker = new EnhancedContextMaker(_gitRepository);
         DiffContext context = textContextMaker.GetContext(_position.Value, depth);

         DiffContextFormatter formatter = new DiffContextFormatter();
         htmlPanel.Text = formatter.FormatAsHTML(context);

         tbFileName.Text = _difftoolInfo.LeftSideFileNameBrief + " vs " + _difftoolInfo.RightSideFileNameBrief;
      }

      private void createDiscussionAtGitlab(DiscussionParameters parameters)
      {
         GitLabClient client = new GitLabClient(_interprocessSnapshot.Host, _interprocessSnapshot.AccessToken);
         try
         {
            client.CreateNewMergeRequestDiscussion(
               _interprocessSnapshot.Project, _interprocessSnapshot.Id, parameters);
         }
         catch (System.Net.WebException ex)
         {
            handleGitlabError(parameters, client, ex);
         }
      }

      private void handleMatchingError()
      {
         Debug.Assert(false); // matching failed
         MessageBox.Show("Line numbers from diff tool do not match line numbers from git diff." +
            "Context will not be included into the discussion.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

         checkBoxIncludeContext.Checked = false;
         checkBoxIncludeContext.Enabled = false;
         htmlPanel.Text = "<html><body>N/A</body></html>";
         textBoxFileName.Text = "N/A";
      }

      private void handleGitlabError(DiscussionParameters parameters, GitLabClient client, System.Net.WebException ex)
      {
         var response = ((System.Net.HttpWebResponse)ex.Response);

         if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
         {
            Debug.Assert(parameters.Position.HasValue); // otherwise we could not get this error...

            parameters.Body = getFallbackInfo() + "<br>" + parameters.Body;
            parameters.Position = null;
            client.CreateNewMergeRequestDiscussion(
               _interprocessSnapshot.Project, _interprocessSnapshot.Id, parameters);
         }
         else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
         {
            // TODO Implement a fallback here (need to revert a commited discussion) 
            Debug.Assert(false);
         }

         MessageBox.Show(ex.Message +
            "Cannot create a new discussion. Gitlab does not accept passed line numbers.",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }

      private readonly InterprocessSnapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly RefsToLinesMatcher _matcher;

      private Position? _position;
      private GitRepository _gitRepository;
   }
}

