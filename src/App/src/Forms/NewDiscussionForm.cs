using System;
using System.Drawing;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Context;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Matching;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : Form
   {
      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      internal NewDiscussionForm(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitRepository gitRepository)
      {
         InitializeComponent();
         htmlPanel.BorderStyle = BorderStyle.FixedSingle;
         htmlPanel.Location = new Point(12, 73);
         htmlPanel.Size = new Size(860, 76);
         Controls.Add(htmlPanel);

         this.Text = mrHelper.Common.Constants.Constants.NewDiscussionCaption;
         this.ActiveControl = textBoxDiscussionBody;
         showDiscussionContext(leftSideFileName, rightSideFileName, position, gitRepository);
      }

      public bool IncludeContext { get { return checkBoxIncludeContext.Checked; } }
      public string Body { get { return textBoxDiscussionBody.Text; } }

      private void TextBoxDiscussionBody_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      private void NewDiscussionForm_Shown(object sender, EventArgs e)
      {
         TopMost = false; // disable TopMost property which is initially true
         Activate(); // steal Focus
      }

      /// <summary>
      /// Throws ArgumentException.
      /// Throws GitOperationException and GitObjectException in case of problems with git.
      /// </summary>
      private void showDiscussionContext(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitRepository gitRepository)
      {
         ContextDepth depth = new ContextDepth(0, 3);
         IContextMaker textContextMaker = new SimpleContextMaker(gitRepository);
         DiffContext context = textContextMaker.GetContext(position, depth);

         DiffContextFormatter formatter = new DiffContextFormatter();
         htmlPanel.Text = formatter.FormatAsHTML(context);

         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }
   }
}

