using System;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class TextEditForm : CustomFontForm
   {
      internal TextEditForm(string caption, string initialText, bool editable, bool multiline,
         Control extraActionsControl)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);
         Text = caption;

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

      private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }

      private void ViewDiscussionItemForm_Shown(object sender, System.EventArgs e)
      {
         textBox.Focus();
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;
   }
}
