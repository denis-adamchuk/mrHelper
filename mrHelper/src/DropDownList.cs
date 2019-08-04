using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelperUI
{
   public class DropDownList : ComboBox
   {
      new public event EventHandler SelectedIndexChanged;

      public DropDownList()
      {
         base.SelectionChangeCommitted +=
            (sender, args) =>
         {
            SelectedIndex = base.SelectedIndex;
         };

         base.SelectedIndexChanged +=
            (sender, args) =>
         {
            SelectedIndex = base.SelectedIndex;
         };
      }

      new public int SelectedIndex
      {
         get
         {
            return _selectedIndex;
         }

         set
         {
            _selectedIndex = value;
            base.SelectedItem = SelectedItem;
            base.SelectedText = SelectedText;
            base.Text = Text;
            SelectedIndexChanged?.Invoke(this, null);
         }
      }

      new public object SelectedItem
      {
         get
         {
            return _selectedIndex == -1 ? null : Items[_selectedIndex];
         }
      }

      new public string SelectedText
      {
         get
         {
            return _selectedIndex == -1 ? "" : GetItemText(Items[_selectedIndex]);
         }
      }

      new public string Text
      {
         get
         {
            return _selectedIndex == -1 ? "" : GetItemText(Items[_selectedIndex]);
         }
      }

      private int _selectedIndex = -1;
   }
}

