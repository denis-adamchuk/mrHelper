using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
         textBox.AcceptsReturn = multiline;
         textBox.IsReadOnly = isReadOnly;
         textBox.Text = text;
         textBox.SelectionStart = text.Length;
         textBox.VerticalContentAlignment = multiline ?
            System.Windows.VerticalAlignment.Top : System.Windows.VerticalAlignment.Stretch;
         textBox.VerticalAlignment = multiline ?
            System.Windows.VerticalAlignment.Stretch : System.Windows.VerticalAlignment.Center;
         textBox.SpellCheck.IsEnabled = isSpellCheckEnabled;
         textBox.Loaded += (s, e) =>
         {
            double verticalPadding = multiline ? 4 : (this.Height - textBox.ActualHeight) / 2.0;
            textBox.Padding = new System.Windows.Thickness(0, verticalPadding, 0, verticalPadding);
            if (softwareOnlyRenderMode)
            {
               System.Windows.PresentationSource source = System.Windows.PresentationSource.FromVisual(textBox);
               if (source.CompositionTarget is System.Windows.Interop.HwndTarget hwndTarget)
               {
                  hwndTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
               }
            }
         };
      }

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
         if (entities == null)
         {
            throw new ArgumentException("Value cannot be null: entities");
         }
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

      public int GetSelectionStart()
      {
         return textBox.SelectionStart;
      }

      public void SetSelectionStart(int start)
      {
         textBox.SelectionStart = start;
      }

      public int GetSelectionLength()
      {
         return textBox.SelectionLength;
      }

      private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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

      private void textBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
      {
         if (_listBoxAutoComplete != null && !_listBoxAutoComplete.Focused)
         {
            hideAutoCompleteList();
         }
      }

      private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         OnKeyDown(new System.Windows.Forms.KeyEventArgs(WPFHelpers.GetKeysOnWPFKeyDown(e.Key)));
      }

      private void textBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

      private void listBox_LostFocus(object sender, EventArgs e)
      {
         hideAutoCompleteList();
      }

      private void listBox_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         ListBox listBox = sender as ListBox;
         e.ItemHeight = AutoCompletionItemHeight;
         e.ItemWidth = listBox.Width; // see resizeListBox()
      }

      private void listBox_DrawItem(object sender, DrawItemEventArgs e)
      {
         int imageHeight = AutoCompletionImageHeight;
         int imageWidth = AutoCompletionImageWidth;
         int imagePaddingRight = AutoCompletionImageRightPadding;
         int x = e.Bounds.X + ListBoxPaddingLeft;

         e.DrawBackground();

         ListBox listBox = sender as ListBox;
         AutoCompletionEntity item = (AutoCompletionEntity)listBox.Items[e.Index];
         Image image = item.GetImage();

         Rectangle imageRect = new Rectangle(
            x,
            e.Bounds.Y + (e.Bounds.Height - imageHeight) / 2,
            imageWidth,
            imageHeight);

         if (image != null)
         {
            WinFormsHelpers.DrawClippedCircleImage(e.Graphics, image, imageRect);
         }
         else
         {
            using (Brush grayBrush = new SolidBrush(Color.Gray))
            {
               e.Graphics.FillEllipse(grayBrush, imageRect);
            }
         }

         Rectangle textRect = new Rectangle(
            x + imageWidth + imagePaddingRight,
            e.Bounds.Y + (e.Bounds.Height - e.Font.Height) / 2,
            e.Bounds.Width - imageWidth - imagePaddingRight,
            e.Bounds.Height);

         using (Brush textBrush = new SolidBrush(e.ForeColor))
         {
            string itemText = format(item);
            e.Graphics.DrawString(itemText, e.Font, textBrush, textRect);
         }

         e.DrawFocusRectangle();
      }

      private void listBox_NCMouseButtonDown(object sender, EventArgs e)
      {
         cancelDelayedHiding();
      }

      private IEnumerable<AutoCompletionEntity> getMatchingEntities()
      {
         TextUtils.WordInfo currentWordInfo = getCurrentWord(textBox);
         if (!currentWordInfo.IsValid || currentWordInfo.Word.Length < 2)
         {
            return Array.Empty<AutoCompletionEntity>();
         }

         EntityComparer comparer = new EntityComparer(currentWordInfo.Word);
         return _autoCompletionEntities
            .Where(entity => StringUtils.ContainsNoCase(entity.Hint, currentWordInfo.Word)
                          || StringUtils.ContainsNoCase(entity.Name, currentWordInfo.Word))
            .OrderBy(entity => entity, comparer);
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
            DrawMode = DrawMode.OwnerDrawVariable,
            Font = this.Font
         };
         listBox.Click += listBox_Click;
         listBox.KeyDown += listBox_KeyDown;
         listBox.PreviewKeyDown += listBox_PreviewKeyDown;
         listBox.LostFocus += listBox_LostFocus;
         listBox.NCMouseButtonDown += listBox_NCMouseButtonDown;
         listBox.DrawItem += listBox_DrawItem;
         listBox.MeasureItem += listBox_MeasureItem;

         // If we don't create control manually here, it is created on
         // showPopupWindow() call and resets size to a default one.
         listBox.CreateControl();
         return listBox;
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
         preferredWidth += AutoCompletionImageRightPadding + AutoCompletionImageWidth;
         preferredWidth += ListBoxPaddingLeft + ListBoxPaddingRight;

         // Cannot use listBox.ItemHeight because it does not change on high DPI
         int singleLineWithoutBorderHeight = AutoCompletionItemHeight;
         int calcPreferredHeight(int rows) => rows * singleLineWithoutBorderHeight;
         listBox.MaximumSize = new Size(preferredWidth, calcPreferredHeight(MaxRowsToShowInListBox));
         listBox.Size = new Size(preferredWidth, calcPreferredHeight(objects.Length));
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

      private static string format(AutoCompletionEntity entity)
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

      private readonly System.Windows.Forms.Timer _delayedHidingTimer = new System.Windows.Forms.Timer
      {
         Interval = 250
      };

      private ListBox _listBoxAutoComplete;
      private PopupWindow _popupWindow =
         new PopupWindow(autoClose: false, borderRadius: null);
      private IEnumerable<AutoCompletionEntity> _autoCompletionEntities = new AutoCompletionEntity[] { };

      private static readonly char GitLabLabelPrefixChar = '@';

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private int AutoCompletionItemHeight => scale(42);
      private int AutoCompletionImageWidth => scale(40);
      private int AutoCompletionImageHeight => AutoCompletionImageWidth;
      private int AutoCompletionImageRightPadding => scale(10);
      private int ListBoxPaddingLeft => scale(5);
      private int ListBoxPaddingRight => scale(10);

      private static readonly Padding PopupWindowPadding = new Padding(1, 2, 1, 2);
      private static readonly int MaxRowsToShowInListBox = 5;
   }
}

