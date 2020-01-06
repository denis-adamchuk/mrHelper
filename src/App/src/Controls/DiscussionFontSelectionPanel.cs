using System;
using System.Windows.Forms;
using mrHelper.Common.Constants;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   public partial class DiscussionFontSelectionPanel : UserControl
   {
      public DiscussionFontSelectionPanel(Action<string> onFontSelectionChanged)
      {
         _onFontSelectionChanged = onFontSelectionChanged;

         InitializeComponent();
         WinFormsHelpers.FillComboBox(comboBoxFonts,
            Constants.DiscussionsWindowFontSizeChoices, Program.Settings.MainWindowFontSizeName);
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

