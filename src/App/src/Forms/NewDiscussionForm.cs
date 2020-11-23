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

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm(IGitCommandService git,
         DiffPosition newDiscussionPosition,
         Func<string, bool, Task> onSubmitNewDiscussion,
         ReportedDiscussionNote[] oldNotes,
         Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> onEditOldNote,
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
         _reportedNotes = oldNotes;
         _currentNoteIndex = getNewNoteFakeIndex();

         updateControlState();
         showCurrentNote();
      }

      async private void buttonOK_Click(object sender, EventArgs e)
      {
         Hide();
         await submitNewDiscussion();
         await submitOldEditedNotes();
         Close();
      }

      async private void buttonCancel_Click(object sender, EventArgs e)
      {
         Hide();
         await submitOldEditedNotes();
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
         Debug.Assert(_reportedNotes.Length > 0);
         Debug.Assert(isCurrentNoteNew() || _currentNoteIndex != 0);
         decrementCurrentNoteIndex();
         showCurrentNote();
         updateControlState();
      }


      private void buttonNext_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Length > 0);
         Debug.Assert(!isCurrentNoteNew());
         incrementCurrentNoteIndex();
         showCurrentNote();
         updateControlState();
      }

      private void buttonFirst_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Length > 0);
         Debug.Assert(isCurrentNoteNew() || _currentNoteIndex != 0);
         scrollCurrentNoteIndexToBegin();
         showCurrentNote();
         updateControlState();
      }

      private void buttonLast_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Length > 0);
         Debug.Assert(!isCurrentNoteNew());
         resetCurrentNote();
         showCurrentNote();
         updateControlState();
      }

      private void NewDiscussionForm_Resize(object sender, EventArgs e)
      {
         updateLabelCounterPosition();
         updateDiscussionsLinkLabelPosition();
      }

      private void labelCounter_TextChanged(object sender, EventArgs e)
      {
         updateLabelCounterPosition();
      }

      private void linkLabelDiscussions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         _onShowDiscussions?.Invoke();
      }

      private void showCurrentNote()
      {
         DiffPosition position = isCurrentNoteNew()
            ? _newDiscussionPosition
            : _reportedNotes[_currentNoteIndex].Position.DiffPosition;
         showDiscussionContext(position, _git);
         textBoxDiscussionBody.Text = getCurrentNoteText();
         updatePreview();
      }

      private void saveNoteText(int index, string text)
      {
         _cachedNoteText[index] = text;
      }

      private string getCurrentNoteText()
      {
         if (isCurrentNoteNew())
         {
            return getNewNoteText();
         }
         else if (_cachedNoteText.TryGetValue(_currentNoteIndex, out string value))
         {
            return value;
         }
         else
         {
            return _reportedNotes[_currentNoteIndex].Content.Body;
         }
      }

      private string getNewNoteText()
      {
         if (_cachedNoteText.TryGetValue(getNewNoteFakeIndex(), out string value))
         {
            return value;
         }
         return String.Empty;
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
         buttonFirst.Enabled = buttonPrev.Enabled = _reportedNotes.Any() && _currentNoteIndex != 0;
         buttonLast.Enabled = buttonNext.Enabled = _reportedNotes.Any() && !isCurrentNoteNew();
         buttonOK.Enabled = isCurrentNoteNew();
         checkBoxIncludeContext.Enabled = isCurrentNoteNew();
         labelCounter.Visible = !isCurrentNoteNew();
         if (!isCurrentNoteNew())
         {
            labelCounter.Text = String.Format("{0} / {1}", _currentNoteIndex + 1, _reportedNotes.Length);
         }
      }

      private void updateLabelCounterPosition()
      {
         labelCounter.Location = new System.Drawing.Point(
            buttonLast.Location.X + (buttonLast.Width / 2 - labelCounter.Width / 2), labelCounter.Location.Y);
      }

      private void updateDiscussionsLinkLabelPosition()
      {
         linkLabelDiscussions.Location = new System.Drawing.Point(
            buttonLast.Location.X + (buttonLast.Width / 2 - linkLabelDiscussions.Width / 2),
            linkLabelDiscussions.Location.Y);
      }

      private void updateInvisibleCharactersLabel(string text)
      {
         bool areUnescapedCharacters = StringUtils.DoesContainUnescapedSpecialCharacters(text);
         labelNoteAboutInvisibleCharacters.Visible = areUnescapedCharacters;
      }

      private void createWPFTextBox()
      {
         textBoxDiscussionBody = Helpers.WPFHelpers.CreateWPFTextBox(textBoxDiscussionBodyHost,
            false, String.Empty, true, !Program.Settings.DisableSpellChecker);
         textBoxDiscussionBody.KeyDown += textBoxDiscussionBody_KeyDown;
         textBoxDiscussionBody.TextChanged += textBoxDiscussionBody_TextChanged;
      }

      private void decrementCurrentNoteIndex()
      {
         _currentNoteIndex--;
      }

      private void incrementCurrentNoteIndex()
      {
         _currentNoteIndex++;
      }

      private void scrollCurrentNoteIndexToBegin()
      {
         _currentNoteIndex = 0;
      }

      private void resetCurrentNote()
      {
         _currentNoteIndex = getNewNoteFakeIndex();
      }

      private async Task submitNewDiscussion()
      {
         string body = getNewNoteText();
         bool needIncludeContext = checkBoxIncludeContext.Checked;
         await _onSubmitNewDiscussion?.Invoke(body, needIncludeContext);
      }

      private async Task submitOldEditedNotes()
      {
         foreach (KeyValuePair<int, string> keyValuePair in _cachedNoteText)
         {
            if (keyValuePair.Key != getNewNoteFakeIndex())
            {
               string discussionId = _reportedNotes[keyValuePair.Key].Key.DiscussionId;
               int noteId = _reportedNotes[keyValuePair.Key].Key.Id;
               string body = keyValuePair.Value;
               await _onEditOldNote(
                  new ReportedDiscussionNoteKey(noteId, discussionId),
                  new ReportedDiscussionNoteContent(body));
            }
         }
      }

      private bool isCurrentNoteNew()
      {
         return _currentNoteIndex == getNewNoteFakeIndex();
      }

      private int getNewNoteFakeIndex()
      {
         return _reportedNotes.Length;
      }

      private static int MaximumTextLengthTocancelWithoutConfirmation = 5;

      private readonly IGitCommandService _git;
      private readonly Action _onShowDiscussions;
      private readonly Action _onDialogClosed;

      /// <summary>
      /// Historical notes
      /// </summary>
      private readonly Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> _onEditOldNote;
      private readonly ReportedDiscussionNote[] _reportedNotes;

      /// <summary>
      /// New note
      /// </summary>
      private readonly Func<string, bool, Task> _onSubmitNewDiscussion;
      private readonly DiffPosition _newDiscussionPosition;

      /// <summary>
      /// Currently selected note index in _reportedNotes collection. If `null`, current note is the new note.
      /// </summary>
      private int _currentNoteIndex;

      /// <summary>
      /// Cached texts of edited and new discussions.
      /// </summary>
      private readonly Dictionary<int, string> _cachedNoteText = new Dictionary<int, string>();
   }
}


