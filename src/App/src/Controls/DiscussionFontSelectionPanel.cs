using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   public partial class DiscussionFontSelectionPanel : UserControl
   {
      public DiscussionFontSelectionPanel(Action<string> onFontSelectionChanged)
      {
         _onFontSelectionChanged = onFontSelectionChanged;

         InitializeComponent();
         WinFormsHelpers.FillComboBox(comboBoxFonts,
            Common.Constants.Constants.DiscussionsWindowFontSizeChoices, Program.Settings.MainWindowFontSizeName);
      }

      private void comboBoxFonts_SelectionChangeCommitted(object sender, EventArgs e)
      {
         if (comboBoxFonts.SelectedItem == null)
         {
            return;
         }

         string font = comboBoxFonts.SelectedItem.ToString();
         _onFontSelectionChanged(font);
      }

      Action<string> _onFontSelectionChanged;
   }
}

