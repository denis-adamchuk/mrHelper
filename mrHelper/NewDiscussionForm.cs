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
         _renameChecker = new GitRenameDetector(_gitRepository);
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

         if (checkForRenamedFile())
         {
            Close();
            return;
         }

         _position = _matcher.Match(_interprocessSnapshot.Refs, _difftoolInfo);
         if (!_position.HasValue)
         {
            handleMatchingError();
            return;
         }

         showDiscussionContext(htmlPanel, textBoxFileName);
      }

      private bool checkForRenamedFile()
      {
         if (_difftoolInfo.Left.HasValue && _difftoolInfo.Right.HasValue)
         {
            // two file names are provided, nothing to check
            return false;
         }

         string anotherName = String.Empty;
         if (!_difftoolInfo.Left.HasValue)
         {
            Debug.Assert(_difftoolInfo.Right.HasValue);
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.BaseSHA, _interprocessSnapshot.Refs.HeadSHA, _difftoolInfo.Right?.FileName, false);
            if (anotherName == _difftoolInfo.Right?.FileName)
            {
               // it is not a renamed but removed file
               return false;
            }
         }
         if (!_difftoolInfo.Right.HasValue)
         {
            Debug.Assert(_difftoolInfo.Left.HasValue);
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.BaseSHA, _interprocessSnapshot.Refs.HeadSHA, _difftoolInfo.Left?.FileName, true);
            if (anotherName == _difftoolInfo.Left?.FileName)
            {
               // it is not a renamed but added file
               return false;
            }
         }

         MessageBox.Show(
            "We detected that this file is a renamed version of "
            + "\"" + anotherName + "\"" 
            + ". GitLab will not accept such input. Please match files manually in the diff tool and try again.",
            "Cannot create a discussion",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
         return true;
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
         return "<b>" + (_difftoolInfo.Left?.FileName ?? "N/A") + "</b>"
            + " (line " + (_difftoolInfo.Left?.LineNumber.ToString() ?? "N/A") + ") <i>vs</i> "
            + "<b>" + (_difftoolInfo.Right?.FileName ?? "N/A") + "</b>"
            + " (line " + (_difftoolInfo.Right?.LineNumber.ToString() ?? "N/A") + ")";
      }

      private void showDiscussionContext(HtmlPanel htmlPanel, TextBox tbFileName)
      {
         ContextDepth depth = new ContextDepth(0, 3);
         ContextMaker textContextMaker = new EnhancedContextMaker(_gitRepository);
         DiffContext context = textContextMaker.GetContext(_position.Value, depth);

         DiffContextFormatter formatter = new DiffContextFormatter();
         htmlPanel.Text = formatter.FormatAsHTML(context);

         tbFileName.Text = "Left: " + (_difftoolInfo.Left?.FileName ?? "N/A")
            + "  Right: " + (_difftoolInfo.Right?.FileName ?? "N/A");
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
            createMergeRequestWithoutPosition(parameters, client);
         }
         else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
         {
            Debug.Assert(parameters.Position.HasValue); // otherwise we could not get this error...
            cleanupBadNotes(parameters, client);
            createMergeRequestWithoutPosition(parameters, client);
         }
      }

      private void createMergeRequestWithoutPosition(DiscussionParameters parameters, GitLabClient client)
      {
         parameters.Body = getFallbackInfo() + "<br>" + parameters.Body;
         parameters.Position = null;
         client.CreateNewMergeRequestDiscussion(
            _interprocessSnapshot.Project, _interprocessSnapshot.Id, parameters);
      }

      // Instead of searching for a latest discussion note with some heuristically prepared parameters,
      // let's clean up all similar notes, including a recently added one
      private void cleanupBadNotes(DiscussionParameters parameters, GitLabClient client)
      {
         Debug.Assert(parameters.Position.HasValue);

         List<Discussion> discussions = client.GetMergeRequestDiscussions(
            _interprocessSnapshot.Project, _interprocessSnapshot.Id);
         foreach (Discussion discussion in discussions)
         {
            foreach (DiscussionNote note in discussion.Notes)
            {
               if (note.Position.HasValue && note.Position.Value.Equals(parameters.Position.Value))
               {
                  client.DeleteDiscussionNote(
                     _interprocessSnapshot.Project, _interprocessSnapshot.Id, discussion.Id, note.Id);
               }
            }
         }
      }

      private readonly InterprocessSnapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly RefsToLinesMatcher _matcher;
      private readonly GitRenameDetector _renameChecker;

      private Position? _position;
      private GitRepository _gitRepository;
   }
}

