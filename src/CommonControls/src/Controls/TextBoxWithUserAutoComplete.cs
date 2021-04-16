using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.CommonControls.Controls
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

      private static readonly char GitLabLabelPrefixChar = '@';

      public struct User
      {
         public User(string name, string username)
         {
            Name = name;
            Username = username;
         }

         public string Name { get; }
         public string Username { get; }
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
         return String.Format("{0} ({1}{2})", user.Name, GitLabLabelPrefixChar, user.Username);
      }

      /// <summary>
      /// Extends functionality.
      /// Trims result of TextUtils.GetCurrentWord():
      /// - removes all non-letter characters before '@' character.
      /// - removes all non-letter characters after a series of letter characters (after '@')
      ///   e.g. "[@abcd]" converted to "abcd"
      /// The following strings are considered incorrect:
      /// - strings without '@'
      /// - strings with letter-characters prior to '@'
      /// - strings where there is a non-letter character next to '@'
      /// </summary>
      private TextUtils.WordInfo getCurrentWord(RichTextBox txt)
      {
         int selectionStartPosition = txt.SelectionStart - 1;
         TextUtils.WordInfo word = TextUtils.GetCurrentWord(txt.Text, selectionStartPosition);
         if (!word.IsValid)
         {
            return word;
         }

         int? firstLabelPrefixPosition = null;
         int? firstLetterPosition = null;
         int? firstNonLetterAfterLabelPrefixPosition = null;
         for (int iPosition = 0; iPosition < word.Word.Length; ++iPosition)
         {
            char currentChar = word.Word[iPosition];
            if (Char.IsLetter(currentChar))
            {
               if (!firstLetterPosition.HasValue)
               {
                  firstLetterPosition = iPosition;
               }
            }
            else if (currentChar == GitLabLabelPrefixChar)
            {
               if (!firstLabelPrefixPosition.HasValue)
               {
                  firstLabelPrefixPosition = iPosition;
               }
            }
            else if (!firstNonLetterAfterLabelPrefixPosition.HasValue && firstLabelPrefixPosition.HasValue)
            {
               firstNonLetterAfterLabelPrefixPosition = iPosition;
            }
         }

         if (!firstLetterPosition.HasValue
          || !firstLabelPrefixPosition.HasValue
          ||  firstLetterPosition.Value < firstLabelPrefixPosition.Value)
         {
            return TextUtils.WordInfo.Invalid;
         }

         int firstCharAfterLabelPrefixPosition = firstLabelPrefixPosition.Value + 1;
         int textLength = firstNonLetterAfterLabelPrefixPosition.HasValue
            ? firstNonLetterAfterLabelPrefixPosition.Value - firstCharAfterLabelPrefixPosition
            : word.Word.Length - firstCharAfterLabelPrefixPosition;
         if (textLength == 0)
         {
            return TextUtils.WordInfo.Invalid;
         }

         int startPosition = word.Start + firstCharAfterLabelPrefixPosition;
         string trimmedWord = word.Word.Substring(firstCharAfterLabelPrefixPosition, textLength);
         if (selectionStartPosition < startPosition || selectionStartPosition >= startPosition + trimmedWord.Length)
         {
            return TextUtils.WordInfo.Invalid;
         }

         return new TextUtils.WordInfo(startPosition, trimmedWord);
      }

      private void showAutoCompleteList()
      {
         hideAutoCompleteList();

         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
         if (!currentWordInfo.IsValid || currentWordInfo.Word.Length < 2)
         {
            return;
         }

         bool doesWordContainWord(string container, string containee) =>
               container.ToLower().Contains(containee.ToLower());

         object[] objects = _users?
            .Where(user => doesWordContainWord(user.Name, currentWordInfo.Word)
                        || doesWordContainWord(user.Username, currentWordInfo.Word))
            .Cast<object>()
            .ToArray() ?? Array.Empty<object>();
         if (objects.Length == 0)
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
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
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
         ListBoxEx listBox = new ListBoxEx
         {
            BorderStyle = BorderStyle.None,
            FormattingEnabled = true
         };
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
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBoxAutoComplete);
         if (_listBoxAutoComplete.SelectedItem == null || !currentWordInfo.IsValid)
         {
            return;
         }

         string substitutionWord = ((User)(_listBoxAutoComplete.SelectedItem)).Username;
         textBoxAutoComplete.Text = TextUtils.ReplaceWord(textBoxAutoComplete.Text, currentWordInfo, substitutionWord);
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

      private readonly System.Windows.Forms.Timer _delayedHidingTimer = new System.Windows.Forms.Timer
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

