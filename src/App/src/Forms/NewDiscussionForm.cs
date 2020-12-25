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
using mrHelper.CommonControls.Tools;
using mrHelper.Common.Tools;
using mrHelper.App.Interprocess;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm(
         DiffPosition newDiscussionPosition,
         ReportedDiscussionNote[] oldNotes,
         Action onDialogClosed,
         Func<string, bool, Task> onSubmitNewDiscussion,
         Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> onEditOldNote,
         Func<ReportedDiscussionNoteKey, Task> onDeleteOldNote,
         Func<ReportedDiscussionNoteKey?, DiffPosition, IEnumerable<ReportedDiscussionNote>> getRelatedDiscussions,
         Func<DiffPosition, DiffContext> getDiffContext)
      {
         InitializeComponent();
         this.TopMost = Program.Settings.NewDiscussionIsTopMostForm;

         applyFont(Program.Settings.MainWindowFontSizeName);
         createWPFTextBox();
         _groupBoxRelatedThreadsDefaultHeight = groupBoxRelated.Height;
         checkBoxShowRelated.Checked = false; // let's hide them for beginning

         this.Text = Constants.StartNewThreadCaption;
         labelInvisibleCharactersHint.Text = Constants.WarningOnUnescapedMarkdown;
         _newDiscussionPosition = newDiscussionPosition;
         _onDialogClosed = onDialogClosed;

         buttonCancel.ConfirmationCondition =
            () => (getNewNoteText().Length > MaximumTextLengthTocancelWithoutConfirmation) || areHistoryModifications();
         _onSubmitNewDiscussion = onSubmitNewDiscussion;
         _onEditOldNote = onEditOldNote;
         _onDeleteOldNote = onDeleteOldNote;
         _reportedNotes = oldNotes.ToList();
         _getDiffContext = getDiffContext;
         _getRelatedDiscussions = getRelatedDiscussions;

         resetCurrentNoteIndex();
         updateControlState();
      }

      async private void buttonOK_Click(object sender, EventArgs e)
      {
         if (checkModifications(out bool needSubmitNewDiscussion, out bool needSubmitModifications))
         {
            Hide();
            await submit(needSubmitNewDiscussion, needSubmitModifications);
            Close();
         }
      }

      private void buttonCancel_Click(object sender, EventArgs e)
      {
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
            updatePreview(htmlPanelPreview, getCurrentNoteText());
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
         updateInvisibleCharactersHint();
         updateModificationsHint();
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
         if (confirmNoteDeletion())
         {
            deleteNote();
            updateControlState();
         }
      }

      private void groupBoxRelated_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
      {
         if (e.Delta < 0)
         {
            buttonNextRelatedDiscussion.PerformClick();
         }
         else if (e.Delta > 0)
         {
            buttonPrevRelatedDiscussion.PerformClick();
         }
      }

      private void panelNavigation_SizeChanged(object sender, EventArgs e)
      {
         alignControlBetweenTwoOther(labelCounter, buttonPrev, buttonNext);
      }

      private void groupBoxRelated_SizeChanged(object sender, EventArgs e)
      {
         alignControlBetweenTwoOther(labelRelatedDiscussionCounter,
            buttonPrevRelatedDiscussion, buttonNextRelatedDiscussion);
      }

      private void labelCounter_SizeChanged(object sender, EventArgs e)
      {
         alignControlBetweenTwoOther(labelCounter, buttonPrev, buttonNext);
      }

      private void labelRelatedDiscussionCounter_SizeChanged(object sender, EventArgs e)
      {
         alignControlBetweenTwoOther(labelRelatedDiscussionCounter,
            buttonPrevRelatedDiscussion, buttonNextRelatedDiscussion);
      }

      private void buttonRelatedPrev_Click(object sender, EventArgs e)
      {
         Debug.Assert(_relatedDiscussions.Any());
         Debug.Assert(_relatedDiscussionIndex.HasValue);
         Debug.Assert(_relatedDiscussionIndex.Value > 0);
         decrementRelatedDiscussionIndex();
         updateRelatedDiscussionControlState();
      }

      private void buttonRelatedNext_Click(object sender, EventArgs e)
      {
         Debug.Assert(_relatedDiscussions.Any());
         Debug.Assert(_relatedDiscussionIndex.HasValue);
         Debug.Assert(_relatedDiscussionIndex.Value < _relatedDiscussions.Length);
         incrementRelatedDiscussionIndex();
         updateRelatedDiscussionControlState();
      }

      private void checkBoxShowRelated_CheckedChanged(object sender, EventArgs e)
      {
         applyRelatedThreadsVisibility();
      }

      private void panelNavigation_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
      {
         if (e.Delta < 0)
         {
            buttonNext.PerformClick();
         }
         else if (e.Delta > 0)
         {
            buttonPrev.PerformClick();
         }
      }

      private void applyRelatedThreadsVisibility()
      {
         if (!checkBoxShowRelated.Checked)
         {
            labelInvisibleCharactersHint.Location = new System.Drawing.Point(
               labelInvisibleCharactersHint.Location.X,
               labelInvisibleCharactersHint.Location.Y + _groupBoxRelatedThreadsDefaultHeight);
            checkBoxShowRelated.Location = new System.Drawing.Point(
               checkBoxShowRelated.Location.X,
               checkBoxShowRelated.Location.Y + _groupBoxRelatedThreadsDefaultHeight);
            tabControlMode.Size = new System.Drawing.Size(
               tabControlMode.Size.Width,
               tabControlMode.Size.Height + _groupBoxRelatedThreadsDefaultHeight);

            MinimumSize = new System.Drawing.Size(
               MinimumSize.Width,
               MinimumSize.Height - _groupBoxRelatedThreadsDefaultHeight);
            Size = new System.Drawing.Size(
               Size.Width,
               Size.Height - _groupBoxRelatedThreadsDefaultHeight);

            groupBoxRelated.Height = 0;
            groupBoxRelated.Visible = false;
         }
         else
         {
            groupBoxRelated.Height = _groupBoxRelatedThreadsDefaultHeight;
            groupBoxRelated.Visible = true;

            Size = new System.Drawing.Size(
               Size.Width,
               Size.Height + _groupBoxRelatedThreadsDefaultHeight);
            MinimumSize = new System.Drawing.Size(
               MinimumSize.Width,
               MinimumSize.Height + _groupBoxRelatedThreadsDefaultHeight);

            labelInvisibleCharactersHint.Location = new System.Drawing.Point(
               labelInvisibleCharactersHint.Location.X,
               labelInvisibleCharactersHint.Location.Y - _groupBoxRelatedThreadsDefaultHeight);
            checkBoxShowRelated.Location = new System.Drawing.Point(
               checkBoxShowRelated.Location.X,
               checkBoxShowRelated.Location.Y - _groupBoxRelatedThreadsDefaultHeight);
            tabControlMode.Size = new System.Drawing.Size(
               tabControlMode.Size.Width,
               tabControlMode.Size.Height - _groupBoxRelatedThreadsDefaultHeight);
         }
      }

      private void decrementRelatedDiscussionIndex()
      {
         _relatedDiscussionIndex--;
      }

      private void incrementRelatedDiscussionIndex()
      {
         _relatedDiscussionIndex++;
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
         _modifiedNoteTexts.Remove(key);
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
               _modifiedNoteTexts[note.Key] = text;
            }
            else
            {
               _modifiedNoteTexts.Remove(note.Key);
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
            if (_modifiedNoteTexts.TryGetValue(key, out string value))
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

      private void updatePreview(HtmlPanel previewPanel, string text)
      {
         previewPanel.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(previewPanel));

         var pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
         string body = MarkDownUtils.ConvertToHtml(text, String.Empty, pipeline);
         previewPanel.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
      }

      private void showDiscussionContext(DiffPosition position)
      {
         if (position == null)
         {
            htmlPanelContext.Text = "This discussion is not associated with a diff context";
            textBoxFileName.Text = "N/A";
            return;
         }

         string html = getContextHtmlText(position, out string stylesheet);
         htmlPanelContext.BaseStylesheet = stylesheet;
         htmlPanelContext.Text = html;

         string leftSideFileName = position.LeftPath;
         string rightSideFileName = position.RightPath;
         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      private string getContextHtmlText(DiffPosition position, out string stylesheet)
      {
         stylesheet = String.Empty;
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(htmlPanelContext);
         return DiffContextFormatter.GetHtml(_getDiffContext(position), fontSizePx, 2, true);
      }

      private void updateControlState()
      {
         buttonPrev.Enabled = _reportedNotes.Any() && _currentNoteIndex > 0;
         buttonNext.Enabled = _reportedNotes.Any() && _currentNoteIndex < _reportedNotes.Count();
         toolTip.SetToolTip(buttonNext,
            _currentNoteIndex < _reportedNotes.Count() - 1 ? "Go to my next discussion" : "Go to new discussion");
         buttonDelete.Enabled = !isCurrentNoteNew();

         labelCounter.Visible = !isCurrentNoteNew();
         labelCounter.Text = String.Format("{0} / {1}", _currentNoteIndex + 1, _reportedNotes.Count());

         DiffPosition position = isCurrentNoteNew()
            ? _newDiscussionPosition
            : _reportedNotes[_currentNoteIndex].Position.DiffPosition;
         showDiscussionContext(position);
         textBoxDiscussionBody.Text = getCurrentNoteText();
         checkBoxIncludeContext.Enabled = isCurrentNoteNew();

         ReportedDiscussionNoteKey? keyOpt = isCurrentNoteNew()
            ? new ReportedDiscussionNoteKey?()
            : _reportedNotes[_currentNoteIndex].Key;
         _relatedDiscussions = _getRelatedDiscussions(keyOpt, position).ToArray();
         checkBoxShowRelated.Text = String.Format("Show related threads ({0})", _relatedDiscussions.Count());
         updateRelatedDiscussionControlState();

         updatePreview(htmlPanelPreview, getCurrentNoteText());
         updateModificationsHint();
      }

      private void updateInvisibleCharactersHint()
      {
         string text = getCurrentNoteText();
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(text);
         labelInvisibleCharactersHint.Visible = areUnescapedCharacters;
      }

      private void updateModificationsHint()
      {
         labelModificationsHint.Visible = areHistoryModifications();
      }

      private static void alignControlBetweenTwoOther(Control control, Control controlLeft, Control controlRight)
      {
         int buttonPrevRightBorder = controlLeft.Location.X + controlLeft.Width;
         int space = controlRight.Location.X - buttonPrevRightBorder;
         int labelOffsetFromButtonPrev = space / 2 - control.Width / 2;
         int labelLocationX = buttonPrevRightBorder + labelOffsetFromButtonPrev;
         int labelLocationY = control.Location.Y;
         control.Location = new System.Drawing.Point(labelLocationX, labelLocationY);
      }

      private static int getLineNumberFromDiffPosition(DiffPosition position)
      {
         // TODO Add comment why Right Line is in priority here
         int defaultLineNumber = default(int);
         if (position.RightLine != null)
         {
            return int.TryParse(position.RightLine, out int lineNumber) ? lineNumber : defaultLineNumber;
         }
         else if (position.LeftLine != null)
         {
            return int.TryParse(position.LeftLine, out int lineNumber) ? lineNumber : defaultLineNumber;
         }
         else
         {
            return defaultLineNumber;
         }
      }

      private void updateRelatedDiscussionControlState()
      {
         bool areRelatedDisussionsAvailable = _relatedDiscussions.Any();
         if (areRelatedDisussionsAvailable)
         {
            if (!_relatedDiscussionIndex.HasValue)
            {
               _relatedDiscussionIndex = 0;
            }
            else
            {
               _relatedDiscussionIndex = Math.Min(_relatedDiscussionIndex.Value, _relatedDiscussions.Count() - 1);
            }
         }
         else
         {
            _relatedDiscussionIndex = null;
         }

         int currentRelatedIndex = areRelatedDisussionsAvailable ? _relatedDiscussionIndex.Value : 0;
         int currentRelatedIndexOneBased = areRelatedDisussionsAvailable ? _relatedDiscussionIndex.Value + 1 : 0;
         int totalRelatedIndex = areRelatedDisussionsAvailable ? _relatedDiscussions.Count() : 0;
         labelRelatedDiscussionCounter.Text = String.Format("{0} / {1}", currentRelatedIndexOneBased, totalRelatedIndex);
         labelRelatedDiscussionCounter.Visible = areRelatedDisussionsAvailable;

         bool allowScrollForward = areRelatedDisussionsAvailable && currentRelatedIndex < totalRelatedIndex - 1;
         bool allowScrollBackward = areRelatedDisussionsAvailable && currentRelatedIndex > 0;
         buttonPrevRelatedDiscussion.Enabled = allowScrollBackward;
         buttonNextRelatedDiscussion.Enabled = allowScrollForward;

         htmlPanelPreview.Enabled = areRelatedDisussionsAvailable;
         labelRelatedDiscussionLineNumber.Visible = areRelatedDisussionsAvailable;
         labelRelatedDiscussionAuthor.Visible = areRelatedDisussionsAvailable;
         if (areRelatedDisussionsAvailable)
         {
            var note = _relatedDiscussions[_relatedDiscussionIndex.Value];
            labelRelatedDiscussionLineNumber.Text =
               "Line: " + getLineNumberFromDiffPosition(note.Position.DiffPosition).ToString();
            labelRelatedDiscussionAuthor.Text = "Author: " + note.Content.AuthorName;
            updatePreview(htmlPanelPreviewRelatedDiscussion, note.Content.Body);
         }
         else
         {
            labelRelatedDiscussionLineNumber.Text = String.Empty;
            labelRelatedDiscussionAuthor.Text = String.Empty;
            htmlPanelPreviewRelatedDiscussion.Text = String.Empty;
         }
      }

      private bool areHistoryModifications()
      {
         return _modifiedNoteTexts.Any() || _deletedNotes.Any();
      }

      private void createWPFTextBox()
      {
         textBoxDiscussionBody = Helpers.WPFHelpers.CreateWPFTextBox(textBoxDiscussionBodyHost,
            false, String.Empty, true, !Program.Settings.DisableSpellChecker);
         textBoxDiscussionBody.KeyDown += textBoxDiscussionBody_KeyDown;
         textBoxDiscussionBody.TextChanged += textBoxDiscussionBody_TextChanged;
      }

      private bool checkModifications(out bool needSubmitNewDiscussion, out bool needSubmitModifications)
      {
         needSubmitNewDiscussion = getNewNoteText() != String.Empty;
         needSubmitModifications = areHistoryModifications();
         if (!needSubmitNewDiscussion && needSubmitModifications)
         {
            DialogResult result =
               MessageBox.Show("Do you want to apply modifications to old records only? " +
                               "New discussion will not be reported.", "Confirmation",
                               MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            switch (result)
            {
               case DialogResult.Yes:
                  needSubmitModifications = true;
                  return true;

               case DialogResult.No:
                  needSubmitModifications = false;
                  return true;

               case DialogResult.Cancel:
                  needSubmitModifications = false;
                  return false;
            }
            Debug.Assert(false);
            return false;
         }
         return true;
      }

      private bool confirmNoteDeletion()
      {
         return MessageBox.Show("Discussion note will be deleted, are you sure?", "Confirm deletion",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                             == DialogResult.Yes;
      }

      private async Task submit(bool needSubmitNewDiscussion, bool needSubmitModifications)
      {
         if (needSubmitNewDiscussion)
         {
            await submitNewDiscussion();
         }
         if (needSubmitModifications)
         {
            await submitOldEditedNotes();
            await submitOldDeletedNotes();
         }
      }

      private async Task submitNewDiscussion()
      {
         string body = getNewNoteText();
         bool needIncludeContext = checkBoxIncludeContext.Checked;
         await _onSubmitNewDiscussion?.Invoke(body, needIncludeContext);
      }

      private async Task submitOldEditedNotes()
      {
         foreach (KeyValuePair<ReportedDiscussionNoteKey, string> keyValuePair in _modifiedNoteTexts)
         {
            string discussionId = keyValuePair.Key.DiscussionId;
            int noteId = keyValuePair.Key.Id;
            string body = keyValuePair.Value;
            await _onEditOldNote(
               new ReportedDiscussionNoteKey(noteId, discussionId),
               new ReportedDiscussionNoteContent(body, String.Empty));
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

      private readonly Action _onDialogClosed;

      /// <summary>
      /// Historical notes
      /// </summary>
      private readonly Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> _onEditOldNote;
      private readonly Func<ReportedDiscussionNoteKey, Task> _onDeleteOldNote;
      private readonly List<ReportedDiscussionNote> _reportedNotes = new List<ReportedDiscussionNote>();
      private readonly List<ReportedDiscussionNoteKey> _deletedNotes = new List<ReportedDiscussionNoteKey>();

      /// <summary>
      /// Currently selected note index in _reportedNotes collection.
      /// If the index is equal to _reportedNotes size then current note is the new note.
      /// </summary>
      private int _currentNoteIndex;

      /// <summary>
      /// New note
      /// </summary>
      private readonly Func<string, bool, Task> _onSubmitNewDiscussion;

      /// <summary>
      /// Other callbacks
      /// </summary>
      private readonly Func<DiffPosition, DiffContext> _getDiffContext;
      private readonly Func<ReportedDiscussionNoteKey?, DiffPosition, IEnumerable<ReportedDiscussionNote>> _getRelatedDiscussions;

      /// <summary>
      /// DiffPosition for a note that is going to be reported
      /// </summary>
      private readonly DiffPosition _newDiscussionPosition;

      /// <summary>
      /// Cached text of a note that is going to be reported.
      /// </summary>
      private string _cachedNewNoteText = String.Empty;

      /// <summary>
      /// Related discussions for a currently selected note.
      /// </summary>
      private ReportedDiscussionNote[] _relatedDiscussions;

      /// <summary>
      /// An index within _relatedDiscussions.
      /// </summary>
      private int? _relatedDiscussionIndex;

      /// <summary>
      /// Cached texts of edited and new discussions, where Key is an index in _reportedNotes.
      /// </summary>
      private readonly Dictionary<ReportedDiscussionNoteKey, string> _modifiedNoteTexts =
         new Dictionary<ReportedDiscussionNoteKey, string>();

      /// <summary>
      /// Cached height allows to hide the groupbox and show it back by user request.
      /// </summary>
      private readonly int _groupBoxRelatedThreadsDefaultHeight;
   }
}

