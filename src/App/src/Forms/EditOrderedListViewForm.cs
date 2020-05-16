using GitLabSharp.Entities;
using mrHelper.Client.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   public interface IEditOrderedListViewCallback
   {
      Task<bool> CanAddItem(string item, IEnumerable<string> currentItems);
   }

   public partial class EditOrderedListViewForm : CustomFontForm
   {
      public EditOrderedListViewForm(
         IEnumerable<Tuple<string, bool>> initialItems,
         IEditOrderedListViewCallback callback)
      {
         Debug.Assert(initialItems != null);

         InitializeComponent();

         updateListView(initialItems);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _callback = callback;

         buttonCancel.ConfirmationCondition = () => !Enumerable.SequenceEqual(initialItems, Items);
      }

      public IEnumerable<Tuple<string, bool>> Items
      {
         get
         {
            return listView.Items
               .Cast<ListViewItem>()
               .Select(x => x.Tag)
               .Cast<Tuple<string, bool>>();
         }
      }

      private void updateListView(IEnumerable<Tuple<string, bool>> items)
      {
         listView.Items.Clear();

         foreach (Tuple<string, bool> item in items) addListViewItem(item);
      }

      private void addListViewItem(Tuple<string, bool> item)
      {
         listView.Items.Add(new ListViewItem(item.Item1) { Tag = item });
      }

      private void listView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Tuple<string, bool> tag = (Tuple<string, bool>)(e.Item.Tag);

         e.DrawBackground();

         bool isSelected = e.Item.Selected;
         if (isSelected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
         }

         string text = tag.Item1;
         Brush textBrush = isSelected ? SystemBrushes.HighlightText :
            (tag.Item2 ? SystemBrushes.ControlText : Brushes.LightGray);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, e.Bounds, format);
      }

      private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         ListView listView = (sender as ListView);
         listView.Refresh();

         if (listView.SelectedItems.Count < 1)
         {
            buttonRemoveItem.Enabled = false;
            buttonToggleState.Enabled = false;
            return;
         }

         buttonUp.Enabled = listView.SelectedIndices[0] != 0;
         buttonDown.Enabled = listView.SelectedIndices[0] != listView.Items.Count - 1;

         buttonRemoveItem.Enabled = true;
         buttonToggleState.Enabled = true;

         Tuple<string, bool> tag = (Tuple<string, bool>)(listView.SelectedItems[0].Tag);
         buttonToggleState.Text = tag.Item2 ? "Disable" : "Enable";
      }

      async private void buttonAddItem_Click(object sender, EventArgs e)
      {
         using (AddItemForm form = new AddItemForm())
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (await _callback.CanAddItem(form.Item,
                  listView.Items.Cast<ListViewItem>().Select(x => x.Text)))
            {
               addListViewItem(new Tuple<string, bool>(form.Item, true));
            }
         }
      }

      private void buttonRemoveItem_Click(object sender, EventArgs e)
      {
         if (listView.SelectedItems.Count > 0)
         {
            listView.Items.Remove(listView.SelectedItems[0]);
         }
      }

      private void buttonToggleState_Click(object sender, EventArgs e)
      {
         if (listView.SelectedItems.Count > 0)
         {
            Tuple<string, bool> tag = (Tuple<string, bool>)(listView.SelectedItems[0].Tag);
            listView.SelectedItems[0].Tag = new Tuple<string, bool>(tag.Item1, !tag.Item2);

            buttonToggleState.Text = !tag.Item2 ? "Disable" : "Enable";
         }
      }

      private void buttonUp_Click(object sender, EventArgs e)
      {
         if (listView.SelectedIndices.Count > 0)
         {
            int selectedIndex = listView.SelectedIndices[0];
            Debug.Assert(selectedIndex > 0);

            moveItem(true, selectedIndex);
         }
      }

      private void buttonDown_Click(object sender, EventArgs e)
      {
         if (listView.SelectedItems.Count > 0)
         {
            int selectedIndex = listView.SelectedIndices[0];
            Debug.Assert(selectedIndex < listView.Items.Count - 1);

            moveItem(false, selectedIndex);
         }
      }

      private void moveItem(bool up, int index)
      {
         ListViewItem selectedItem = listView.Items[index];
         listView.Items.RemoveAt(index);
         listView.Items.Insert(up ? index - 1 : index + 1, selectedItem);
      }

      private void listView_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      private readonly IEditOrderedListViewCallback _callback;
   }
}

