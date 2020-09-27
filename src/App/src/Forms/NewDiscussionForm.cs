using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Constants;
using mrHelper.CommonNative;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;
using System.Threading.Tasks;
using mrHelper.Common.Tools;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitCommandService git, Func<string, bool, Task> onSubmitDiscussion)
      {
         InitializeComponent();
         this.TopMost = Program.Settings.NewDiscussionIsTopMostForm;

         applyFont(Program.Settings.MainWindowFontSizeName);
         createWPFTextBox();

         this.Text = Constants.StartNewThreadCaption;
         labelNoteAboutInvisibleCharacters.Text = Constants.WarningOnUnescapedMarkdown;
         showDiscussionContext(leftSideFileName, rightSideFileName, position, git);

         buttonCancel.ConfirmationCondition =
            () => textBoxDiscussionBody.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;
         _onSubmitDiscussion = onSubmitDiscussion;
      }

      async private void buttonOK_Click(object sender, EventArgs e)
      {
         Hide();

         string body = textBoxDiscussionBody.Text;
         bool needIncludeContext = checkBoxIncludeContext.Checked;
         await _onSubmitDiscussion?.Invoke(body, needIncludeContext);

         Close();
      }

      private void buttonCancel_Click(object sender, EventArgs e)
      {
         Close();
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControlMode.SelectedTab == tabPagePreview)
         {
            htmlPanelPreview.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
               Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanelPreview));

            var pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
            string body = MarkDownUtils.ConvertToHtml(textBoxDiscussionBody.Text, String.Empty, pipeline);
            htmlPanelPreview.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         }
      }

      private void textBoxDiscussionBody_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }

      private void textBoxDiscussionBody_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(textBoxDiscussionBody.Text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         Helpers.WPFHelpers.InsertCodePlaceholderIntoTextBox(textBoxDiscussionBody);
         textBoxDiscussionBody.Focus();
      }

      private void newDiscussionForm_Shown(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
         textBoxDiscussionBody.Focus();
      }

      private void showDiscussionContext(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitCommandService git)
      {
         string html = getContextHtmlText(position, git, out string stylesheet);
         htmlPanelContext.BaseStylesheet = stylesheet;
         htmlPanelContext.Text = html;

         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      private string getContextHtmlText(DiffPosition position, IGitCommandService git, out string stylesheet)
      {
         stylesheet = String.Empty;

         DiffContext? context;
         try
         {
            ContextDepth depth = new ContextDepth(0, 3);
            IContextMaker textContextMaker = new SimpleContextMaker(git);
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
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(htmlPanelContext);
         return DiffContextFormatter.GetHtml(context.Value, fontSizePx, 2, true);
      }

      private void createWPFTextBox()
      {
         textBoxDiscussionBody = Helpers.WPFHelpers.CreateWPFTextBox(textBoxDiscussionBodyHost,
            false, String.Empty, true, !Program.Settings.DisableSpellChecker);
         textBoxDiscussionBody.KeyDown += textBoxDiscussionBody_KeyDown;
         textBoxDiscussionBody.TextChanged += textBoxDiscussionBody_TextChanged;
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;

      private Func<string, bool, Task> _onSubmitDiscussion;
   }
}

