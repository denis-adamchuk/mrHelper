using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Discussions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Core.Git;
using System.Threading.Tasks;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : Form
   {
      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      internal NewDiscussionForm(Snapshot snapshot, DiffToolInfo difftoolInfo)
      {
         _interprocessSnapshot = snapshot;
         _difftoolInfo = difftoolInfo;

         GitClientFactory factory = new GitClientFactory(snapshot.TempFolder, null);
         _gitRepository = factory.GetClient(_interprocessSnapshot.Host, _interprocessSnapshot.Project);
         _renameChecker = new GitRenameDetector(_gitRepository);
         _matcher = new RefToLineMatcher(_gitRepository);

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

      async private void ButtonOK_Click(object sender, EventArgs e)
      {
         if (await submitDiscussion())
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

            buttonOK.PerformClick();
         }
      }

      private void onApplicationStarted()
      {
         this.ActiveControl = textBoxDiscussionBody;
         string anotherName;
         bool moved;
         bool fileRenamed;
         try
         {
            fileRenamed = checkForRenamedFile(out anotherName, out moved);
         }
         catch (GitOperationException)
         {
            throw; // fatal error
         }

         if (moved)
         {
            Trace.TraceInformation("Detected file rename. DiffToolInfo: {0}", _difftoolInfo);

            MessageBox.Show(
                  "We detected that this file is a moved version of "
                  + "\"" + anotherName + "\""
                  + ". GitLab does not allow to create discussions on moved files.",
                  "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
         }
         else if (fileRenamed)
         {
            Trace.TraceInformation("Detected file rename. DiffToolInfo: {0}", _difftoolInfo);

            MessageBox.Show(
                  "We detected that this file is a renamed version of "
                  + "\"" + anotherName + "\""
                  + ". GitLab requires both files to be available to create discussions. "
                  + "Please match files manually in the diff tool and try again.",
                  "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
         }

         _position = _matcher.Match(_interprocessSnapshot.Refs, _difftoolInfo);

         showDiscussionContext(htmlPanel, textBoxFileName);
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      private bool checkForRenamedFile(out string anotherName, out bool moved)
      {
         anotherName = String.Empty;
         moved = false;

         if (!_difftoolInfo.Left.HasValue)
         {
            Debug.Assert(_difftoolInfo.Right.HasValue);
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.LeftSHA,
               _interprocessSnapshot.Refs.RightSHA,
               _difftoolInfo.Right?.FileName,
               false, out moved);
            if (anotherName == _difftoolInfo.Right?.FileName)
            {
               // it is not a renamed but removed file
               return false;
            }
         }
         else if (!_difftoolInfo.Right.HasValue)
         {
            Debug.Assert(_difftoolInfo.Left.HasValue);
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.LeftSHA,
               _interprocessSnapshot.Refs.RightSHA,
               _difftoolInfo.Left?.FileName,
               true, out moved);
            if (anotherName == _difftoolInfo.Left?.FileName)
            {
               // it is not a renamed but added file
               return false;
            }
         }
         else
         {
            // If even two names are given, we need to check here because use might selected manually two
            // versions of a moved file
            anotherName = _renameChecker.IsRenamed(
               _interprocessSnapshot.Refs.LeftSHA,
               _interprocessSnapshot.Refs.RightSHA,
               _difftoolInfo.IsLeftSideCurrent ? _difftoolInfo.Left?.FileName : _difftoolInfo.Right?.FileName,
               _difftoolInfo.IsLeftSideCurrent, out moved);
            return moved;
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
      async private Task<bool> submitDiscussion()
      {
         if (textBoxDiscussionBody.Text.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return false;
         }

         Hide(); // things below may take some time but we no longer need to show the form

         NewDiscussionParameters parameters = prepareDiscussionParameters();
         await createDiscussionAtGitlab(parameters);
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

      async private Task createDiscussionAtGitlab(NewDiscussionParameters parameters)
      {
         UserDefinedSettings settings = new UserDefinedSettings(false);
         DiscussionManager manager = new DiscussionManager(settings);
         DiscussionCreator creator = manager.GetDiscussionCreator(
            new MergeRequestDescriptor
            {
               HostName = _interprocessSnapshot.Host,
               ProjectName = _interprocessSnapshot.Project,
               IId = _interprocessSnapshot.MergeRequestIId
            });

         try
         {
            await creator.CreateDiscussionAsync(parameters);
         }
         catch (DiscussionCreatorException ex)
         {
            Trace.TraceInformation(
                  "Additional information about exception:\n" +
                  "Position: {0}\n" +
                  "Include context: {1}\n" +
                  "Snapshot refs: {2}\n" +
                  "DiffToolInfo: {3}\n" +
                  "Body:\n{4}",
                  _position.ToString(),
                  checkBoxIncludeContext.Checked.ToString(),
                  _interprocessSnapshot.Refs.ToString(),
                  _difftoolInfo.ToString(),
                  textBoxDiscussionBody.Text);

            if (!ex.Handled)
            {
               throw;
            }
         }
      }

      private readonly Snapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly RefToLineMatcher _matcher;
      private readonly GitRenameDetector _renameChecker;

      private DiffPosition _position;
      private readonly IGitRepository _gitRepository;
   }
}

