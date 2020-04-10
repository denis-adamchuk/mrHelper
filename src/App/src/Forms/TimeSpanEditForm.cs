using System;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   public partial class EditTimeForm : CustomFontForm
   {
      public EditTimeForm(TimeSpan span)
      {
         InitializeComponent();
         numericUpDownH.Value = span.Hours;
         numericUpDownM.Value = span.Minutes;
         numericUpDownS.Value = span.Seconds;

         applyFont(Program.Settings.MainWindowFontSizeName);

         buttonCancel.ConfirmationCondition = () => !span.Equals(TimeSpan);
      }

      private void NumericUpDown_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      public TimeSpan TimeSpan
      {
         get
         {
            int h = Decimal.ToInt32(numericUpDownH.Value);
            int m = Decimal.ToInt32(numericUpDownM.Value);
            int s = Decimal.ToInt32(numericUpDownS.Value);
            return new TimeSpan(h, m, s);
         }
      }
   }
}

