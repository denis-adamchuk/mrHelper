using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Forms
{
   internal partial class ReplyOnDiscussionFormBase : TextEditForm
   {
      internal ReplyOnDiscussionFormBase(string uploadsPrefix, IEnumerable<User> fullUserList)
         : base("Reply on a Discussion", "", true, true, uploadsPrefix, fullUserList)
      {
      }
   }

   internal partial class ReplyOnDiscussionForm1 : ReplyOnDiscussionFormBase
   {
      internal ReplyOnDiscussionForm1(string resolveText, bool proposeUserToToggleResolveOnReply, string uploadsPrefix, IEnumerable<User> fullUserList)
         : base(uploadsPrefix, fullUserList)
      {
         Controls.NoteEditPanel p = new Controls.NoteEditPanel(resolveText, proposeUserToToggleResolveOnReply, onInsertCode);
         _isResolveChecked = () => p.IsResolveActionChecked;
         SetExtraActionsControl(p);
      }

      internal bool IsResolveActionChecked => _isResolveChecked();
      private readonly Func<bool> _isResolveChecked;
   }

   internal partial class ReplyOnDiscussionForm2 : ReplyOnDiscussionFormBase
   {
      internal ReplyOnDiscussionForm2(string uploadsPrefix, IEnumerable<User> fullUserList)
         : base(uploadsPrefix, fullUserList)
      {
         Controls.ReplyOnRelatedNotePanel p = new Controls.ReplyOnRelatedNotePanel(true, onInsertCode);
         _isCloseDialogChecked = () => p.IsCloseDialogActionChecked;
         SetExtraActionsControl(p);
      }

      internal bool IsCloseDialogActionChecked => _isCloseDialogChecked();
      private readonly Func<bool> _isCloseDialogChecked;
   }

   internal partial class SimpleTextEditForm : TextEditForm
   {
      internal SimpleTextEditForm(string caption, string initialText, bool multiline, string uploadsPrefix, IEnumerable<User> fullUserList)
         : base(caption, initialText, true, multiline, uploadsPrefix, fullUserList)
      {
      }
   }

   internal partial class EditNoteForm1 : TextEditForm
   {
      internal EditNoteForm1(string initialText, string uploadsPrefix, IEnumerable<User> fullUserList)
         : base("Edit Discussion Note", initialText, true, true, uploadsPrefix, fullUserList)
      {
         SetExtraActionsControl(new Controls.NoteEditPanel(onInsertCode));
      }
   }

   internal partial class ViewNoteForm1 : TextEditForm
   {
      internal ViewNoteForm1(string initialText, string uploadsPrefix)
         : base("View Discussion Note", initialText, false, true, uploadsPrefix, null)
      {
      }
   }

   internal partial class TextEditForm : CustomFontForm
   {
      internal TextEditForm(string caption, string initialText, bool editable, bool multiline,
         string uploadsPrefix, IEnumerable<User> fullUserList)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         Text = caption;
         labelNoteAboutInvisibleCharacters.Text = Constants.WarningOnUnescapedMarkdown;
         _uploadsPrefix = uploadsPrefix;

         createWPFTextBox(initialText, editable, multiline, fullUserList);

         buttonCancel.ConfirmationCondition =
            () => initialText != String.Empty
                  ? textBox.Text != initialText
                  : textBox.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;

         applyFont(Program.Settings.MainWindowFontSizeName);
         adjustFormHeight();
      }

      protected void SetExtraActionsControl(Control extraActionsControl)
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
            htmlPanelPreview.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
               Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanelPreview));

            Markdig.MarkdownPipeline pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
            string body = MarkDownUtils.ConvertToHtml(textBox.Text, _uploadsPrefix, pipeline, htmlPanelPreview);
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

      private void textEditForm_Shown(object sender, System.EventArgs e)
      {
         textBox.Focus();
         toggleWarningVisibility();
      }

      private void createWPFTextBox(string initialText, bool editable, bool multiline, IEnumerable<User> fullUserList)
      {
         textBox.Init(!editable, initialText, multiline,
            !Program.Settings.DisableSpellChecker, Program.Settings.WPFSoftwareOnlyRenderMode);

         textBox.SetAutoCompletionEntities(fullUserList
            .Select(user => new SmartTextBox.AutoCompletionEntity(
               user.Name, user.Username, SmartTextBox.AutoCompletionEntity.EntityType.User)));

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
               extraHeight += 10; // some extra space for better look
               this.Height += extraHeight;
            }
         }
      }

      protected void onInsertCode()
      {
         if (textBox != null)
         {
            DynamicHelpers.InsertCodePlaceholderIntoTextBox(textBox);
            textBox.Focus();
         }
      }

      private void toggleWarningVisibility()
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(textBox.Text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private static readonly int MaximumTextLengthTocancelWithoutConfirmation = 5;
      private readonly string _uploadsPrefix;
   }
}
