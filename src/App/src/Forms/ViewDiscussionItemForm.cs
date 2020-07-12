using System;
using System.Windows;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class ViewDiscussionItemForm : CustomFontForm
   {
      internal ViewDiscussionItemForm(string caption, string initialText = "", bool editable = true)
      {
         InitializeComponent();
         Text = caption;

         createWPFTextBox(initialText, editable);
         applyFont(Program.Settings.MainWindowFontSizeName);
         adjustFormHeight();

         buttonCancel.ConfirmationCondition =
            () => initialText != String.Empty
                  ? textBox.Text != initialText
                  : textBox.Text.Length > MaximumTextLengthTocancelWithoutConfirmation;
      }

      internal string Body => textBox.Text;

      private void createWPFTextBox(string initialText, bool editable)
      {
         textBox = Helpers.WPFHelpers.CreateWPFTextBox(textBoxHost, !editable, initialText);
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
            e.Handled = false;

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
