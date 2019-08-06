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
            SelectedIndexChanged?.Invoke(sender, args);
         };

         base.Disposed +=
            (sender, args) =>
         {
            System.Diagnostics.Debug.WriteLine("Disposing " + this.ToString());
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
            if (_selectedIndex > Items.Count - 1)
            {
               _selectedIndex = -1;
            }
            base.SelectedIndex = _selectedIndex;
            base.SelectedItem = SelectedItem;
            base.SelectedText = SelectedText;
            base.Text = Text;
         }
      }

      new public object SelectedItem
      {
         get
         {
            return _selectedIndex == -1 ? null : Items[_selectedIndex];
         }
         set
         {
            SelectedIndex = Items.IndexOf(value);
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
            if (DropDownStyle == ComboBoxStyle.DropDown && _customText != null)
            {
               return _customText;
            }
            return _selectedIndex == -1 ? "" : GetItemText(Items[_selectedIndex]);
         }
         set
         {
            _customText = value;
            base.Text = Text;
         }
      }

      private int _selectedIndex = -1;
      private string _customText;
   }
}

