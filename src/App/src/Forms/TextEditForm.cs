using System;
using System.Windows.Forms;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class TextEditForm : CustomFontForm
   {
      internal TextEditForm(string caption, string initialText, bool editable, bool multiline,
         Control extraActionsControl, string uploadsPrefix)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);
         Text = caption;
         labelNoteAboutInvisibleCharacters.Text = Constants.WarningOnUnescapedMarkdown;
         _uploadsPrefix = uploadsPrefix;

         createWPFTextBox(initialText, editable, multiline);

         buttonCancel.ConfirmationCondition =
            () => initialText != String.Empty
                  ? textBox.Text != initialText
                  : textBox.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;

         if (extraActionsControl != null)
         {
            panelExtraActions.Controls.Add(extraActionsControl);
            extraActionsControl.Dock = DockStyle.Fill;
         }

         applyFont(Program.Settings.MainWindowFontSizeName);
         adjustFormHeight();
      }

      internal string Body => textBox.Text;

      internal System.Windows.Controls.TextBox TextBox => textBox;

      private void createWPFTextBox(string initialText, bool editable, bool multiline)
      {
         textBox = Helpers.WPFHelpers.CreateWPFTextBox(textBoxHost, !editable, initialText, multiline,
            !Program.Settings.DisableSpellChecker);
         textBox.KeyDown += textBox_KeyDown;
         textBox.TextChanged += textBox_TextChanged;
      }

      private void adjustFormHeight()
      {
         if (textBox.Text != String.Empty)
         {
            // if even extraHeight is negative, it will not cause the Form to be smaller than MinimumSize
            int actualHeight = textBoxHost.Height - textBoxHost.Margin.Bottom - textBoxHost.Margin.Top;
            int extraHeight = textBoxHost.PreferredSize.Height - actualHeight;
            this.Height += extraHeight;
         }
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControlMode.SelectedTab == tabPagePreview)
         {
            htmlPanelPreview.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
               Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanelPreview));

            Markdig.MarkdownPipeline pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
            string body = MarkDownUtils.ConvertToHtml(textBox.Text, _uploadsPrefix, pipeline);
            htmlPanelPreview.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         }
      }

      private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }

      private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
      {
         toggleWarningVisibility();
      }

      private void textEditForm_Shown(object sender, System.EventArgs e)
      {
         textBox.Focus();
         toggleWarningVisibility();
      }

      private void toggleWarningVisibility()
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(textBox.Text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;
      private readonly string _uploadsPrefix;
   }
}
