﻿using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.App.Controls;
using mrHelper.App.Interprocess;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.CommonNative;
using mrHelper.CommonControls.Tools;
using mrHelper.App.Helpers;
using GitLabSharp.Entities;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : ThemedForm
   {
      internal NewDiscussionForm(
         DiffPosition newDiscussionPosition,
         ReportedDiscussionNote[] oldNotes,
         ProjectKey projectKey,
         string webUrl,
         Func<DiffPosition, bool, DiffPosition> onScrollPosition,
         Action onDialogClosed,
         Action<string> onGoToNote,
         Func<string, bool, DiffPosition, Task> onSubmitNewDiscussion,
         Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> onEditOldNote,
         Func<ReportedDiscussionNoteKey, Task> onDeleteOldNote,
         Func<ReportedDiscussionNoteKey, string, Task> onReply,
         Func<ReportedDiscussionNoteKey?, DiffPosition, IEnumerable<ReportedDiscussionNote>> getRelatedDiscussions,
         Func<DiffPosition, DiffContext> getNewDiscussionDiffContext,
         Func<DiffPosition, DiffContext> getDiffContext,
         IEnumerable<User> fullUserList,
         IEnumerable<Project> fullProjectList,
         AvatarImageCache avatarImageCache)
      {
         InitializeComponent();
         ThemeSupport.ThemeSupportHelper.ExcludeFromProcessing(labelInvisibleCharactersHint);
         this.TopMost = Program.Settings.NewDiscussionIsTopMostForm;
         htmlPanelContext.MouseWheelEx += panelScroll_MouseWheel;

         applyFont(Program.Settings.MainWindowFontSizeName);
         _webUrl = webUrl;
         _groupBoxRelatedThreadsDefaultHeight = groupBoxRelated.Height;
         _diffContextDefaultHeight = panelHtmlContextCanvas.Height;
         Project project = fullProjectList.FirstOrDefault(p => p.Path_With_Namespace == projectKey.ProjectName);
         _imagePath = StringUtils.GetUploadsPrefix(projectKey.HostName, project?.Id ?? 0);
         _avatarImageCache = avatarImageCache;
         _fullUserList = fullUserList;
         initSmartTextBox();

         this.Text = Constants.StartNewThreadCaption;
         labelInvisibleCharactersHint.Text = Constants.WarningOnUnescapedMarkdown;

         buttonCancel.ConfirmationCondition =
            () => (getNewNoteText().Length > MaximumTextLengthTocancelWithoutConfirmation) || areHistoryModifications();

         _onDialogClosed = onDialogClosed;
         _onScrollPosition = onScrollPosition;
         _onGoToNote = onGoToNote;
         _onSubmitNewDiscussion = onSubmitNewDiscussion;
         _onEditOldNote = onEditOldNote;
         _onDeleteOldNote = onDeleteOldNote;
         _onReply = onReply;
         _getNewDiscussionDiffContext = getNewDiscussionDiffContext;
         _getDiffContext = getDiffContext;
         _getRelatedDiscussions = getRelatedDiscussions;
         ColorScheme.Modified += onColorSchemeModified;

         NewDiscussionPosition = newDiscussionPosition;
         _needIncludeContextInNewDiscussion = NewDiscussionPosition != null;

         _reportedNotes = oldNotes.ToList();
         resetCurrentNoteIndex();
      }

      private void NewDiscussionForm_Load(object sender, EventArgs e)
      {
         bool currentCheckBoxShowRelatedState = checkBoxShowRelated.Checked;
         checkBoxShowRelated.Checked = Program.Settings.ShowRelatedThreads;
         bool firedCheckedChangedEvent = currentCheckBoxShowRelatedState != checkBoxShowRelated.Checked;
         if (!firedCheckedChangedEvent) // optimization: don't execute code which is executed within event handler
         {
            updateRelatedDiscussions();
            updateControlState();
         }

         Size = MinimumSize;
      }

      async private void buttonOK_Click(object sender, EventArgs e)
      {
         await checkAndSubmit();
      }

      private void buttonCancel_Click(object sender, EventArgs e)
      {
         Close();
      }

      private void buttonScrollUp_Click(object sender, EventArgs e)
      {
         Debug.Assert(canScroll(true));
         NewDiscussionPosition = scroll(true);
         updateControlState();
      }

      private void buttonScrollDown_Click(object sender, EventArgs e)
      {
         Debug.Assert(canScroll(false));
         NewDiscussionPosition = scroll(false);
         updateControlState();
      }

      private void panelScroll_MouseWheel(object sender, HtmlPanelEx.MouseWheelExArgs e)
      {
         WinFormsHelpers.ConvertMouseWheelToClick(buttonScrollDown, buttonScrollUp, e.Delta);
      }

      private void panelScroll_MouseWheel(object sender, MouseEventArgs e)
      {
         WinFormsHelpers.ConvertMouseWheelToClick(buttonScrollDown, buttonScrollUp, e.Delta);
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

      private void textBoxDiscussionBody_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && e.Modifiers.HasFlag(Keys.Control))
         {
            buttonOK.PerformClick();
         }
      }

      private void textBoxDiscussionBody_TextChanged(object sender, EventArgs e)
      {
         saveNoteText(CurrentNoteIndex, textBoxDiscussionBody.Text);
         updateInvisibleCharactersHint();
         updateModificationsHint();
      }

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         SmartTextBoxHelpers.InsertCodePlaceholderIntoTextBox(textBoxDiscussionBody);
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
         Debug.Assert(CurrentNoteIndex > 0);
         decrementCurrentNoteIndex();
         updateControlState();
      }

      private void buttonNext_Click(object sender, EventArgs e)
      {
         Debug.Assert(_reportedNotes.Any());
         Debug.Assert(CurrentNoteIndex < _reportedNotes.Count());
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
         WinFormsHelpers.ConvertMouseWheelToClick(buttonNextRelatedDiscussion, buttonPrevRelatedDiscussion, e.Delta);
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

      private void panelCanvas_SizeChanged(object sender, EventArgs e)
      {
         if (sender == panelHtmlContextCanvas)
         {
            if (needShowDiffContext())
            {
               showDiscussionContext(getCurrentNoteDiffPosition(), htmlPanelContext);
            }
         }
         else if (sender == panelRelatedDiscussionHtmlContextCanvas)
         {
            if (needShowRelatedDiscussions())
            {
               ReportedDiscussionNote note = _relatedDiscussions[_relatedDiscussionIndex.Value];
               showDiscussionContext(note.Position.DiffPosition, htmlPanelRelatedDiscussionContext);
            }
         }
      }

      private void buttonRelatedPrev_Click(object sender, EventArgs e)
      {
         Debug.Assert(_relatedDiscussions.Any());
         Debug.Assert(_relatedDiscussionIndex.HasValue);
         Debug.Assert(_relatedDiscussionIndex.Value > 0);
         decrementRelatedDiscussionIndex();
         updateControlState();
      }

      private void buttonRelatedNext_Click(object sender, EventArgs e)
      {
         Debug.Assert(_relatedDiscussions.Any());
         Debug.Assert(_relatedDiscussionIndex.HasValue);
         Debug.Assert(_relatedDiscussionIndex.Value < _relatedDiscussions.Length);
         incrementRelatedDiscussionIndex();
         updateControlState();
      }

      private void checkBoxShowRelated_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.ShowRelatedThreads = checkBoxShowRelated.Checked;
         updateRelatedDiscussions();
         updateControlState();
      }

      private void checkBoxIncludeContext_CheckedChanged(object sender, EventArgs e)
      {
         if (isCurrentNoteNew())
         {
            _needIncludeContextInNewDiscussion = checkBoxIncludeContext.Checked;
         }
         updateRelatedDiscussions();
         updateControlState();
      }

      private void panelNavigation_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
      {
         WinFormsHelpers.ConvertMouseWheelToClick(buttonNext, buttonPrev, e.Delta);
      }

      async private void buttonReply_Click(object sender, EventArgs e)
      {
         using (ReplyOnRelatedNoteForm form = new ReplyOnRelatedNoteForm(
            _imagePath, _fullUserList, _avatarImageCache))
         {
            form.TopMost = Program.Settings.NewDiscussionIsTopMostForm;
            if (WinFormsHelpers.ShowDialogOnControl(form, this) == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Reply text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               if (form.IsCloseDialogActionChecked)
               {
                  Hide();
               }

               try
               {
                  string text = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
                  await _onReply(_relatedDiscussions[_relatedDiscussionIndex.Value].Key, text);
               }
               catch (SubmitFailedException ex)
               {
                  Clipboard.SetText(form.Body);

                  string message = 
                     "Cannot reply on a discussion at GitLab. Check your network connection.\r\n" +
                     "Your text was copied to Clipboard.";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                     MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                  if (form.IsCloseDialogActionChecked)
                  {
                     Show();
                  }
                  return;
               }

               if (form.IsCloseDialogActionChecked)
               {
                  await checkAndSubmit();
               }
            }
         }
      }

      private void linkLabelGoToRelatedDiscussion_Click(object sender, EventArgs e)
      {
         if (!needShowRelatedDiscussions())
         {
            return;
         }

         ReportedDiscussionNote note = _relatedDiscussions[_relatedDiscussionIndex.Value];
         string noteUrl = StringUtils.GetNoteUrl(_webUrl, note.Key.Id);
         _onGoToNote(noteUrl);
      }

      private bool needShowDiffContext()
      {
         return checkBoxIncludeContext.Checked && getCurrentNoteDiffPosition() != null;
      }

      private void updateDiffContextVisibility()
      {
         bool doVisible = needShowDiffContext();
         if (doVisible == panelHtmlContextCanvas.Visible)
         {
            return;
         }

         if (!doVisible)
         {
            tabControlMode.Location = new System.Drawing.Point(
               tabControlMode.Location.X,
               tabControlMode.Location.Y - _diffContextDefaultHeight);
            tabControlMode.Size = new System.Drawing.Size(
               tabControlMode.Size.Width,
               tabControlMode.Size.Height + _diffContextDefaultHeight);
            panelNavigation.Location = new System.Drawing.Point(
               panelNavigation.Location.X,
               panelNavigation.Location.Y - _diffContextDefaultHeight);
            buttonInsertCode.Location = new System.Drawing.Point(
               buttonInsertCode.Location.X,
               buttonInsertCode.Location.Y - _diffContextDefaultHeight);
            buttonOK.Location = new System.Drawing.Point(
               buttonOK.Location.X,
               buttonOK.Location.Y - _diffContextDefaultHeight);
            buttonCancel.Location = new System.Drawing.Point(
               buttonCancel.Location.X,
               buttonCancel.Location.Y - _diffContextDefaultHeight);

            MinimumSize = new System.Drawing.Size(
               MinimumSize.Width,
               MinimumSize.Height - _diffContextDefaultHeight);
            Size = new System.Drawing.Size(
               Size.Width,
               Size.Height - _diffContextDefaultHeight);

            panelHtmlContextCanvas.Height = 0;
            panelHtmlContextCanvas.Visible = false;
         }
         else
         {
            Size = new System.Drawing.Size(
               Size.Width,
               Size.Height + _diffContextDefaultHeight);
            MinimumSize = new System.Drawing.Size(
               MinimumSize.Width,
               MinimumSize.Height + _diffContextDefaultHeight);

            tabControlMode.Size = new System.Drawing.Size(
               tabControlMode.Size.Width,
               tabControlMode.Size.Height - _diffContextDefaultHeight);
            tabControlMode.Location = new System.Drawing.Point(
               tabControlMode.Location.X,
               tabControlMode.Location.Y + _diffContextDefaultHeight);
            panelNavigation.Location = new System.Drawing.Point(
               panelNavigation.Location.X,
               panelNavigation.Location.Y + _diffContextDefaultHeight);
            buttonInsertCode.Location = new System.Drawing.Point(
               buttonInsertCode.Location.X,
               buttonInsertCode.Location.Y + _diffContextDefaultHeight);
            buttonOK.Location = new System.Drawing.Point(
               buttonOK.Location.X,
               buttonOK.Location.Y + _diffContextDefaultHeight);
            buttonCancel.Location = new System.Drawing.Point(
               buttonCancel.Location.X,
               buttonCancel.Location.Y + _diffContextDefaultHeight);

            panelHtmlContextCanvas.Height = _diffContextDefaultHeight;
            panelHtmlContextCanvas.Visible = true;
         }
      }

      private bool needShowRelatedDiscussions()
      {
         return checkBoxShowRelated.Checked
             && _relatedDiscussions != null
             && _relatedDiscussions.Any()
             && _relatedDiscussionIndex != null
             && needShowDiffContext();
       }

      private void updateRelatedThreadsGroupBoxVisibility()
      {
         bool doVisible = needShowRelatedDiscussions();
         if (doVisible == groupBoxRelated.Visible)
         {
            return;
         }

         if (!doVisible)
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

            groupBoxRelated.Height = _groupBoxRelatedThreadsDefaultHeight;
            groupBoxRelated.Visible = true;
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
         CurrentNoteIndex--;
      }

      private void incrementCurrentNoteIndex()
      {
         if (CurrentNoteIndex < _reportedNotes.Count() - 1)
         {
            CurrentNoteIndex++;
         }
         else
         {
            resetCurrentNoteIndex();
         }
      }

      private void resetCurrentNoteIndex()
      {
         CurrentNoteIndex = getNewNoteFakeIndex();
      }

      private void deleteNote()
      {
         ReportedDiscussionNoteKey key = _reportedNotes[CurrentNoteIndex].Key;
         _deletedNotes.Add(key);
         _modifiedNoteTexts.Remove(key);
         _reportedNotes.RemoveAt(CurrentNoteIndex);
         updateRelatedDiscussions();
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
            ReportedDiscussionNoteKey key = _reportedNotes[CurrentNoteIndex].Key;
            if (_modifiedNoteTexts.TryGetValue(key, out string value))
            {
               return value;
            }
            else
            {
               return _reportedNotes[CurrentNoteIndex].Content.Body;
            }
         }
      }

      private string getNewNoteText()
      {
         return _cachedNewNoteText;
      }

      private void updatePreview(HtmlPanel previewPanel, string text)
      {
         Markdig.MarkdownPipeline pipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
         string body = MarkDownUtils.ConvertToHtml(text, String.Empty, pipeline, previewPanel);
         previewPanel.BaseStylesheet = ResourceHelper.ApplyFontSizeAndColorsToCSS(previewPanel);
         previewPanel.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
      }

      private void showDiscussionContext(DiffPosition position, HtmlPanel htmlPanel)
      {
         if (position == null)
         {
            htmlPanel.Text = "This discussion is not associated with code";
            return;
         }
         htmlPanel.Text = getContextHtmlText(position, htmlPanel);
      }

      private void showFileName(DiffPosition position)
      {
         if (position == null)
         {
            textBoxFileName.Text = "This discussion is not associated with code";
            return;
         }

         string leftSideFileName = position.LeftPath;
         string rightSideFileName = position.RightPath;
         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      private string getContextHtmlText(DiffPosition position, HtmlPanel htmlPanel)
      {
         DiffContext context = isCurrentNoteNew() ?
            _getNewDiscussionDiffContext(position) : _getDiffContext(position);

         string longestLine = context.IsValid() ? context.GetLongestLine() : null;
         double fontSizePt = WinFormsHelpers.GetFontSizeInPoints(htmlPanel);
         string htmlSnippet = longestLine != null ?
            DiffContextFormatter.GetHtml(longestLine, fontSizePt, null, getColorProvider()) : null;

         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(htmlPanel);
         int tableWidth = DiffContextHelpers.EstimateHtmlWidth(htmlSnippet, fontSizePx, htmlPanel.Width);
         return DiffContextFormatter.GetHtml(context, fontSizePt, tableWidth, getColorProvider());
      }

      private void updateControlState()
      {
         buttonPrev.Enabled = _reportedNotes.Any() && CurrentNoteIndex > 0;
         buttonNext.Enabled = _reportedNotes.Any() && CurrentNoteIndex < _reportedNotes.Count();
         toolTip.SetToolTip(buttonNext,
            CurrentNoteIndex < _reportedNotes.Count() - 1 ? "Go to my next discussion" : "Go to new discussion");
         buttonDelete.Enabled = !isCurrentNoteNew();

         labelCounter.Visible = !isCurrentNoteNew();
         labelCounter.Text = String.Format("{0} / {1}", CurrentNoteIndex + 1, _reportedNotes.Count());
         textBoxDiscussionBody.Text = getCurrentNoteText();

         checkBoxIncludeContext.Checked = isCurrentNoteNew()
            ? _needIncludeContextInNewDiscussion : getCurrentNoteDiffPosition() != null;
         checkBoxIncludeContext.Enabled = isCurrentNoteNew() && getCurrentNoteDiffPosition() != null;

         bool isScrollingEnabled = isCurrentNoteNew() && _needIncludeContextInNewDiscussion;
         buttonScrollUp.Visible = isScrollingEnabled;
         buttonScrollUp.Enabled = isScrollingEnabled && canScroll(true);
         buttonScrollDown.Visible = isScrollingEnabled;
         buttonScrollDown.Enabled = isScrollingEnabled && canScroll(false);

         showFileName(needShowDiffContext() ? getCurrentNoteDiffPosition() : null);
         showDiscussionContext(needShowDiffContext() ? getCurrentNoteDiffPosition() : null, htmlPanelContext);

         updateDiffContextVisibility();
         updateRelatedDiscussionControlState();

         updatePreview(htmlPanelPreview, getCurrentNoteText());
         updateModificationsHint();
      }

      private DiffPosition getCurrentNoteDiffPosition()
      {
         return isCurrentNoteNew()
            ? NewDiscussionPosition
            : _reportedNotes[CurrentNoteIndex].Position.DiffPosition;
      }

      private void updateRelatedDiscussions()
      {
         if (!needShowDiffContext())
         {
            _relatedDiscussions = Array.Empty<ReportedDiscussionNote>();
            return;
         }

         ReportedDiscussionNoteKey? keyOpt = isCurrentNoteNew()
            ? new ReportedDiscussionNoteKey?()
            : _reportedNotes[CurrentNoteIndex].Key;
         _relatedDiscussions = _getRelatedDiscussions(keyOpt, getCurrentNoteDiffPosition()).ToArray();
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
            checkBoxShowRelated.Enabled = true;
         }
         else
         {
            _relatedDiscussionIndex = null;
            checkBoxShowRelated.Enabled = false;
         }
         checkBoxShowRelated.Text = String.Format("Show related threads ({0})", _relatedDiscussions.Count());

         int currentRelatedIndex = areRelatedDisussionsAvailable ? _relatedDiscussionIndex.Value : 0;
         int currentRelatedIndexOneBased = areRelatedDisussionsAvailable ? _relatedDiscussionIndex.Value + 1 : 0;
         int totalRelatedIndex = areRelatedDisussionsAvailable ? _relatedDiscussions.Count() : 0;
         labelRelatedDiscussionCounter.Text = String.Format("{0} / {1}", currentRelatedIndexOneBased, totalRelatedIndex);
         labelRelatedDiscussionCounter.Visible = areRelatedDisussionsAvailable;

         bool allowScrollForward = areRelatedDisussionsAvailable && currentRelatedIndex < totalRelatedIndex - 1;
         bool allowScrollBackward = areRelatedDisussionsAvailable && currentRelatedIndex > 0;
         buttonPrevRelatedDiscussion.Enabled = allowScrollBackward;
         buttonNextRelatedDiscussion.Enabled = allowScrollForward;

         labelRelatedDiscussionAuthor.Visible = areRelatedDisussionsAvailable;
         avatarBoxRelatedDiscussionAvatar.Visible = areRelatedDisussionsAvailable;
         linkLabelGoToRelatedDiscussion.Visible = areRelatedDisussionsAvailable;

         updateRelatedThreadsGroupBoxVisibility();
         if (needShowRelatedDiscussions())
         {
            ReportedDiscussionNote note = _relatedDiscussions[_relatedDiscussionIndex.Value];
            bool areRefsEqual = note.Position.DiffPosition.Refs.Equals(NewDiscussionPosition.Refs);
            labelDifferentContextHint.Visible = !areRefsEqual;
            labelRelatedDiscussionAuthor.Text = String.Format("{0} -- {1}",
               note.Details.Author.Name, TimeUtils.DateTimeToStringAgo(note.Details.CreatedAt));
            toolTip.SetToolTip(labelRelatedDiscussionAuthor, TimeUtils.DateTimeToString(note.Details.CreatedAt));
            avatarBoxRelatedDiscussionAvatar.Image = _avatarImageCache.GetAvatar(note.Details.Author);
            updatePreview(htmlPanelPreviewRelatedDiscussion, note.Content.Body);
            showDiscussionContext(note.Position.DiffPosition, htmlPanelRelatedDiscussionContext);
         }
         else
         {
            labelRelatedDiscussionAuthor.Text = String.Empty;
            toolTip.SetToolTip(labelRelatedDiscussionAuthor, String.Empty);
            avatarBoxRelatedDiscussionAvatar.Image = null;
            htmlPanelPreviewRelatedDiscussion.Text = String.Empty;
            htmlPanelRelatedDiscussionContext.Text = String.Empty;
         }

         // We want to hide "Show related" check box when related discussions don't make sense or unwanted
         checkBoxShowRelated.Visible = needShowDiffContext();
      }

      private bool areHistoryModifications()
      {
         return _modifiedNoteTexts.Any() || _deletedNotes.Any();
      }

      private void initSmartTextBox()
      {
         textBoxDiscussionBody.Init(false, String.Empty, true,
            !Program.Settings.DisableSpellChecker, Program.Settings.WPFSoftwareOnlyRenderMode,
            ThemeSupport.StockColors.GetThemeColors().TextBoxBorder);

         if (_fullUserList != null && _avatarImageCache != null)
         {
            textBoxDiscussionBody.SetAutoCompletionEntities(_fullUserList
               .Select(user => new CommonControls.Controls.SmartTextBox.AutoCompletionEntity(
                  user.Name, user.Username, CommonControls.Controls.SmartTextBox.AutoCompletionEntity.EntityType.User,
                  () => _avatarImageCache.GetAvatar(user))));
         }

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

      private async Task checkAndSubmit()
      {
         if (checkModifications(out bool needSubmitNewDiscussion, out bool needSubmitModifications))
         {
            Hide();
            try
            {
               await submit(needSubmitNewDiscussion, needSubmitModifications);
            }
            catch (SubmitFailedException)
            {
               Show();
               return;
            }
            Close();
         }
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
         try
         {
            string body = getNewNoteText();
            await _onSubmitNewDiscussion?.Invoke(body, _needIncludeContextInNewDiscussion, NewDiscussionPosition);
         }
         catch (SubmitFailedException ex)
         {
            string message = "Cannot create a discussion at GitLab. Check your network connection and try again.";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            throw; // re-throw to un-hide the dialog
         }
      }

      private async Task submitOldEditedNotes()
      {
         HashSet<ReportedDiscussionNoteKey> submitted = new HashSet<ReportedDiscussionNoteKey>();
         foreach (KeyValuePair<ReportedDiscussionNoteKey, string> keyValuePair in _modifiedNoteTexts)
         {
            string discussionId = keyValuePair.Key.DiscussionId;
            int noteId = keyValuePair.Key.Id;
            string body = keyValuePair.Value;
            try
            {
               await _onEditOldNote(
                  new ReportedDiscussionNoteKey(noteId, discussionId),
                  new ReportedDiscussionNoteContent(body));
            }
            catch (SubmitFailedException ex)
            {
               StringBuilder builder = new StringBuilder();
               _modifiedNoteTexts
                  .Where(kv => !submitted.Contains(kv.Key))
                  .ToList()
                  .ForEach(kv => builder.AppendLine(kv.Value));
               Clipboard.SetText(builder.ToString());
               string message = 
                  "Cannot edit one or more discussions at GitLab. Check your network connection.\r\n" +
                  "Unsubmitted text was copied to Clipboard.";
               ExceptionHandlers.Handle(message, ex);
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               break; // don't rethrow because it is ok to close dialog if a Edit attempt failed
            }
            submitted.Add(keyValuePair.Key);
         }
      }

      private async Task submitOldDeletedNotes()
      {
         foreach (ReportedDiscussionNoteKey item in _deletedNotes)
         {
            try
            {
               await _onDeleteOldNote(new ReportedDiscussionNoteKey(item.Id, item.DiscussionId));
            }
            catch (SubmitFailedException ex)
            {
               string message = 
                  "Cannot delete one or more discussions at GitLab. " +
                  "Check your network connection and try again.";
               ExceptionHandlers.Handle(message, ex);
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               break; // don't rethrow because it is ok to close dialog if a Delete attempt failed
            }
         }
      }

      private bool isCurrentNoteNew()
      {
         return CurrentNoteIndex == getNewNoteFakeIndex();
      }

      private int getNewNoteFakeIndex()
      {
         return _reportedNotes.Count();
      }

      private DiffPosition scroll(bool up)
      {
         return _onScrollPosition(NewDiscussionPosition, up);
      }

      private bool canScroll(bool up)
      {
         Debug.Assert(isCurrentNoteNew());
         return _getNewDiscussionDiffContext(scroll(up)).IsValid();
      }

      private void onColorSchemeModified()
      {
         updateControlState();
      }

      private static Core.ContextColorProvider getColorProvider()
      {
         return new Core.ContextColorProvider(
            ColorScheme.GetColor("HTML_Diff_LineNumbers_Text").Color,
            ColorScheme.GetColor("HTML_Diff_LineNumbers_Background").Color,
            ColorScheme.GetColor("HTML_Diff_LineNumbers_Right_Border").Color,
            ColorScheme.GetColor("HTML_Diff_Text").Color,
            ColorScheme.GetColor("HTML_Diff_Unchanged_Background").Color,
            ColorScheme.GetColor("HTML_Diff_Text").Color,
            ColorScheme.GetColor("HTML_Diff_Added_Background").Color,
            ColorScheme.GetColor("HTML_Diff_Text").Color,
            ColorScheme.GetColor("HTML_Diff_Removed_Background").Color);
      }

      private static readonly int MaximumTextLengthTocancelWithoutConfirmation = 5;

      private readonly Action _onDialogClosed;

      /// <summary>
      /// Historical notes
      /// </summary>
      private readonly Func<ReportedDiscussionNoteKey, ReportedDiscussionNoteContent, Task> _onEditOldNote;
      private readonly Func<ReportedDiscussionNoteKey, Task> _onDeleteOldNote;
      private readonly Func<ReportedDiscussionNoteKey, string, Task> _onReply;
      private readonly Func<DiffPosition, DiffContext> _getNewDiscussionDiffContext;
      private readonly List<ReportedDiscussionNote> _reportedNotes = new List<ReportedDiscussionNote>();
      private readonly List<ReportedDiscussionNoteKey> _deletedNotes = new List<ReportedDiscussionNoteKey>();

      /// <summary>
      /// Currently selected note index in _reportedNotes collection.
      /// If the index is equal to _reportedNotes size then current note is the new note.
      /// </summary>
      private int CurrentNoteIndex
      {
         get
         {
            return _currentNoteIndex;
         }
         set
         {
            _currentNoteIndex = value;
            updateRelatedDiscussions();
         }
      }
      private int _currentNoteIndex;

      /// <summary>
      /// New note
      /// </summary>
      private readonly Func<string, bool, DiffPosition, Task> _onSubmitNewDiscussion;

      /// <summary>
      /// Other callbacks
      /// </summary>
      private readonly Func<DiffPosition, DiffContext> _getDiffContext;
      private readonly Func<ReportedDiscussionNoteKey?, DiffPosition, IEnumerable<ReportedDiscussionNote>> _getRelatedDiscussions;

      /// <summary>
      /// DiffPosition for a note that is going to be reported
      /// </summary>
      private DiffPosition NewDiscussionPosition
      {
         get
         {
            return _newDiscussionPosition;
         }
         set
         {
            _newDiscussionPosition = value;
            updateRelatedDiscussions();
         }
      }
      private DiffPosition _newDiscussionPosition;

      /// <summary>
      /// User-defined checkbox value for a new discussion. Saved to restore on navigation.
      /// </summary>
      private bool _needIncludeContextInNewDiscussion;

      /// <summary>
      /// Functor which scrolls a position up/down.
      /// </summary>
      private readonly Func<DiffPosition, bool, DiffPosition> _onScrollPosition;

      /// <summary>
      /// Functor which opens a note by url in Discussions view.
      /// </summary>
      private readonly Action<string> _onGoToNote;

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
      private readonly string _webUrl;

      /// <summary>
      /// Cached height allows to hide the groupbox and show it back by user request.
      /// </summary>
      private readonly int _groupBoxRelatedThreadsDefaultHeight;
      private readonly int _diffContextDefaultHeight;
      private readonly string _imagePath;
      private readonly AvatarImageCache _avatarImageCache;
      private readonly IEnumerable<User> _fullUserList;
   }
}

