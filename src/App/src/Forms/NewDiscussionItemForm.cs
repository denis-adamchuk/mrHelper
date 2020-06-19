﻿using System;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionItemForm : CustomFontForm
   {
      internal NewDiscussionItemForm(string caption, string initialText = "")
      {
         InitializeComponent();
         this.Text = caption;
         this.textBox.Text = initialText;
         if (initialText != String.Empty)
         {
            // if even extraHeight is negative, it will not cause the Form to be smaller than MinimumSize
            int extraHeight = textBox.PreferredHeight - textBox.Height;
            this.Height += extraHeight;

            // a simple solution to disable text auto-selection on Form show (see https://stackoverflow.com/a/3537816)
            textBox.SelectionStart = initialText.Length;
            textBox.DeselectAll();
         }

         applyFont(Program.Settings.MainWindowFontSizeName);

         buttonCancel.ConfirmationCondition =
            () => initialText != String.Empty
                  ? textBox.Text != initialText
                  : textBox.TextLength > MaximumTextLengthTocancelWithoutConfirmation;
      }

      internal string Body => textBox.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      private void NewDiscussionItemForm_Load(object sender, EventArgs e)
      {
         this.ActiveControl = textBox;
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;
   }
}
