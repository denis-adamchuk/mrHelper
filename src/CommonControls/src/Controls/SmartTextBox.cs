using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

namespace mrHelper.CommonControls.Controls
{
   /// <summary>
   /// Supports:
   /// - auto-completion
   /// - spell checking
   /// Note: WPF-based
   /// </summary>
   public partial class SmartTextBox : UserControl
   {
      public SmartTextBox()
      {
         InitializeComponent();
         _delayedHidingTimer.Tick +=
            (s, e) =>
            {
               hideAutoCompleteList();
               cancelDelayedHiding();
            };
      }

      public void Init(bool isReadOnly, string text, bool multiline,
         bool isSpellCheckEnabled, bool softwareOnlyRenderMode)
      {
         textBox = WPFHelpers.CreateWPFTextBox(textBoxHost, isReadOnly, text, multiline,
            isSpellCheckEnabled, softwareOnlyRenderMode);
         textBox.TextChanged += TextBox_TextChanged;
         textBox.LostFocus += TextBox_LostFocus;
         textBox.KeyDown += TextBox_KeyDown;
         textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
      }

      private static readonly char GitLabLabelPrefixChar = '@';

      public struct AutoCompletionEntity
      {
         public enum EntityType
         {
            User
         }

         public AutoCompletionEntity(string hint, string name, EntityType type, Func<Image> getImage)
         {
            Hint = hint;
            Name = name;
            Type = type;
            GetImage = getImage;
         }

         public string Hint { get; }
         public string Name { get; }
         public EntityType Type { get; }
         public Func<Image> GetImage { get; }
      }

      // Compares names and hints so that it look like Gitlab Web UI auto-completion
      private class EntityComparer : IComparer<AutoCompletionEntity>
      {
         public EntityComparer(string substr)
         {
            _substr = substr;
         }

         public int Compare(AutoCompletionEntity x, AutoCompletionEntity y)
         {
            bool xMatchesHint = StringUtils.ContainsNoCase(x.Hint, _substr);
            bool xMatchesName = StringUtils.ContainsNoCase(x.Name, _substr);
            bool yMatchesHint = StringUtils.ContainsNoCase(y.Hint, _substr);
            bool yMatchesName = StringUtils.ContainsNoCase(y.Name, _substr);

            // Not all cases are implemented
            Debug.Assert(xMatchesHint || xMatchesName);
            Debug.Assert(yMatchesHint || yMatchesName);

            if (xMatchesHint && yMatchesHint)
            {
               if (xMatchesName)
               {
                  if (yMatchesName)
                  {
                     return String.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                  }
                  else
                  {
                     return -1;
                  }
               }
               else
               {
                  if (yMatchesName)
                  {
                     return 1;
                  }
                  else
                  {
                     return String.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                  }
               }
            }
            else if (xMatchesHint && !yMatchesHint)
            {
               if (!xMatchesName && yMatchesName)
               {
                  return 1;
               }
               else if (xMatchesName && yMatchesName)
               {
                  return -1;
               }
            }
            else if (!xMatchesHint && yMatchesHint)
            {
               if (xMatchesName && !yMatchesName)
               {
                  return -1;
               }
               else if (xMatchesName && yMatchesName)
               {
                  return 1;
               }
            }
            else if (!xMatchesHint && !yMatchesHint)
            {
               return String.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
            }

            Debug.Assert(false);
            return 0;
         }

         private readonly string _substr;
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

      public void SetAutoCompletionEntities(IEnumerable<AutoCompletionEntity> entities)
      {
         _autoCompletionEntities = entities;
      }

      public override string Text
      {
         get
         {
            return textBox.Text;
         }
         set
         {
            textBox.Text = value;
         }
      }

      private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
      {
         if (_listBoxAutoComplete != null)
         {
            refreshAutoCompleteList();
         }
         else
         {
            showAutoCompleteList();
         }
         OnTextChanged(e);
      }

      private void TextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_listBoxAutoComplete != null && !_listBoxAutoComplete.Focused)
         {
            hideAutoCompleteList();
         }
      }

      private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         OnKeyDown(new System.Windows.Forms.KeyEventArgs(WPFHelpers.GetKeysOnWPFKeyDown(e.Key)));
      }

      private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Down)
         {
            e.Handled = activateAutoCompleteList();
         }
         else if (e.Key == System.Windows.Input.Key.Escape)
         {
            e.Handled = hideAutoCompleteList();
         }

         if (!e.Handled)
         {
            OnPreviewKeyDown(new PreviewKeyDownEventArgs(WPFHelpers.GetKeysOnWPFKeyDown(e.Key)));
         }
      }

      private void listBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Space)
         {
            applyAutoCompleteListSelection();
            hideAutoCompleteList();
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Up && (sender as ListBox).SelectedIndex == 0)
         {
            hideAutoCompleteList();
            e.Handled = true;
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
            else
            {
               hideAutoCompleteList();
            }
         }
      }

      private void listBox_Click(object sender, EventArgs e)
      {
         applyAutoCompleteListSelection();
         hideAutoCompleteList();
      }

      private void listBox_Format(object sender, ListControlConvertEventArgs e)
      {
         AutoCompletionEntity item = (AutoCompletionEntity)(e.ListItem);
         e.Value = format(item);
      }

      private void listBox_LostFocus(object sender, EventArgs e)
      {
         hideAutoCompleteList();
      }

      private string format(AutoCompletionEntity entity)
      {
         switch (entity.Type)
         {
            case AutoCompletionEntity.EntityType.User:
               return String.Format("{0} ({1}{2})", entity.Hint, GitLabLabelPrefixChar, entity.Name);

            default:
               Debug.Assert(false);
               return entity.Name;
         }
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
      private static TextUtils.WordInfo getCurrentWord(System.Windows.Controls.TextBox txt)
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

      private IEnumerable<AutoCompletionEntity> getMatchingEntities()
      {
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBox);
         if (!currentWordInfo.IsValid || currentWordInfo.Word.Length < 2)
         {
            return Array.Empty<AutoCompletionEntity>();
         }

         EntityComparer comparer = new EntityComparer(currentWordInfo.Word);
         return _autoCompletionEntities?
            .Where(entity => StringUtils.ContainsNoCase(entity.Hint, currentWordInfo.Word)
                          || StringUtils.ContainsNoCase(entity.Name, currentWordInfo.Word))
            .OrderBy(entity => entity, comparer);
      }

      private void refreshAutoCompleteList()
      {
         Debug.Assert(_listBoxAutoComplete != null);
         _listBoxAutoComplete.Items.Clear();

         AutoCompletionEntity[] entities = getMatchingEntities().ToArray();
         if (!entities.Any())
         {
            hideAutoCompleteList();
            return;
         }

         fillListBox(_listBoxAutoComplete, entities);
         resizeListBox(_listBoxAutoComplete, entities);
      }

      private void showAutoCompleteList()
      {
         Debug.Assert(_listBoxAutoComplete == null);

         AutoCompletionEntity[] entities = getMatchingEntities().ToArray();
         if (!entities.Any())
         {
            return;
         }

         ListBox listBox = createListBox();
         fillListBox(listBox, entities);
         resizeListBox(listBox, entities);
         bindPopupWindowToListBox(listBox);
         showPopupWindow();
         _listBoxAutoComplete = listBox;
      }

      private void bindPopupWindowToListBox(ListBox listBox)
      {
         _popupWindow.SetContent(listBox, PopupWindowPadding);
      }

      private void showPopupWindow()
      {
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBox);
         Debug.Assert(currentWordInfo.IsValid);

         System.Windows.Point position = textBox.GetRectFromCharacterIndex(currentWordInfo.Start).BottomLeft;
         Point pt = PointToScreen(new Point((int)position.X, (int)(position.Y)));
         _popupWindow.Show(pt);
      }

      private class ListBoxEx : ListBox
      {
         const int WM_NCLBUTTONDOWN = 0x00A1;
         const int WM_NCRBUTTONDOWN = 0x00A4;

         public ListBoxEx()
         {
            DoubleBuffered = true;
         }

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
            //FormattingEnabled = true,
            DrawMode = DrawMode.OwnerDrawVariable,
            Size = new Size(200, 200)
         };
         listBox.Click += new System.EventHandler(listBox_Click);
         listBox.Font = this.Font;
         //listBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(listBox_Format);
         listBox.KeyDown += new System.Windows.Forms.KeyEventHandler(listBox_KeyDown);
         listBox.PreviewKeyDown += listBox_PreviewKeyDown;
         listBox.LostFocus += listBox_LostFocus;
         listBox.NCMouseButtonDown += listBox_NCMouseButtonDown;
         listBox.DrawItem += ListBox_DrawItem;
         listBox.MeasureItem += ListBox_MeasureItem;
         listBox.SizeChanged += ListBox_SizeChanged;

         // If we don't create control manually here, it is created on
         // showPopupWindow() call and resets size to a default one.
         listBox.CreateControl();
         return listBox;
      }

      private void ListBox_SizeChanged(object sender, EventArgs e)
      {
         Console.Write("");
      }

      private void ListBox_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         ListBox listBox = sender as ListBox;
         AutoCompletionEntity item = (AutoCompletionEntity)listBox.Items[e.Index];
         string itemText = format(item);
         SizeF textSize = e.Graphics.MeasureString(itemText, listBox.Font, listBox.Width);
         int textWidth = (int)Math.Ceiling(textSize.Width);

         e.ItemHeight = 32;
         e.ItemWidth = textWidth + 32;
      }

      private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
      {
         ListBox listBox = sender as ListBox;
         AutoCompletionEntity item = (AutoCompletionEntity)listBox.Items[e.Index];
         string itemText = format(item);
         var imageRect = new Rectangle(e.Bounds.X, e.Bounds.Y, 32, 32);
         var textRect = new Rectangle(e.Bounds.X + 32, e.Bounds.Y + (e.Bounds.Height - e.Font.Height) / 2, e.Bounds.Width - 32, e.Bounds.Height);

         e.DrawBackground();
         Image image = item.GetImage();
         if (image != null)
         {
            e.Graphics.DrawImage(item.GetImage(), imageRect);
         }
         else
         {
            using (Brush b = new SolidBrush(Color.Gray))
            {
               e.Graphics.FillEllipse(b, imageRect);
            }
         }
         e.Graphics.DrawString(itemText, e.Font, new SolidBrush(e.ForeColor), textRect);
         e.DrawFocusRectangle();
      }

      private void listBox_NCMouseButtonDown(object sender, EventArgs e)
      {
         cancelDelayedHiding();
      }

      private void fillListBox(ListBox listBox, AutoCompletionEntity[] objects)
      {
         listBox.Items.AddRange(objects.Cast<object>().ToArray());
      }

      private void resizeListBox(ListBox listBox, AutoCompletionEntity[] objects)
      {
         AutoCompletionEntity longestObject = objects
            .OrderByDescending(e => format(e).Length).First();
         int preferredWidth = TextRenderer.MeasureText(format(longestObject), listBox.Font).Width;
         if (objects.Length > MaxRowsToShowInListBox)
         {
            preferredWidth += SystemInformation.VerticalScrollBarWidth;
         }
         preferredWidth += 32;

         // Cannot use listBox.ItemHeight because it does not change on high DPI
         int singleLineWithoutBorderHeight = 32;
         int calcPreferredHeight(int rows) => rows * singleLineWithoutBorderHeight;
         listBox.MaximumSize = new Size(preferredWidth, calcPreferredHeight(MaxRowsToShowInListBox));
         listBox.Size = new Size(preferredWidth, calcPreferredHeight(objects.Length));
      }

      private bool activateAutoCompleteList()
      {
         if (_listBoxAutoComplete != null)
         {
            _listBoxAutoComplete.SelectedIndex = 0;
            _listBoxAutoComplete.Focus();
            cancelDelayedHiding(); // Focus() caused Form Deactivation and scheduled list box hiding, stop it
            return true;
         }
         return false;
      }

      private bool hideAutoCompleteList()
      {
         if (_listBoxAutoComplete != null)
         {
            _popupWindow.Close();
            _listBoxAutoComplete = null;

            Focus();
            return true;
         }
         return false;
      }

      private void applyAutoCompleteListSelection()
      {
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBox);
         if (_listBoxAutoComplete.SelectedItem == null || !currentWordInfo.IsValid)
         {
            return;
         }

         string substitutionWord = ((AutoCompletionEntity)(_listBoxAutoComplete.SelectedItem)).Name;

         hideAutoCompleteList(); // Hide before Text change to avoid TextChange event handling

         textBox.Text = TextUtils.ReplaceWord(textBox.Text, currentWordInfo, substitutionWord);
         textBox.SelectionStart = currentWordInfo.Start + substitutionWord.Length;
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
      private PopupWindow _popupWindow =
         new PopupWindow(autoClose: false, borderRadius: null);
      private IEnumerable<AutoCompletionEntity> _autoCompletionEntities;

      private static readonly Padding PopupWindowPadding = new Padding(1, 2, 1, 2);
      private static readonly int MaxRowsToShowInListBox = 5;
   }
}

