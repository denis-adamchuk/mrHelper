using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Constants;
using mrHelper.CommonNative;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;
using mrHelper.App.Interprocess;
using System.Drawing;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm(IGitCommandService git,
         DiffPosition newDiscussionPosition,
         Func<string, bool, Task> onSubmitNewDiscussion,
         ReportedDiscussionNote[] oldNotes,
         Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> onEditOldNote,
         Func<ReportedDiscussionNoteKey, Task> onDeleteOldNote,
         Action onShowDiscussions,
         Action onDialogClosed)
      {
         InitializeComponent();
         this.TopMost = Program.Settings.NewDiscussionIsTopMostForm;

         applyFont(Program.Settings.MainWindowFontSizeName);
         createWPFTextBox();

         this.Text = Constants.StartNewThreadCaption;
         labelNoteAboutInvisibleCharacters.Text = Constants.WarningOnUnescapedMarkdown;
         _newDiscussionPosition = newDiscussionPosition;
         _git = git;
         _onShowDiscussions = onShowDiscussions;
         _onDialogClosed = onDialogClosed;

         buttonCancel.ConfirmationCondition =
            () => getNewNoteText().Length > MaximumTextLengthTocancelWithoutConfirmation;
         _onSubmitNewDiscussion = onSubmitNewDiscussion;
         _onEditOldNote = onEditOldNote;
         _onDeleteOldNote = onDeleteOldNote;
         _reportedNotes = oldNotes.ToList();

         resetCurrentNoteIndex();
         updateControlState();
      }

      async private void buttonOK_Click(object sender, EventArgs e)
      {
         Hide();
         await submitNewDiscussion();
         await submitOldEditedNotes();
         await submitOldDeletedNotes();
         Close();
      }

      async private void buttonCancel_Click(object sender, EventArgs e)
      {
         Hide();
         await submitOldEditedNotes();
         await submitOldDeletedNotes();
         Close();
      }

      private void NewDiscussionForm_FormClosed(object sender, FormClosedEventArgs e)
      {
         _onDialogClosed();
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControlMode.SelectedTab == tabPagePreview)
         {
            updatePreview();
         }
      }

      private void textBoxDiscussionBody_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }

      private void textBoxDiscussionBody_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
      {
         saveNoteText(_currentNoteIndex, textBoxDiscussionBody.Text);
         updateInvisibleCharactersLabel(textBoxDiscussionBody.Text);
      }

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         Helpers.WPFHelpers.InsertCodePlaceholderIntoTextBox(textBoxDiscussionBody);
         textBoxDiscussionBody.Focus();
      }

      private void newDiscussionForm_Shown(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
         textBoxDiscussionBody.Focus();
      }

      private void buttonPrev_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Any());
         Debug.Assert(_currentNoteIndex > 0);
         decrementCurrentNoteIndex();
         updateControlState();
      }


      private void buttonNext_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Any());
         Debug.Assert(_currentNoteIndex < _reportedNotes.Count());
         incrementCurrentNoteIndex();
         updateControlState();
      }

      private void buttonDelete_Click(object sender, EventArgs e)
      {
         if (MessageBox.Show("Discussion note will be deleted, are you sure?", "Confirm deletion",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
         {
            deleteNote();
            updateControlState();
         }
      }

      private void buttonDiscussions_Click(object sender, EventArgs e)
      {
         _onShowDiscussions?.Invoke();
      }

      private void panelNavigation_SizeChanged(object sender, EventArgs e)
      {
         updateLabelCounterPosition();
      }

      private void labelCounter_TextChanged(object sender, EventArgs e)
      {
         updateLabelCounterPosition();
      }

      private void decrementCurrentNoteIndex()
      {
         _currentNoteIndex--;
      }

      private void incrementCurrentNoteIndex()
      {
         if (_currentNoteIndex < _reportedNotes.Count() - 1)
         {
            _currentNoteIndex++;
         }
         else
         {
            resetCurrentNoteIndex();
         }
      }

      private void resetCurrentNoteIndex()
      {
         _currentNoteIndex = getNewNoteFakeIndex();
      }

      private void deleteNote()
      {
         ReportedDiscussionNoteKey key = _reportedNotes[_currentNoteIndex].Key;
         _deletedNotes.Add(key);
         _cachedNoteText.Remove(key);
         _reportedNotes.RemoveAt(_currentNoteIndex);
      }

      private void saveNoteText(int index, string text)
      {
         if (index == getNewNoteFakeIndex())
         {
            _cachedNewNoteText = text;
         }
         else
         {
            ReportedDiscussionNote note = _reportedNotes[index];
            if (note.Content.Body != text)
            {
               _cachedNoteText[note.Key] = text;
            }
         }
      }

      private string getCurrentNoteText()
      {
         if (isCurrentNoteNew())
         {
            return getNewNoteText();
         }
         else
         {
            ReportedDiscussionNoteKey key = _reportedNotes[_currentNoteIndex].Key;
            if (_cachedNoteText.TryGetValue(key, out string value))
            {
               return value;
            }
            else
            {
               return _reportedNotes[_currentNoteIndex].Content.Body;
            }
         }
      }

      private string getNewNoteText()
      {
         return _cachedNewNoteText;
      }

      private void updatePreview()
      {
         htmlPanelPreview.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanelPreview));

         var pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
         string body = MarkDownUtils.ConvertToHtml(textBoxDiscussionBody.Text, String.Empty, pipeline);
         htmlPanelPreview.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
      }

      private void showDiscussionContext(DiffPosition position, IGitCommandService git)
      {
         if (position == null)
         {
            htmlPanelContext.Text = "This discussion is not associated with a diff context";
            textBoxFileName.Text = "N/A";
            return;
         }

         string html = getContextHtmlText(position, git, out string stylesheet);
         htmlPanelContext.BaseStylesheet = stylesheet;
         htmlPanelContext.Text = html;

         string leftSideFileName = position.LeftPath;
         string rightSideFileName = position.RightPath;
         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      private string getContextHtmlText(DiffPosition position, IGitCommandService git, out string stylesheet)
      {
         stylesheet = String.Empty;

         DiffContext? context;
         try
         {
            ContextDepth depth = new ContextDepth(0, 3);
            IContextMaker textContextMaker = new SimpleContextMaker(git);
            context = textContextMaker.GetContext(position, depth);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is ContextMakingException)
            {
               string errorMessage = "Cannot render HTML context.";
               ExceptionHandlers.Handle(errorMessage, ex);
               return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
            }
            throw;
         }

         Debug.Assert(context.HasValue);
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(htmlPanelContext);
         return DiffContextFormatter.GetHtml(context.Value, fontSizePx, 2, true);
      }

      private void updateControlState()
      {
         buttonPrev.Enabled = _reportedNotes.Any() && _currentNoteIndex > 0;
         buttonNext.Enabled = _reportedNotes.Any() && _currentNoteIndex < _reportedNotes.Count();
         toolTip.SetToolTip(buttonNext,
            _currentNoteIndex < _reportedNotes.Count() - 1 ? "Go to my next discussion" : "Go to new discussion");
         buttonDelete.Enabled = !isCurrentNoteNew();
         buttonOK.Enabled = isCurrentNoteNew();

         labelCounter.Visible = !isCurrentNoteNew();
         labelCounter.Text = String.Format("{0} / {1}", _currentNoteIndex + 1, _reportedNotes.Count());

         DiffPosition position = isCurrentNoteNew()
            ? _newDiscussionPosition
            : _reportedNotes[_currentNoteIndex].Position.DiffPosition;
         showDiscussionContext(position, _git);
         textBoxDiscussionBody.Text = getCurrentNoteText();
         checkBoxIncludeContext.Enabled = isCurrentNoteNew();

         updatePreview();
      }

      private void updateInvisibleCharactersLabel(string text)
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private void updateLabelCounterPosition()
      {
         int buttonPrevRightBorder = buttonPrev.Location.X + buttonPrev.Width;
         int space = buttonNext.Location.X - buttonPrevRightBorder;
         int labelOffsetFromButtonPrev = space / 2 - labelCounter.Width / 2;
         int labelLocationX = buttonPrevRightBorder + labelOffsetFromButtonPrev;
         int labelLocationY = labelCounter.Location.Y;
         labelCounter.Location = new System.Drawing.Point(labelLocationX, labelLocationY);
      }

      private void createWPFTextBox()
      {
         textBoxDiscussionBody = Helpers.WPFHelpers.CreateWPFTextBox(textBoxDiscussionBodyHost,
            false, String.Empty, true, !Program.Settings.DisableSpellChecker);
         textBoxDiscussionBody.KeyDown += textBoxDiscussionBody_KeyDown;
         textBoxDiscussionBody.TextChanged += textBoxDiscussionBody_TextChanged;
      }

      private async Task submitNewDiscussion()
      {
         string body = getNewNoteText();
         bool needIncludeContext = checkBoxIncludeContext.Checked;
         await _onSubmitNewDiscussion?.Invoke(body, needIncludeContext);
      }

      private async Task submitOldEditedNotes()
      {
         foreach (KeyValuePair<ReportedDiscussionNoteKey, string> keyValuePair in _cachedNoteText)
         {
            string discussionId = keyValuePair.Key.DiscussionId;
            int noteId = keyValuePair.Key.Id;
            string body = keyValuePair.Value;
            await _onEditOldNote(
               new ReportedDiscussionNoteKey(noteId, discussionId),
               new ReportedDiscussionNoteContent(body));
         }
      }

      private async Task submitOldDeletedNotes()
      {
         foreach (ReportedDiscussionNoteKey item in _deletedNotes)
         {
            await _onDeleteOldNote(new ReportedDiscussionNoteKey(item.Id, item.DiscussionId));
         }
      }

      private bool isCurrentNoteNew()
      {
         return _currentNoteIndex == getNewNoteFakeIndex();
      }

      private int getNewNoteFakeIndex()
      {
         return _reportedNotes.Count();
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;

      private readonly IGitCommandService _git;
      private readonly Action _onShowDiscussions;
      private readonly Action _onDialogClosed;

      /// <summary>
      /// Historical notes
      /// </summary>
      private readonly Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> _onEditOldNote;
      private readonly Func<ReportedDiscussionNoteKey, Task> _onDeleteOldNote;
      private readonly List<ReportedDiscussionNote> _reportedNotes = new List<ReportedDiscussionNote>();
      private readonly List<ReportedDiscussionNoteKey> _deletedNotes = new List<ReportedDiscussionNoteKey>();

      /// <summary>
      /// New note
      /// </summary>
      private readonly Func<string, bool, Task> _onSubmitNewDiscussion;
      private readonly DiffPosition _newDiscussionPosition;

      /// <summary>
      /// Currently selected note index in _reportedNotes collection.
      /// If the index is equal to _reportedNotes size then current note is the new note.
      /// </summary>
      private int _currentNoteIndex;

      /// <summary>
      /// Cached texts of edited and new discussions, where Key is an index in _reportedNotes.
      /// </summary>
      private readonly Dictionary<ReportedDiscussionNoteKey, string> _cachedNoteText =
         new Dictionary<ReportedDiscussionNoteKey, string>();
      private string _cachedNewNoteText = String.Empty;
   }
}

