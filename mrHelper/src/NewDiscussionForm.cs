using GitLabSharp;
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
      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
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

      /// <summary>
      /// Creates a form and fill its content.
      /// Throws different types of ecxeptions, all of them are considered fatal and passed to the upper-level handler
      /// </summary>
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

         try
         {
            string anotherName = String.Empty;
            if (checkForRenamedFile(out anotherName))
            {
               MessageBox.Show(
                  "We detected that this file is a renamed version of "
                  + "\"" + anotherName + "\"" 
                  + ". GitLab will not accept such input. Please match files manually in the diff tool and try again.",
                  "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Information);
               Close();
               return;
            }

            _position = _matcher.Match(_interprocessSnapshot.Refs, _difftoolInfo);

            showDiscussionContext(htmlPanel, textBoxFileName);
         }
         catch (MatchException)
         {
            // Some kind of special handling
            MessageBox.Show(
               "Line numbers from diff tool do not match line numbers from git diff. " +
               "Make sure that you use correct instance of diff tool.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
         }

         // Pass other exceptions to upper-level handlers
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      private bool checkForRenamedFile(out string anotherName)
      {
         anotherName = String.Empty;
         if (_difftoolInfo.Left.HasValue && _difftoolInfo.Right.HasValue)
         {
            // two file names are provided, nothing to check
            return false;
         }

         if (!_difftoolInfo.Left.HasValue)
         {
            Debug.Assert(_difftoolInfo.Right.HasValue);
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.LeftSHA,
               _interprocessSnapshot.Refs.RightSHA,
               _difftoolInfo.Right?.FileName,
               false);
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
               _interprocessSnapshot.Refs.LeftSHA,
               _interprocessSnapshot.Refs.RightSHA,
               _difftoolInfo.Left?.FileName,
               true);
            if (anotherName == _difftoolInfo.Left?.FileName)
            {
               // it is not a renamed but added file
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Throws ArgumentException.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      private void showDiscussionContext(HtmlPanel htmlPanel, TextBox tbFileName)
      {
         ContextDepth depth = new ContextDepth(0, 3);
         IContextMaker textContextMaker = new EnhancedContextMaker(_gitRepository);
         DiffContext context = textContextMaker.GetContext(_position, depth);

         DiffContextFormatter formatter = new DiffContextFormatter();
         htmlPanel.Text = formatter.FormatAsHTML(context);

         tbFileName.Text = "Left: " + (_difftoolInfo.Left?.FileName ?? "N/A")
            + "  Right: " + (_difftoolInfo.Right?.FileName ?? "N/A");
      }

      /// <summary>
      /// Throws GitLabRequestException in case of fatal GitLab problems.
      /// </summary>
      private bool submitDiscussion()
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion body cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return false;
         }

         NewDiscussionParameters parameters = prepareDiscussionParameters();
         createDiscussionAtGitlab(parameters);
         return true;
      }

      private NewDiscussionParameters prepareDiscussionParameters()
      {
         NewDiscussionParameters parameters = new NewDiscussionParameters
         {
            Body = textBoxDiscussionBody.Text
         };
         parameters.Position = checkBoxIncludeContext.Checked
            ? createPositionParameters(_position) : new Nullable<PositionParameters>();
         return parameters;
      }

      private PositionParameters createPositionParameters(DiffPosition position)
      {
         return new PositionParameters
         {
            OldPath = position.LeftPath,
            OldLine = position.LeftLine,
            NewPath = position.RightPath,
            NewLine = position.RightLine,
            BaseSHA = position.Refs.LeftSHA,
            HeadSHA = position.Refs.RightSHA,
            StartSHA = position.Refs.LeftSHA
         };
      }

      private void createDiscussionAtGitlab(NewDiscussionParameters parameters)
      {
         GitLab gl = new GitLab(_interprocessSnapshot.Host, _interprocessSnapshot.AccessToken);
         try
         {
            var project = gl.Projects.Get(_interprocessSnapshot.Project);
            project.MergeRequests.Get(_interprocessSnapshot.MergeRequestId).Discussions.CreateNew(parameters);
         }
         catch (System.Net.WebException ex)
         {
            handleGitlabError(parameters, gl, ex);
         }
      }

      private void handleGitlabError(NewDiscussionParameters parameters, GitLab gl, System.Net.WebException ex)
      {
         var response = ((System.Net.HttpWebResponse)ex.Response);

         if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
         {
            // Something went wrong at the GitLab site, let's report a discussion without Position
            createMergeRequestWithoutPosition(parameters, gl);
         }
         else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
         {
            // Something went wrong at the GitLab site, let's report a discussion without Position
            cleanupBadNotes(parameters, gl);
            createMergeRequestWithoutPosition(parameters, gl);
         }
         else
         {
            // Fatal error. To be catched and logged at the upper level.
            throw ex;
         }
      }

      private void createMergeRequestWithoutPosition(NewDiscussionParameters parameters, GitLab gl)
      {
         Debug.Assert(parameters.Position.HasValue);

         parameters.Body = getFallbackInfo() + "<br>" + parameters.Body;
         parameters.Position = null;

         var project = gl.Projects.Get(_interprocessSnapshot.Project);
         project.MergeRequests.Get(_interprocessSnapshot.MergeRequestId).Discussions.CreateNew(parameters);
      }

      private string getFallbackInfo()
      {
         return "<b>" + (_difftoolInfo.Left?.FileName ?? "N/A") + "</b>"
            + " (line " + (_difftoolInfo.Left?.LineNumber.ToString() ?? "N/A") + ") <i>vs</i> "
            + "<b>" + (_difftoolInfo.Right?.FileName ?? "N/A") + "</b>"
            + " (line " + (_difftoolInfo.Right?.LineNumber.ToString() ?? "N/A") + ")";
      }

      // Instead of searching for a latest discussion note with some heuristically prepared parameters,
      // let's clean up all similar notes, including a recently added one
      private void cleanupBadNotes(NewDiscussionParameters parameters, GitLab gl)
      {
         Debug.Assert(parameters.Position.HasValue);

         var project = gl.Projects.Get(_interprocessSnapshot.Project);
         var mergeRequest = project.MergeRequests.Get(_interprocessSnapshot.MergeRequestId);
         List<Discussion> discussions = mergeRequest.Discussions.LoadAll();
         foreach (Discussion discussion in discussions)
         {
            foreach (DiscussionNote note in discussion.Notes)
            {
               if (arePositionsEqual(note.Position, parameters.Position.Value))
               {
                  mergeRequest.Notes.Get(note.Id).Delete();
               }
            }
         }
      }

      /// <summary>
      /// Compares GitLabSharp.Position object which is received from GitLab
      /// to GitLabSharp.PositionParameters whichi is sent to GitLab for equality
      /// </summary>
      /// <returns>true if objects point to the same position</returns>
      private bool arePositionsEqual(Position pos, PositionParameters posParams)
      {
         return pos.Base_SHA == posParams.BaseSHA
             && pos.Head_SHA == posParams.HeadSHA
             && pos.Start_SHA == posParams.StartSHA
             && pos.Old_Line == posParams.OldLine
             && pos.Old_Path == posParams.OldPath
             && pos.New_Line == posParams.NewLine
             && pos.New_Path == posParams.NewPath;
      }

      private readonly InterprocessSnapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly RefsToLinesMatcher _matcher;
      private readonly GitRenameDetector _renameChecker;

      private DiffPosition _position;
      private readonly GitRepository _gitRepository;
   }
}

