using System;
using System.Windows.Forms;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.CommonNative;
using mrHelper.Common.Exceptions;
using System.Diagnostics;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitCommitStorage gitRepository)
      {
         InitializeComponent();
         htmlPanel.AutoScroll = false;
         htmlPanel.BorderStyle = BorderStyle.None;
         htmlPanel.Dock = DockStyle.Fill;
         htmlContextCanvas.Controls.Add(htmlPanel);

         applyFont(Program.Settings.MainWindowFontSizeName);
         createWPFTextBox();

         this.Text = Constants.StartNewThreadCaption;
         showDiscussionContext(leftSideFileName, rightSideFileName, position, gitRepository);

         buttonCancel.ConfirmationCondition =
            () => textBoxDiscussionBody.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;
      }

      public bool IncludeContext { get { return checkBoxIncludeContext.Checked; } }
      public string Body { get { return textBoxDiscussionBody.Text; } }

      private void textBoxDiscussionBody_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      private void NewDiscussionForm_Shown(object sender, EventArgs e)
      {
         TopMost = false; // disable TopMost property which is initially true
         Win32Tools.ForceWindowIntoForeground(this.Handle);
         textBoxDiscussionBody.Focus();
      }

      /// <summary>
      /// Throws ArgumentException.
      /// </summary>
      private void showDiscussionContext(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitCommitStorage gitRepository)
      {
         string html = getContextHtmlText(position, gitRepository, out string stylesheet);
         htmlPanel.BaseStylesheet = stylesheet;
         htmlPanel.Text = html;

         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      private string getContextHtmlText(DiffPosition position, IGitCommitStorage gitRepository, out string stylesheet)
      {
         stylesheet = String.Empty;

         DiffContext? context;
         try
         {
            ContextDepth depth = new ContextDepth(0, 3);
            IContextMaker textContextMaker = new SimpleContextMaker(gitRepository);
            context = textContextMaker.GetContext(position, depth);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is ContextMakingException)
            {
               string errorMessage = "Cannot render HTML context.";
               ExceptionHandlers.Handle(errorMessage, ex);
               return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
            }
            throw;
         }

         Debug.Assert(context.HasValue);
         DiffContextFormatter formatter =
            new DiffContextFormatter(WinFormsHelpers.GetFontSizeInPixels(htmlPanel), 2);
         stylesheet = formatter.GetStylesheet();
         return formatter.GetBody(context.Value);
      }

      private void createWPFTextBox()
      {
         textBoxDiscussionBody = new System.Windows.Controls.TextBox();
         textBoxDiscussionBodyHost.Child = this.textBoxDiscussionBody;
         textBoxDiscussionBody.AcceptsReturn = true;
         textBoxDiscussionBody.TextWrapping = System.Windows.TextWrapping.Wrap;
         textBoxDiscussionBody.SpellCheck.IsEnabled = true;
         textBoxDiscussionBody.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
         textBoxDiscussionBody.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
         textBoxDiscussionBody.KeyDown += textBoxDiscussionBody_KeyDown;
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;
   }
}

