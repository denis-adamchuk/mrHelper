using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelperUI
{
   /// <summary>
   /// Unlike its parent class, <> preserves state of SelectedIndex, SelectedItem, SelectedText and Text properties
   /// until SelectedIndexChanged event is invoked, including cases when dropdown is expanded and user hovers but
   /// does not click on items
   /// </summary>
   public class SelectionPreservingComboBox : ComboBox
   {
      new public event EventHandler SelectedIndexChanged;

      public SelectionPreservingComboBox()
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

