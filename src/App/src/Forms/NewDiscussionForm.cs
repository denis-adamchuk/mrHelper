﻿using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.CommonNative;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
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
         htmlPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
         Controls.Add(htmlPanel);

         applyFont(Program.Settings.MainWindowFontSizeName);

         this.Text = Constants.NewDiscussionCaption;
         this.ActiveControl = textBoxDiscussionBody;
         showDiscussionContext(leftSideFileName, rightSideFileName, position, gitRepository);

         applyTheme(Program.Settings.VisualThemeName);
      }

      private void applyTheme(string theme)
      {
         if (theme == "New Year 2020")
         {
            pictureBox1.BackgroundImage = mrHelper.App.Properties.Resources.Penguin;
            pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox1.Visible = true;
            htmlPanel.Width = pictureBox1.Location.X - 50 - htmlPanel.Location.X;
         }
         else
         {
            pictureBox1.Visible = false;
            htmlPanel.Width = textBoxDiscussionBody.Width;
         }
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
         Win32Tools.ForceWindowIntoForeground(this.Handle);
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
         htmlPanel.Text = formatter.FormatAsHTML(context, htmlPanel.Font.Height, 2);
         htmlPanel.Height = htmlPanel.DisplayRectangle.Height + 2;

         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }
   }
}

