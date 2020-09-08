using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class TextBoxWithUserAutoComplete : UserControl
   {
      public TextBoxWithUserAutoComplete()
      {
         InitializeComponent();
      }

      public void HideAutoCompleteBox()
      {
         if (!_suppressExternalHideRequests)
         {
            hideAutoCompleteList();
         }
      }

      public void SetUsers(IEnumerable<User> users)
      {
         _users = users;
      }

      private void textBoxAutoComplete_TextChanged(object sender, EventArgs e)
      {
         showAutoCompleteList();
      }

      private void textBoxAutoComplete_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Down)
         {
            activateAutoCompleteList();
         }
      }

      private void textBoxAutoComplete_Leave(object sender, EventArgs e)
      {
         hideAutoCompleteList();
      }

      private void textBoxAutoComplete_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
      {
         if (e.KeyCode == Keys.Escape)
         {
            hideAutoCompleteList();
         }
      }

      private void listBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Space)
         {
            applyAutoCompleteListSelection();
            hideAutoCompleteList();
         }
         else if (e.KeyCode == Keys.Up && (sender as ListBox).SelectedIndex == 0)
         {
            hideAutoCompleteList();
         }
      }

      private void listBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
      {
         if (e.KeyCode == Keys.Escape)
         {
            hideAutoCompleteList();
         }
         else if (e.KeyCode == Keys.Tab)
         {
            applyAutoCompleteListSelection();
            hideAutoCompleteList();
         }
      }

      private void listBox_Click(object sender, EventArgs e)
      {
         applyAutoCompleteListSelection();
         hideAutoCompleteList();
      }

      private void listBox_Format(object sender, ListControlConvertEventArgs e)
      {
         User item = (User)(e.ListItem);
         e.Value = formatUser(item);
      }

      private void ListBox_LostFocus(object sender, EventArgs e)
      {
         hideAutoCompleteList();
      }

      private string formatUser(User user)
      {
         return String.Format("{0} ({1}{2})", user.Name, Common.Constants.Constants.GitLabLabelPrefix, user.Username);
      }

      private string getLastWord(TextBox txt)
      {
         return txt.Text.Split(' ').LastOrDefault() ?? String.Empty;
      }

      private void showAutoCompleteList()
      {
         string lastWord = getLastWord(textBoxAutoComplete).ToLower();
         object[] objects = _users?
            .Where(user => user.Name.ToLower().Contains(lastWord) || user.Username.ToLower().Contains(lastWord))
            .Cast<object>()
            .ToArray() ?? Array.Empty<object>();

         hideAutoCompleteList();
         if (lastWord == String.Empty || objects.Length == 0)
         {
            return;
         }

         ListBox listBox = createListBox();
         fillListBox(listBox, objects);
         resizeListBox(listBox, objects);
         createPopupWindow(listBox);
         showPopupWindow();
         _listBoxAutoComplete = listBox;

         Focus();
      }

      private void showPopupWindow()
      {
         string lastWord = getLastWord(textBoxAutoComplete);
         int currentPosition = Math.Max(0, textBoxAutoComplete.SelectionStart - lastWord.Length);
         Point position = textBoxAutoComplete.GetPositionFromCharIndex(currentPosition);

         Point pt = PointToScreen(new Point(position.X, position.Y + textBoxAutoComplete.Height));
         _popupWindow.Show(pt);
      }

      private void createPopupWindow(ListBox listBox)
      {
         _popupWindow = new PopupWindow(listBox, PopupWindowPadding);
      }

      private ListBox createListBox()
      {
         ListBox listBox = new ListBox();
         listBox.BorderStyle = BorderStyle.None;
         listBox.FormattingEnabled = true;
         listBox.Click += new System.EventHandler(listBox_Click);
         listBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(listBox_Format);
         listBox.KeyDown += new System.Windows.Forms.KeyEventHandler(listBox_KeyDown);
         listBox.PreviewKeyDown += listBox_PreviewKeyDown;
         listBox.LostFocus += ListBox_LostFocus;
         return listBox;
      }

      private void fillListBox(ListBox listBox, object[] objects)
      {
         listBox.Items.AddRange(objects);
      }

      private void resizeListBox(ListBox listBox, object[] objects)
      {
         User longestObject = objects.Cast<User>().OrderByDescending(user => formatUser(user).Length).First();
         int preferredWidth = TextRenderer.MeasureText(formatUser(longestObject), Font).Width;
         if (objects.Length > MaxRowsToShowInListBox)
         {
            preferredWidth += SystemInformation.VerticalScrollBarWidth;
         }

         int calcPreferredHeight(int rows) => rows * listBox.ItemHeight;
         listBox.Size = new Size(preferredWidth, calcPreferredHeight(objects.Length));
         listBox.MaximumSize = new Size(preferredWidth, calcPreferredHeight(MaxRowsToShowInListBox));
      }

      private void activateAutoCompleteList()
      {
         if (_listBoxAutoComplete != null)
         {
            // _listBoxAutoComplete.Focus() causes Form.Deactivate() which should normally hide the list box
            // but in this particular case it should not.
            _suppressExternalHideRequests = true;
            _listBoxAutoComplete.Focus();
            _suppressExternalHideRequests = false;
            _listBoxAutoComplete.SelectedIndex = 0;
         }
      }

      private void hideAutoCompleteList()
      {
         var popupWindow = _popupWindow;
         _popupWindow = null;
         popupWindow?.Close();
         popupWindow?.Dispose();
         _listBoxAutoComplete = null;
      }

      private void applyAutoCompleteListSelection()
      {
         if (_listBoxAutoComplete.SelectedItem == null)
         {
            return;
         }

         string lastWord = getLastWord(textBoxAutoComplete);
         string substitutionWord = ((User)(_listBoxAutoComplete.SelectedItem)).Username;
         textBoxAutoComplete.Text =
            textBoxAutoComplete.Text.Substring(0, textBoxAutoComplete.Text.Length - lastWord.Length);
         textBoxAutoComplete.AppendText(Common.Constants.Constants.GitLabLabelPrefix);
         textBoxAutoComplete.AppendText(substitutionWord);
      }

      private ListBox _listBoxAutoComplete;
      private PopupWindow _popupWindow;
      private IEnumerable<User> _users;
      private bool _suppressExternalHideRequests;

      private static readonly Padding PopupWindowPadding = new Padding(1, 2, 1, 2);
      private static readonly int MaxRowsToShowInListBox = 5;
   }
}

