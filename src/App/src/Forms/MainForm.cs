using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : Form
   {
      internal MainForm()
      {
         InitializeComponent();

         comboBoxFonts.Items.Add("8.25");
         comboBoxFonts.Items.Add("9.00");
         comboBoxFonts.Items.Add("9.75");
         comboBoxFonts.Items.Add("11.25");
         comboBoxFonts.SelectedIndex = 3;
      }

      private void comboBoxFonts_SelectedIndexChanged(object sender, EventArgs e)
      {
         this.Font = new Font(this.Font.FontFamily, float.Parse(comboBoxFonts.SelectedItem.ToString()));
      }

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);
         Debug.Assert(textBox1.Location.X >= 0 && textBox1.Location.Y >= 0);
      }
   }
}

