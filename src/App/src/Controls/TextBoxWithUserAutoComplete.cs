using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
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
         _delayedHidingTimer.Tick +=
            (s, e) =>
            {
               hideAutoCompleteList();
               cancelDelayedHiding();
            };
      }

      public enum HidingReason
      {
         FormDeactivation,
         FormMovedOrResized
      }

      public void HideAutoCompleteBox(HidingReason reason)
      {
         if (reason == HidingReason.FormDeactivation)
         {
            // Need to delay hiding on form deactivation when a mouse click on list box occurs
            // to not hide the list box immediately but process the click event.
            scheduleDelayedHiding();
            return;
         }

         hideAutoCompleteList();
      }

      public void SetUsers(IEnumerable<User> users)
      {
         _users = users;
      }

      public override string Text
      {
         get
         {
            return textBoxAutoComplete.Text;
         }
         set
         {
            textBoxAutoComplete.Text = value;
         }
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
         else
         {
            OnKeyDown(e);
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
            if (!e.Modifiers.HasFlag(Keys.Shift))
            {
               applyAutoCompleteListSelection();
            }
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

      private void listBox_LostFocus(object sender, EventArgs e)
      {
         hideAutoCompleteList();
      }

      private string formatUser(User user)
      {
         return String.Format("{0} ({1}{2})", user.Name, Constants.GitLabLabelPrefix, user.Username);
      }

      private StringUtils.WordInfo getCurrentWord(RichTextBox txt)
      {
         return StringUtils.GetCurrentWord(txt.Text, txt.SelectionStart - 1);
      }

      private void showAutoCompleteList()
      {
         StringUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
         if (!currentWordInfo.IsValid)
         {
            return;
         }

         string currentWord = currentWordInfo.Word.ToLower();
         string pureCurrentWord = currentWord.StartsWith(Constants.GitLabLabelPrefix)
            ? currentWord.Substring(Constants.GitLabLabelPrefix.Length) : currentWord;
         object[] objects = _users?
            .Where(user => user.Name.ToLower().Contains(pureCurrentWord)
                        || user.Username.ToLower().Contains(pureCurrentWord))
            .Cast<object>()
            .ToArray() ?? Array.Empty<object>();

         hideAutoCompleteList();
         if (currentWord == String.Empty || objects.Length == 0)
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
         StringUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
         if (!currentWordInfo.IsValid)
         {
            return;
         }

         Point position = textBoxAutoComplete.GetPositionFromCharIndex(currentWordInfo.Start);
         Point pt = PointToScreen(new Point(position.X, position.Y + textBoxAutoComplete.Height));
         _popupWindow.Show(pt);
      }

      private void createPopupWindow(ListBox listBox)
      {
         _popupWindow = new PopupWindow(listBox, PopupWindowPadding);
      }

      private class ListBoxEx : ListBox
      {
         const int WM_NCLBUTTONDOWN = 0x00A1;
         const int WM_NCRBUTTONDOWN = 0x00A4;
         protected override void WndProc(ref Message m)
         {
            if (m.Msg == WM_NCLBUTTONDOWN || m.Msg == WM_NCRBUTTONDOWN)
            {
               NCMouseButtonDown?.Invoke(this, null);
            }
            base.WndProc(ref m);
         }

         internal event EventHandler NCMouseButtonDown;
      }

      private ListBox createListBox()
      {
         ListBoxEx listBox = new ListBoxEx();
         listBox.BorderStyle = BorderStyle.None;
         listBox.FormattingEnabled = true;
         listBox.Click += new System.EventHandler(listBox_Click);
         listBox.Font = this.Font;
         listBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(listBox_Format);
         listBox.KeyDown += new System.Windows.Forms.KeyEventHandler(listBox_KeyDown);
         listBox.PreviewKeyDown += listBox_PreviewKeyDown;
         listBox.LostFocus += listBox_LostFocus;
         listBox.NCMouseButtonDown += listBox_NCMouseButtonDown;
         return listBox;
      }

      private void listBox_NCMouseButtonDown(object sender, EventArgs e)
      {
         cancelDelayedHiding();
      }

      private void fillListBox(ListBox listBox, object[] objects)
      {
         listBox.Items.AddRange(objects);
      }

      private void resizeListBox(ListBox listBox, object[] objects)
      {
         User longestObject = objects.Cast<User>().OrderByDescending(user => formatUser(user).Length).First();
         int preferredWidth = TextRenderer.MeasureText(formatUser(longestObject), listBox.Font).Width;
         if (objects.Length > MaxRowsToShowInListBox)
         {
            preferredWidth += SystemInformation.VerticalScrollBarWidth;
         }

         // Cannot use listBox.ItemHeight because it does not change on high DPI
         int singleLineWithoutBorderHeight = TextRenderer.MeasureText(Alphabet, listBox.Font).Height;
         int calcPreferredHeight(int rows) => rows * singleLineWithoutBorderHeight;
         listBox.Size = new Size(preferredWidth, calcPreferredHeight(objects.Length));
         listBox.MaximumSize = new Size(preferredWidth, calcPreferredHeight(MaxRowsToShowInListBox));
      }

      private void activateAutoCompleteList()
      {
         if (_listBoxAutoComplete != null)
         {
            _listBoxAutoComplete.SelectedIndex = 0;
            _listBoxAutoComplete.Focus();
            cancelDelayedHiding(); // Focus() caused Form Deactivation and scheduled list box hiding, stop it
         }
      }

      private void hideAutoCompleteList()
      {
         PopupWindow popupWindow = _popupWindow;
         _popupWindow = null;
         popupWindow?.Close();
         popupWindow?.Dispose();
         _listBoxAutoComplete = null;
      }

      private void applyAutoCompleteListSelection()
      {
         StringUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
         if (_listBoxAutoComplete.SelectedItem == null || !currentWordInfo.IsValid)
         {
            return;
         }

         string prefix = textBoxAutoComplete.Text.Substring(0, currentWordInfo.Start);
         string substitutionWord = Constants.GitLabLabelPrefix + ((User)(_listBoxAutoComplete.SelectedItem)).Username;
         string suffix = textBoxAutoComplete.Text.Substring(currentWordInfo.Start + currentWordInfo.Word.Length);
         textBoxAutoComplete.Text = String.Format("{0}{1}{2}", prefix, substitutionWord, suffix);
         textBoxAutoComplete.SelectionStart = currentWordInfo.Start + substitutionWord.Length;
      }

      private void cancelDelayedHiding()
      {
         _delayedHidingTimer.Stop();
      }

      private void scheduleDelayedHiding()
      {
         _delayedHidingTimer.Start();
      }

      private System.Windows.Forms.Timer _delayedHidingTimer = new System.Windows.Forms.Timer
      {
         Interval = 250
      };

      private ListBox _listBoxAutoComplete;
      private PopupWindow _popupWindow;
      private IEnumerable<User> _users;

      private static readonly Padding PopupWindowPadding = new Padding(1, 2, 1, 2);
      private static readonly int MaxRowsToShowInListBox = 5;

      private static readonly string Alphabet =
         "ABCDEFGHIJKLMONPQRSTUVWXYZabcdefghijklmonpqrstuvwxyz1234567890!@#$%^&*()";
   }
}

