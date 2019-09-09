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
      internal NewDiscussionForm(Snapshot snapshot, DiffToolInfo difftoolInfo, DiffPosition position,
         IGitRepository gitRepository)
      {
         _interprocessSnapshot = snapshot;
         _difftoolInfo = difftoolInfo;
         _position = position;
         _gitRepository = gitRepository;

         InitializeComponent();
         htmlPanel.BorderStyle = BorderStyle.FixedSingle;
         htmlPanel.Location = new Point(12, 73);
         htmlPanel.Size = new Size(860, 76);
         Controls.Add(htmlPanel);

         this.ActiveControl = textBoxDiscussionBody;
         showDiscussionContext(htmlPanel, textBoxFileName);
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

      /// <summary>
      /// Throws ArgumentException.
      /// Throws GitOperationException and GitObjectException in case of problems with git.
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
      private readonly DiffPosition _position;
      private readonly IGitRepository _gitRepository;
   }
}

