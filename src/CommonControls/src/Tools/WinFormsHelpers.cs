using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Tools
{
   public static class WinFormsHelpers
   {
      public static void FillComboBox(ComboBox comboBox, IEnumerable<string> choices, string defaultChoice)
      {
         foreach (string choice in choices)
         {
            comboBox.Items.Add(choice);
         }

         string selectedChoice = null;
         foreach (string choice in comboBox.Items.Cast<string>())
         {
            if (choice == defaultChoice)
            {
               selectedChoice = choice;
            }
         }

         if (selectedChoice != null)
         {
            comboBox.SelectedItem = selectedChoice;
         }
         else
         {
            comboBox.SelectedIndex = 0;
         }
      }
   }
}

