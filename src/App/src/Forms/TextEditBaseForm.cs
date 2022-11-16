using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.CommonControls.Controls;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   internal abstract partial class TextEditBaseForm : CustomFontForm
   {
      internal TextEditBaseForm(string caption, string initialText, bool editable, bool multiline,
         string uploadsPrefix, IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         Text = caption;
         labelNoteAboutInvisibleCharacters.Text = Constants.WarningOnUnescapedMarkdown;
         _uploadsPrefix = uploadsPrefix;
         _avatarImageCache = avatarImageCache;
         initSmartTextBox(initialText, editable, multiline, fullUserList);

         buttonCancel.ConfirmationCondition =
            () => initialText != String.Empty
                  ? textBox.Text != initialText
                  : textBox.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;

         applyFont(Program.Settings.MainWindowFontSizeName);
         adjustFormHeight();
      }

      protected void setExtraActionsControl(Control extraActionsControl)
      {
         if (extraActionsControl != null)
         {
            panelExtraActions.Controls.Add(extraActionsControl);
            extraActionsControl.Dock = DockStyle.Fill;
         }
      }

      public string Body => textBox.Text;

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControlMode.SelectedTab == tabPagePreview)
         {
            Markdig.MarkdownPipeline pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
            string body = MarkDownUtils.ConvertToHtml(textBox.Text, _uploadsPrefix, pipeline, htmlPanelPreview);
            htmlPanelPreview.BaseStylesheet = ResourceHelper.SetControlFontSizeToCommonCss(htmlPanelPreview);
            htmlPanelPreview.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         }
      }

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && e.Modifiers.HasFlag(Keys.Control))
         {
            buttonOK.PerformClick();
         }
      }

      private void textBox_TextChanged(object sender, EventArgs e)
      {
         toggleWarningVisibility();
      }

      private void form_Deactivate(object sender, EventArgs e)
      {
         textBox.HideAutoCompleteBox(SmartTextBox.HidingReason.FormDeactivation);
      }

      private void form_LocationChanged(object sender, EventArgs e)
      {
         textBox.HideAutoCompleteBox(SmartTextBox.HidingReason.FormMovedOrResized);
      }

      private void form_SizeChanged(object sender, EventArgs e)
      {
         textBox.HideAutoCompleteBox(SmartTextBox.HidingReason.FormMovedOrResized);
      }

      private void form_Shown(object sender, System.EventArgs e)
      {
         textBox.Focus();
         toggleWarningVisibility();
      }

      private void initSmartTextBox(string initialText, bool editable, bool multiline, IEnumerable<User> fullUserList)
      {
         textBox.Init(!editable, initialText, multiline,
            !Program.Settings.DisableSpellChecker, Program.Settings.WPFSoftwareOnlyRenderMode);

         if (fullUserList != null && _avatarImageCache != null)
         {
            textBox.SetAutoCompletionEntities(fullUserList
               .Select(user => new SmartTextBox.AutoCompletionEntity(
                  user.Name, user.Username, SmartTextBox.AutoCompletionEntity.EntityType.User,
                  () => _avatarImageCache.GetAvatar(user))));
         }

         textBox.KeyDown += textBox_KeyDown;
         textBox.TextChanged += textBox_TextChanged;
      }

      private void adjustFormHeight()
      {
         if (textBox.Text != String.Empty)
         {
            // 1. Obtain full preferred height for text box
            int preferredHeight = textBox.PreferredSize.Height;

            // 2. Force text box to measure its size basing on the  size
            textBox.GetPreferredSize(textBox.Size);

            // 3. Compute extra pixels to stretch out form if needed
            int extraHeight = preferredHeight - textBox.Height;

            // 4. Stretch out the form
            if (extraHeight > 0)
            {
               extraHeight += ExtraHeight; // some extra space for better look
               this.Height += extraHeight;
            }
         }
      }

      protected void onInsertCode()
      {
         if (textBox != null)
         {
            SmartTextBoxHelpers.InsertCodePlaceholderIntoTextBox(textBox);
            textBox.Focus();
         }
      }

      private void toggleWarningVisibility()
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(textBox.Text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private int ExtraHeight => scale(10);

      private static readonly int MaximumTextLengthTocancelWithoutConfirmation = 5;
      private readonly string _uploadsPrefix;
      private readonly AvatarImageCache _avatarImageCache;
   }
}
