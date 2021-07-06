using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Forms
{
   public interface IEditOrderedListViewCallback
   {
      Task<string> CanAddItem(string item, IEnumerable<string> currentItems);
   }

   internal partial class EditOrderedListViewForm : CustomFontForm
   {
      public EditOrderedListViewForm(
         string caption, string addItemCaption, string addItemHint,
         StringToBooleanCollection initialItems,
         IEditOrderedListViewCallback callback,
         bool allowReorder)
      {
         Debug.Assert(initialItems != null);

         InitializeComponent();
         this.Text = caption;
         _allowReorder = allowReorder;
         if (!_allowReorder)
         {
            buttonDown.Enabled = false;
            buttonUp.Enabled = false;
         }

         updateListView(initialItems);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _addItemHint = addItemHint;
         _addItemCaption = addItemCaption;
         _callback = callback;

         buttonCancel.ConfirmationCondition = () => !Enumerable.SequenceEqual(initialItems, Items);
      }

      public StringToBooleanCollection Items
      {
         get
         {
            return new StringToBooleanCollection(listView.Items
               .Cast<ListViewItem>()
               .Select(x => x.Tag)
               .Cast<Tuple<string, bool>>());
         }
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         e.Cancel = isChecking();
      }

      private void updateListView(StringToBooleanCollection items)
      {
         listView.Items.Clear();

         foreach (Tuple<string, bool> item in items) addListViewItem(item);
      }

      private void addListViewItem(Tuple<string, bool> item)
      {
         listView.Items.Add(new ListViewItem(item.Item1) { Tag = item });
      }

      private void updateButtonState()
      {
         bool isAnythingSelected = listView.SelectedItems.Count > 0;

         buttonUp.Enabled = !isChecking()
                         && isAnythingSelected
                         && _allowReorder
                         && listView.SelectedIndices[0] != 0;

         buttonDown.Enabled = !isChecking()
                           && isAnythingSelected
                           && _allowReorder
                           && listView.SelectedIndices[0] != listView.Items.Count - 1;

         buttonAddItem.Enabled = !isChecking();
         buttonRemoveItem.Enabled = !isChecking() && isAnythingSelected;
         buttonToggleState.Enabled = !isChecking() && isAnythingSelected;

         buttonOK.Enabled = !isChecking();
         buttonCancel.Enabled = !isChecking();

         if (isAnythingSelected)
         {
            Tuple<string, bool> tag = (Tuple<string, bool>)(listView.SelectedItems[0].Tag);
            buttonToggleState.Text = tag.Item2 ? "Disable" : "Enable";
         }
      }

      private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         listView.Refresh();
         updateButtonState();
      }

      async private void buttonAddItem_Click(object sender, EventArgs e)
      {
         using (AddItemForm form = new AddItemForm(_addItemCaption, _addItemHint))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, this) != DialogResult.OK)
            {
               return;
            }

            onStartChecking();
            try
            {
               string itemToBeAdded = await _callback.CanAddItem(form.Item,
                     listView.Items.Cast<ListViewItem>().Select(x => x.Text));
               if (itemToBeAdded != null)
               {
                  addListViewItem(new Tuple<string, bool>(itemToBeAdded, true));
               }
            }
            finally
            {
               onEndChecking();
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
            updateButtonState();
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
            buttonOK.PerformClick();
         }
      }

      private void onStartChecking()
      {
         labelChecking.Visible = true;
         updateButtonState();
      }

      private void onEndChecking()
      {
         labelChecking.Visible = false;
         updateButtonState();
      }

      private bool isChecking()
      {
         return labelChecking.Visible;
      }

      private readonly string _addItemHint;
      private readonly string _addItemCaption;
      private readonly bool _allowReorder;
      private readonly IEditOrderedListViewCallback _callback;
   }
}

