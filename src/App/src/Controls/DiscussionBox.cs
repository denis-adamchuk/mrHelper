﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.DiffContextHelpers;
using mrHelper.Core;

namespace mrHelper.App.Controls
{
   internal enum ENoteSelectionRequest
   {
      First,
      Last,
      Prev,
      Next
   }

   internal class DiscussionBox : Panel
   {
      internal DiscussionBox(
         GitLabClient.SingleDiscussionAccessor accessor, IGitCommandService git,
         User currentUser, string imagePath, Discussion discussion,
         User mergeRequestAuthor,
         Action<DiscussionBox> onContentChanging,
         Action<DiscussionBox> onContentChanged,
         Action<Control> onControlGotFocus,
         Action<Control> onSetFocusToNoteProgramatically,
         Action undoFocusChangedOnClick,
         HtmlToolTipEx htmlTooltip,
         PopupWindow popupWindow,
         ConfigurationHelper.DiffContextPosition diffContextPosition,
         ConfigurationHelper.DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies,
         ContextDepth diffContextDepth,
         AvatarImageCache avatarImageCache,
         RoundedPathCache pathCache,
         EstimateWidthCache estimateWidthCache,
         string webUrl,
         IEnumerable<User> fullUserList,
         Action<string> selectNoteByUrl,
         Action<ENoteSelectionRequest, DiscussionBox> selectNoteByPosition)
      {
         Discussion = discussion;

         _accessor = accessor;
         _editor = accessor.GetDiscussionEditor();
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _imagePath = imagePath;
         _avatarImageCache = avatarImageCache;
         _pathCache = pathCache;
         _estimateWidthCache = estimateWidthCache;
         _webUrl = webUrl;
         _fullUserList = fullUserList;

         _diffContextDepth = diffContextDepth;
         _popupDiffContextDepth = new ContextDepth(5, 5);
         if (git != null)
         {
            _panelContextMaker = new EnhancedContextMaker(git);
            _simpleContextMaker = new SimpleContextMaker(git);
            _popupContextMaker = _panelContextMaker;
         }
         ColorScheme.Modified += onColorSchemeModified;

         _diffContextPosition = diffContextPosition;
         _discussionColumnWidth = discussionColumnWidth;
         _needShiftReplies = needShiftReplies;

         _onContentChanging = () =>
         {
            onContentChanging?.Invoke(this);
         };
         _onContentChanged = () =>
         {
            _previousWidth = null;
            onContentChanged?.Invoke(this);
         };
         _onControlGotFocus = onControlGotFocus;
         _onSetFocusToNoteProgramatically = onSetFocusToNoteProgramatically;
         _undoFocusChangedOnClick = undoFocusChangedOnClick;
         _selectNoteUrl = selectNoteByUrl;
         _selectNoteByPosition = selectNoteByPosition;

         _htmlTooltip = htmlTooltip;
         _popupWindow = popupWindow;
         _popupWindow.Closed += onPopupWindowClosed;

         _specialDiscussionNoteMarkdownPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
      }

      internal void Initialize(Control parent)
      {
         onCreate(parent);
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         disposePopupContext();
         if (_popupWindow != null)
         {
            _popupWindow.Closed -= onPopupWindowClosed;
         }
         _popupWindow = null;

         ColorScheme.Modified -= onColorSchemeModified;

         foreach (NoteContainer noteContainer in getNoteContainers())
         {
            noteContainer.NoteContent.ContextMenu?.Dispose();
            noteContainer.NoteAvatar.ContextMenu?.Dispose();
         }
      }

      protected override void OnDpiChangedBeforeParent(EventArgs e)
      {
         _previousWidth = null;
         base.OnDpiChangedBeforeParent(e);
      }

      protected override void OnLocationChanged(EventArgs e)
      {
         base.OnLocationChanged(e);
         if (_popupContext != null) // change location of a popup window if only it is visible now
         {
            showPopupWindow();
         }
      }

      internal bool HasNotes => getNoteContainers().Any();

      internal Discussion Discussion { get; private set; }

      internal bool HasNote(int noteId)
      {
         return getNoteContents().Any(noteControl => getNoteFromControl(noteControl).Id == noteId);
      }

      internal bool HasTextControl(ITextControl textControl)
      {
         if (_textboxFilename == textControl)
         {
            return true;
         }

         IEnumerable<NoteContainer> noteContainers = getNoteContainers();
         foreach (NoteContainer noteContainer in noteContainers)
         {
            if (noteContainer.NoteContent == textControl)
            {
               return true;
            }
         }
         return false;
      }

      internal bool SelectNote(int noteId, int? prevNoteId)
      {
         foreach (Control noteControl in getNoteContents())
         {
            if (getNoteFromControl(noteControl).Id != noteId)
            {
               continue;
            }

            _onSetFocusToNoteProgramatically(noteControl);
            if (!prevNoteId.HasValue || prevNoteId.Value == noteId)
            {
               return true;
            }

            NoteContainer noteContainer = getNoteContainers()
               .FirstOrDefault(container => container.NoteContent == noteControl);
            if (noteContainer == null)
            {
               return true;
            }

            LinkLabel noteBackLink = noteContainer.NoteBack as LinkLabel;
            noteBackLink.Visible = true;
            noteBackLink.LinkClicked += (s, e) =>
            {
               string prevNoteUrl = StringUtils.GetNoteUrl(_webUrl, prevNoteId.Value);
               _selectNoteUrl(prevNoteUrl);
               noteBackLink.Visible = false;
            };
            return true;
         }

         return false;
      }

      internal bool SelectTopVisibleNote()
      {
         foreach (Control noteControl in getNoteContents())
         {
            if (noteControl.Parent.Location.Y + noteControl.Location.Y > 0)
            {
               _onSetFocusToNoteProgramatically(noteControl);
               return true;
            }
         }
         return false;
      }

      internal bool SelectBottomVisibleNote(int screenHeight)
      {
         foreach (Control noteControl in getNoteContents().Reverse())
         {
            if (noteControl.Parent.Location.Y + noteControl.Location.Y + noteControl.Height < screenHeight)
            {
               _onSetFocusToNoteProgramatically(noteControl);
               return true;
            }
         }
         return false;
      }

      internal void SelectNote(ENoteSelectionRequest eNoteSelectionRequest)
      {
         switch (eNoteSelectionRequest)
         {
            case ENoteSelectionRequest.Prev:
               {
                  NoteContainer current = getNoteContainers()
                     .FirstOrDefault(noteContainer => noteContainer.NoteContent.Focused);
                  if (current.Prev != null)
                  {
                     _onSetFocusToNoteProgramatically(current.Prev.NoteContent);
                  }
                  else
                  {
                     _selectNoteByPosition.Invoke(ENoteSelectionRequest.Prev, this);
                  }
               }
               break;

            case ENoteSelectionRequest.Next:
               {
                  NoteContainer current = getNoteContainers()
                     .FirstOrDefault(noteContainer => noteContainer.NoteContent.Focused);
                  if (current.Next != null)
                  {
                     _onSetFocusToNoteProgramatically(current.Next.NoteContent);
                  }
                  else
                  {
                     _selectNoteByPosition.Invoke(ENoteSelectionRequest.Next, this);
                  }
               }
               break;

            case ENoteSelectionRequest.First:
               _onSetFocusToNoteProgramatically(getNoteContents().First());
               break;

            case ENoteSelectionRequest.Last:
               _onSetFocusToNoteProgramatically(getNoteContents().Last());
               break;
         }
      }

      internal void RefreshTimeStamps()
      {
         getNoteContainers()
            .Select(noteContainer => noteContainer?.NoteInfo)
            .ToList()
            .ForEach(noteInfo => noteInfo?.Refresh());
      }

      async private void onMenuItemReply(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyToDiscussionAsync(false);
         }
      }

      async private void onMenuItemReplyTo(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         DiscussionNote note = getNoteFromControl(control);
         if (control?.Parent?.Parent != null && note != null && note.Author != null)
         {
            string initialText = Common.Constants.Constants.GitLabLabelPrefix + note.Author.Username + " ";
            await onReplyToDiscussionAsync(false, initialText);
         }
      }

      async private void onMenuItemReplyAndResolve(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyToDiscussionAsync(true);
         }
      }

      async private void onMenuItemReplyDone(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyAsync("Done", false);
         }
      }

      async private void onMenuItemReplyDoneAndResolve(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyAsync("Done", true);
         }
      }

      async private void onMenuItemEditNote(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent == null)
         {
            return;
         }

         await onEditDiscussionNoteAsync(control);
      }

      private void onMenuItemViewNote(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent == null)
         {
            return;
         }

         onViewDiscussionNote(control);
      }

      async private void onMenuItemDeleteNote(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent == null)
         {
            return;
         }

         if (MessageBox.Show("This discussion note will be deleted. Are you sure?", "Confirm deletion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
         {
            return;
         }

         await onDeleteNoteAsync(getNoteFromControl(control));
      }

      async private void onMenuItemToggleResolveNote(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         HtmlPanel htmlPanel = (HtmlPanel)(menuItem.Tag);
         if (htmlPanel?.Parent?.Parent == null)
         {
            return;
         }

         DiscussionNote note = getNoteFromControl(htmlPanel);
         Debug.Assert(note == null || note.Resolvable);

         await onToggleResolveNoteAsync(note);
      }

      async private void onMenuItemToggleResolveDiscussion(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent == null)
         {
            return;
         }

         await onToggleResolveDiscussionAsync();
      }

      private void noteControl_LinkClicked(object sender, TheArtOfDev.HtmlRenderer.Core.Entities.HtmlLinkClickedEventArgs e)
      {
         string url = e.Link;
         if (UrlParser.IsValidNoteUrl(url))
         {
            _selectNoteUrl.Invoke(url);
            e.Handled = true;
            return;
         }
         e.Handled = false;
      }

      private void noteControl_KeyDown(KeyEventArgs e)
      {
         switch (e.KeyCode)
         {
            case Keys.Up:
               SelectNote(ENoteSelectionRequest.Prev);
               break;

            case Keys.Down:
               SelectNote(ENoteSelectionRequest.Next);
               break;
         }
      }

      private void control_GotFocus(object sender, EventArgs e)
      {
         _onControlGotFocus?.Invoke(sender as Control);
      }

      private void onShowMoreContextClick(object sender, EventArgs e)
      {
         DiscussionNote note = _panelContext == null ? null : getNoteFromControl(_panelContext);
         if (note == null)
         {
            return;
         }

         void setPopupWindowText(string text)
         {
            _popupContext.Text = text;
            resizeLimitedWidthHtmlPanel(_popupContext, _panelContext.Width, DiffContextExtraHeight);
         }

         string formatPopupDiffContextText(Control control, DiffContext? ctx, int minWidth)
         {
            double fontSizePt = WinFormsHelpers.GetFontSizeInPoints(control);
            if (ctx.HasValue && ctx.Value.IsValid())
            {
               string longestLine = ctx.Value.GetLongestLine();
               string htmlSnippet = longestLine != null ?
                  DiffContextFormatter.GetHtml(longestLine, fontSizePt, null, getColorProvider()) : null;

               double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(control);
               int tableWidth = EstimateHtmlWidth(htmlSnippet, fontSizePx, minWidth);
               return getFormattedHtml(ctx.Value, fontSizePt, tableWidth);
            }
            return getErrorHtml("Cannot create a diff ctx for popup window");
         }

         int currentOffset = 0;
         DiffPosition position = PositionConverter.Convert(note.Position);
         Debug.Assert(_popupContext == null); // it should have been disposed and reset when popup window closes
         _popupContext = new HtmlPanelEx(_pathCache, false, false)
         {
            TabStop = false,
            Font = Font,
            Tag = note
         };
         _popupContext.MouseWheelEx += (sender2, e2) =>
         {
            int step = e2.Delta > 0 ? -1 : 1;
            int newOffset = currentOffset + step;
            DiffContext? newContext = getContextSafe(_popupContextMaker, position, newOffset, _popupDiffContextDepth);
            string text = formatPopupDiffContextText(_popupContext, newContext, _panelContext.Width);
            if (text != _popupContext.Text)
            {
               setPopupWindowText(text);
               currentOffset = newOffset;
            }
         };

         DiffContext? context = getContextSafe(_popupContextMaker, position, currentOffset, _popupDiffContextDepth);
         setPopupWindowText(formatPopupDiffContextText(_popupContext, context, _panelContext.Width));

         _popupWindow.SetContent(_popupContext, PopupContextPadding);
         _undoFocusChangedOnClick();
         showPopupWindow();
      }

      private void onCopyToClipboardClick(object sender, EventArgs e)
      {
         DiscussionNote note = _panelContext == null ? null : getNoteFromControl(_panelContext);
         if (note == null || note.Position == null)
         {
            return;
         }

         string contextAsText = getContextAsText(note);
         string noteAsText = String.Format("{0}\r\n---\r\n{1}\r\n---\r\n{2}\r\n{3}",
            getFileName(note.Position), contextAsText, note.Author.Name, note.Body);
         Clipboard.SetText(noteAsText);

         _undoFocusChangedOnClick();
         disableCopyToClipboard();
         scheduleOneShotTimer(enableCopyToClipboard);
      }

      private void onCopyNoteLinkToClipboardClick(Control noteLink)
      {
         if (!(noteLink.Tag is DiscussionNote note))
         {
            return;
         }

         string noteUrl = StringUtils.GetNoteUrl(_webUrl, note.Id);
         Clipboard.SetText(noteUrl);

         _undoFocusChangedOnClick();
         noteLink.Enabled = false;
         scheduleOneShotTimer(() => noteLink.Enabled = true);
      }

      private static void scheduleOneShotTimer(Action onTimer)
      {
         int CopyToClipboardTimerInterval = 500; // 0.5 second
         System.Windows.Forms.Timer copyToClipboardTimer = new System.Windows.Forms.Timer
         {
            Interval = CopyToClipboardTimerInterval
         };
         copyToClipboardTimer.Tick += (s, e) =>
         {
            copyToClipboardTimer.Stop();
            copyToClipboardTimer.Dispose();
            onTimer?.Invoke();
         };
         copyToClipboardTimer.Start();
      }

      private string getContextAsText(DiscussionNote note)
      {
         string errorMessage = "Cannot create a diff context";
         DiffPosition position = PositionConverter.Convert(note.Position);
         DiffContext? context;
         try
         {
            context = getContextSafe(_panelContextMaker, position, 0, _diffContextDepth);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is ContextMakingException)
            {
               ExceptionHandlers.Handle("Cannot create a diff context", ex);
               return errorMessage;
            }
            throw;
         }
         if (!context.HasValue)
         {
            Debug.Assert(false);
            return errorMessage;
         }
         StringBuilder stringBuilder = new StringBuilder();
         int iLine = 0;
         foreach (DiffContext.Line line in context.Value.Lines)
         {
            char prefix = ' ';
            if (line.Left.HasValue && !line.Right.HasValue)
            {
               prefix = '-';
            }
            else if (!line.Left.HasValue && line.Right.HasValue)
            {
               prefix = '+';
            }
            string lineWithoutTabs = line.Text.Replace("\t", "    ");
            string lineWithPrefix = String.Format("{0} {1}", prefix, lineWithoutTabs);
            if (iLine == context.Value.Lines.Count() - 1)
            {
               stringBuilder.Append(lineWithPrefix);
            }
            else
            {
               stringBuilder.AppendLine(lineWithPrefix);
            }
            ++iLine;
         }
         return stringBuilder.ToString();
      }

      private void showPopupWindow()
      {
         Point ptScreen = PointToScreen(new Point(_panelContext.Location.X, _panelContext.Location.Y));
         _popupWindow.Show(ptScreen);
         _showMoreContextHint?.Show();
         _showMoreContext?.Hide();
      }

      private void onPopupWindowClosed(object sender, ToolStripDropDownClosedEventArgs e)
      {
         _showMoreContextHint?.Hide();
         _showMoreContext?.Show();
         disposePopupContext();
      }

      private void disposePopupContext()
      {
         _popupContext?.Dispose();
         _popupContext = null;
      }

      private void onCreate(Control parent)
      {
         Debug.Assert(Discussion.Notes.Any());
         if (!initializeNoteContainers(parent, Discussion.Notes))
         {
            return;
         }

         _textboxFilename = createTextboxFilename(parent, Discussion.Notes.First());
         Controls.Add(_textboxFilename);

         _showMoreContext = createShowMoreContext(Discussion.Notes.First());
         Controls.Add(_showMoreContext);

         _showMoreContextHint = createShowMoreContextHint();
         Controls.Add(_showMoreContextHint);

         _copyToClipboard = createCopyToClipboard(Discussion.Notes.First());
         enableCopyToClipboard();
         Controls.Add(_copyToClipboard);

         _panelContext = createDiffContext(Discussion.Notes.First());
         Controls.Add(_panelContext);
      }

      private Control createDiffContext(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         Control diffContextControl = new HtmlPanelEx(_pathCache, false, true)
         {
            TabStop = false,
            Tag = firstNote,
            Parent = this
         };
         diffContextControl.GotFocus += control_GotFocus;

         return diffContextControl;
      }

      private void setDiffContextText(Control diffContextControl, int? preferredWidth = null)
      {
         Debug.Assert(diffContextControl is HtmlPanel);
         DiscussionNote note = getNoteFromControl(diffContextControl);
         Debug.Assert(note.Type == "DiffNote");
         DiffPosition position = PositionConverter.Convert(note.Position);
         DiffContext? context = getContextSafe(_panelContextMaker, position, 0, _diffContextDepth);

         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(diffContextControl);
         double expectedHeight = fontSizePx * (_diffContextDepth.Size + 1) + (_diffContextDepth.Size + 1) * scale(2);
         int actualHeight = diffContextControl.Height;
         int actualWidth = preferredWidth ?? diffContextControl.Width;
         if (actualWidth == 0)
         {
            return;
         }

         string html;
         if (!context.HasValue)
         {
            html = getErrorHtml("Cannot create a diff context for discussion");
         }
         else
         {
            double fontSizePt = WinFormsHelpers.GetFontSizeInPoints(diffContextControl);

            bool recalcTableWidth = actualHeight == 0 || expectedHeight < actualHeight;
            string longestLine = context.Value.GetLongestLine();
            string htmlSnippet = longestLine != null ?
               DiffContextFormatter.GetHtml(longestLine, fontSizePt, null, getColorProvider()) : null;

            int? tableWidthOpt = new int?();
            if (recalcTableWidth)
            {
               EstimateWidthKey key = new EstimateWidthKey()
               {
                  ActualWidth = actualWidth,
                  FontSizePx = fontSizePx,
                  HtmlSnippet = htmlSnippet
               };
               if (!_estimateWidthCache.TryGetValue(key, out int tableWidth))
               {
                  tableWidthOpt = EstimateHtmlWidth(htmlSnippet, fontSizePx, actualWidth);
                  _estimateWidthCache[key] = tableWidthOpt.Value;
               }
               else
               {
                  tableWidthOpt = tableWidth;
               }
            }
            html = getFormattedHtml(context.Value, fontSizePt, tableWidthOpt);
         }

         if (html == diffContextControl.Text && preferredWidth == null)
         {
            return;
         }

         diffContextControl.SuspendLayout();

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         if (diffContextControl.Visible)
         {
            diffContextControl.Width = 0;
            diffContextControl.Height = 0;
         }
         diffContextControl.Text = html;

         diffContextControl.ResumeLayout(!diffContextControl.Visible);

         if (diffContextControl.Visible)
         {
            resizeLimitedWidthHtmlPanel(diffContextControl as HtmlPanel, actualWidth, DiffContextExtraHeight);
         }
      }

      private DiffContext? getContextSafe(IContextMaker contextMaker,
         DiffPosition position, int offset, ContextDepth depth)
      {
         Debug.Assert(contextMaker != null);
         if (position == null)
         {
            return null;
         }

         DiffContext getContext()
         {
            try
            {
               return contextMaker.GetContext(position, depth, offset, UnchangedLinePolicy.TakeFromRight);
            }
            catch (Exception e) // fallback
            {
               if (e is ArgumentException || e is ContextMakingException)
               {
                  return _simpleContextMaker.GetContext(position, depth, offset, UnchangedLinePolicy.TakeFromRight);
               }
               throw;
            }
         }

         string errorMessage = "Cannot create diff context.";
         try
         {
            return getContext();
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(errorMessage, ex);
         }
         catch (ContextMakingException ex)
         {
            ExceptionHandlers.Handle(errorMessage, ex);
         }
         return null;
      }

      private string getFormattedHtml(DiffContext context, double fontSizePt, int? tableWidth)
      {
         string errorMessage = "Cannot render HTML context.";
         try
         {
            return DiffContextFormatter.GetHtml(context, fontSizePt, tableWidth, getColorProvider());
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(errorMessage, ex);
         }
         return getErrorHtml(errorMessage);
      }

      private string getErrorHtml(string errorMessage)
      {
         return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
      }

      private Control createTextboxFilename(Control parent, DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         TextBox textBox = new SearchableTextBox(parent as IHighlightListener)
         {
            ReadOnly = true,
            Text = getFileName(firstNote.Position),
            Multiline = true,
            WordWrap = false,
            BorderStyle = BorderStyle.None,
            TabStop = false
         };
         // We exclude this control from processing by ThemeSupportHelper to give it
         // a specific BackColor. But it makes us to set ForeColor manually because
         // ThemeSupportHelper won't do that for us.
         // Note that BackColor is set outside of constructor because when noteControl is created
         // after form is loaded, we don't have a chance to exclude it from processing before
         // its BackColor is overwritten by ThemeSupportHelper in OnControlAdded handler.
         textBox.BackColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.Control;
         textBox.ForeColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;
         ThemeSupport.ThemeSupportHelper.ExcludeFromProcessing(textBox);
         textBox.GotFocus += control_GotFocus;
         return textBox;
      }

      private string getFileName(Position position)
      {
         string oldPath = position.Old_Path + " (line " + position.Old_Line + ")";
         string newPath = position.New_Path + " (line " + position.New_Line + ")";
         if (position.Old_Line == null)
         {
            return newPath;
         }
         else if (position.New_Line == null)
         {
            return oldPath;
         }
         else if (position.Old_Path == position.New_Path)
         {
            return newPath;
         }
         return newPath + "\r\n(was " + oldPath + ")";
      }

      private Control createShowMoreContext(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         LinkLabel linkLabel = new LinkLabel()
         {
            AutoSize = true,
            Text = "Show scrollable context",
            BorderStyle = BorderStyle.None,
            TabStop = false
         };
         linkLabel.Click += onShowMoreContextClick;
         return linkLabel;
      }

      private Control createShowMoreContextHint()
      {
         Label label = new Label()
         {
            AutoSize = true,
            Text = "Scroll with mouse wheel",
            BorderStyle = BorderStyle.None,
            Visible = false
         };
         return label;
      }

      private Control createCopyToClipboard(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         LinkLabel linkLabel = new LinkLabel()
         {
            AutoSize = true,
            Text = "Copy issue",
            BorderStyle = BorderStyle.None,
            TabStop = false
         };
         linkLabel.Click += onCopyToClipboardClick;
         return linkLabel;
      }

      private void enableCopyToClipboard()
      {
         if (_copyToClipboard != null)
         {
            _copyToClipboard.Enabled = true;
         }
      }

      private void disableCopyToClipboard()
      {
         if (_copyToClipboard != null)
         {
            _copyToClipboard.Enabled = false;
         }
      }

      private bool initializeNoteContainers(Control parent, IEnumerable<DiscussionNote> notes)
      {
         Debug.Assert(parent != null);
         _noteContainers = createNoteContainers(parent, notes);
         foreach (NoteContainer noteContainer in _noteContainers)
         {
            Controls.Add(noteContainer.NoteInfo);
            Controls.Add(noteContainer.NoteHint);
            Controls.Add(noteContainer.NoteContent);
            Controls.Add(noteContainer.NoteAvatar);
            Controls.Add(noteContainer.NoteLink);
            Controls.Add(noteContainer.NoteBack);
         }
         return getNoteContainers().Any();
      }

      private void removeNoteContainers()
      {
         for (int iControl = Controls.Count - 1; iControl >= 0; --iControl)
         {
            IEnumerable<NoteContainer> noteContainers = getNoteContainers();
            foreach (NoteContainer container in noteContainers)
            {
               Control control = Controls[iControl];
               if (container.NoteContent == control
                || container.NoteInfo == control
                || container.NoteHint == control
                || container.NoteAvatar == control
                || container.NoteLink == control
                || container.NoteBack == control)
               {
                  control.Dispose();
                  Controls.Remove(control);
                  break;
               }
            }
         }
         _noteContainers = null;
      }

      private IEnumerable<NoteContainer> getNoteContainers()
      {
         return _noteContainers ?? Array.Empty<NoteContainer>();
      }

      private IEnumerable<Control> getNoteContents()
      {
         return getNoteContainers()
            .Select(container => container.NoteContent)
            .Where(noteControl => noteControl != null);
      }

      private IEnumerable<Control> getNoteAvatars()
      {
         return getNoteContainers()
            .Select(container => container.NoteAvatar)
            .Where(noteAvatar => noteAvatar != null);
      }

      private IEnumerable<NoteContainer> createNoteContainers(Control parent, IEnumerable<DiscussionNote> allNotes)
      {
         if (parent == null)
         {
            return null;
         }

         bool discussionResolved = allNotes
            .Cast<DiscussionNote>()
            .All(note => !note.Resolvable || note.Resolved);

         List<NoteContainer> containers = new List<NoteContainer>();
         IEnumerable<DiscussionNote> notes = allNotes.Where(item => shouldCreateNoteContainer(item));

         NoteContainer prev = null;
         foreach (DiscussionNote note in notes)
         {
            NoteContainer noteContainer = createNoteContainer(parent, note, discussionResolved);
            if (prev != null)
            {
               noteContainer.Prev = prev;
               noteContainer.Prev.Next = noteContainer;
            }
            prev = noteContainer;
            containers.Add(noteContainer);
         }
         return containers;
      }

      private bool isIndividualSystem()
      {
         if (Discussion.Notes == null || !Discussion.Notes.Any())
         {
            return false;
         }
         return Discussion.Individual_Note && isSystemNote(Discussion.Notes.First());
      }

      private bool isSystemNote(DiscussionNote note)
      {
         return note != null && note.System;
      }

      private bool canBeModified(DiscussionNote note)
      {
         if (note == null)
         {
            return false;
         }
         return note.Author.Id == _currentUser.Id && !isSystemNote(note);
      }

      private bool canAddNotes()
      {
         return !isIndividualSystem();
      }

      private bool shouldCreateNoteContainer(DiscussionNote note)
      {
         if (note == null)
         {
            return false;
         }

         if (note.System)
         {
            return note.Body.StartsWith("approved this merge request")
                || note.Body.StartsWith("unapproved this merge request");
         }

         return true;
      }

      private NoteContainer createNoteContainer(Control parent, DiscussionNote note, bool discussionResolved)
      {
         NoteContainer noteContainer = new NoteContainer();
         noteContainer.NoteInfo = new Label
         {
            Text = getNoteInformation(note),
            AutoSize = true,
            TabStop = false
         };
         noteContainer.NoteInfo.Invalidated += (_, __) =>
            noteContainer.NoteInfo.Text = getNoteInformation(note);

         string hint = getNoteHintHtml(note);
         if (hint != null)
         {
            noteContainer.NoteHint = new PictureBox
            {
               Image = Properties.Resources.exclamation_32x32,
               SizeMode = PictureBoxSizeMode.StretchImage,
               Parent = this /* to inherit Font and set right font size to CSS of a hint tooltip */
            };
         }

         noteContainer.NoteAvatar = new AvatarBox
         {
            Image = _avatarImageCache.GetAvatar(note.Author),
            Tag = note
         };
         noteContainer.NoteAvatar.ContextMenu = createContextMenuForAvatar(note, noteContainer.NoteAvatar);

         noteContainer.NoteLink = new LinkLabel()
         {
            AutoSize = true,
            Text = "Copy link",
            BorderStyle = BorderStyle.None,
            Tag = note,
            TabStop = false
         };
         noteContainer.NoteLink.Click += (s, e) => onCopyNoteLinkToClipboardClick(noteContainer.NoteLink);

         noteContainer.NoteBack = new LinkLabel()
         {
            AutoSize = true,
            Text = "Go back",
            BorderStyle = BorderStyle.None,
            Tag = note,
            TabStop = false,
            Visible = false
         };

         // HtmlPanel background is defined by BackColor property.
         // For some reason, background-color attribute (for body element) does not affect HtmlPanel background.
         if (!isServiceDiscussionNote(note))
         {
            HtmlPanel noteControl = new SearchableHtmlPanel(parent as IHighlightListener, _pathCache)
            {
               Tag = note,
               Parent = this, /* to inherit Font and set right font size to CSS of a tooltip */
               IsContextMenuEnabled = false
            };
            // Note that BackColor is set outside of constructor because when noteControl is created
            // after form is loaded, we don't have a chance to exclude it from processing before
            // its BackColor is overwritten by ThemeSupportHelper in OnControlAdded handler.
            noteControl.BackColor = getNoteColor(note).Item2;
            noteControl.GotFocus += control_GotFocus;
            noteControl.ContextMenu = createContextMenuForDiscussionNote(note, noteControl, discussionResolved);
            noteControl.FontChanged += (sender, e) =>
            {
               if (noteControl.Parent != null)
               {
                  updateStylesheet(noteControl);
                  updateNoteTooltip(noteControl);
                  updateNoteHintTooltip(noteContainer.NoteHint, hint);
               }
            };
            noteControl.LinkClicked += noteControl_LinkClicked;
            noteControl.KeyDown += (s, e) => noteControl_KeyDown(e);

            updateStylesheet(noteControl);
            updateNoteTooltip(noteControl);
            updateNoteHintTooltip(noteContainer.NoteHint, hint);
            ThemeSupport.ThemeSupportHelper.ExcludeFromProcessing(noteControl);

            noteContainer.NoteContent = noteControl;
         }
         else
         {
            HtmlPanel noteControl = new HtmlPanelEx(_pathCache, true, true)
            {
               Tag = note,
               Parent = this /* to inherit Font and set right font size to CSS of a tooltip */
            };
            // Note that BackColor is set outside of constructor because when noteControl is created
            // after form is loaded, we don't have a chance to exclude it from processing before
            // its BackColor is overwritten by ThemeSupportHelper in OnControlAdded handler.
            noteControl.BackColor = getNoteColor(note).Item2;
            noteControl.GotFocus += control_GotFocus;
            noteControl.FontChanged += (sender, e) =>
            {
               if (noteControl.Parent != null)
               {
                  updateStylesheet(noteControl);
               }
            };
            noteControl.LinkClicked += noteControl_LinkClicked;
            noteControl.KeyDown += (s, e) => noteControl_KeyDown(e);

            updateStylesheet(noteControl);
            ThemeSupport.ThemeSupportHelper.ExcludeFromProcessing(noteControl);

            noteContainer.NoteContent = noteControl;
         }

         return noteContainer;
      }

      bool updateStylesheet(HtmlPanel htmlPanel)
      {
         string createStylesheet()
         {
            DiscussionNote note = getNoteFromControl(htmlPanel);
            Color textColor = note == null ? Color.Black : getNoteColor(note).Item1;
            string css = ResourceHelper.ApplyFontSizeAndColorsToCSS(htmlPanel);
            return String.Format(
               @"{0}
                 body {{
                    color: {1};
                 }}
                 body div {{ 
                    padding-left: {2}px;
                    padding-right: {3}px;
                 }}",
               css,
               HtmlUtils.ColorToRgb(textColor),
               NoteHtmlPaddingLeft,
               NoteHtmlPaddingRight);
         }

         string newStylesheet = createStylesheet();
         if (newStylesheet != htmlPanel.BaseStylesheet)
         {
            htmlPanel.BaseStylesheet = newStylesheet;

            // HtmlPanel shows corrupted content without forcing Text update
            if (isServiceDiscussionNote(getNoteFromControl(htmlPanel)))
            {
               setServiceDiscussionNoteText(htmlPanel, getNoteFromControl(htmlPanel));
            }
            else
            {
               setDiscussionNoteText(htmlPanel);
            }
            return true;
         }
         return false;
      }

      private void updateNoteTooltip(Control noteControl)
      {
         DiscussionNote note = getNoteFromControl(noteControl);
         string html = formatNoteTooltipHtml(noteControl, note);
         setToolTipText(noteControl, html);
      }

      private void updateNoteHintTooltip(Control hintControl, string hint)
      {
         if (hintControl == null || hint == null)
         {
            return;
         }

         string html = formatTooltipHtml(hintControl, hint);
         setToolTipText(hintControl, html);
      }	  

      private void setToolTipText(Control control, string text)
      {
         if (text != _htmlTooltip.GetToolTip(control))
         {
            _htmlTooltip.SetToolTip(control, text);
         }
      }

      internal void setDiscussionNoteText(Control noteControl)
      {
         DiscussionNote note = getNoteFromControl(noteControl);
         setDiscussionNoteText(noteControl, note);
      }

      internal void setDiscussionNoteText(Control noteControl, DiscussionNote note)
      {
         if (note == null)
         {
            // this is possible when noteControl detaches from parent
            return;
         }

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         int prevWidth = noteControl.Width;
         if (noteControl.Visible)
         {
            noteControl.Width = 0;
            noteControl.Height = 0;
         }

         Debug.Assert(noteControl is HtmlPanel);

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath,
            _specialDiscussionNoteMarkdownPipeline, noteControl);
         if (Program.Settings.EmulateNativeLineBreaksInDiscussions)
         {
            body = body
               .TrimEnd('\n')
               .Replace("\n", "<br>")
               .Replace("</li><br>", "</li>")
               .Replace("<br><ul>", "<ul>")
               .Replace("<br><ol>", "<ol>");
         }
         noteControl.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);

         if (noteControl.Visible)
         {
            resizeLimitedWidthHtmlPanel(noteControl as HtmlPanel, prevWidth, NormalNoteExtraHeight);
         }
      }

      private void setServiceDiscussionNoteText(Control noteControl, DiscussionNote note)
      {
         if (note == null)
         {
            Debug.Assert(false);
            return;
         }

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         int prevWidth = noteControl.Width;
         if (noteControl.Visible)
         {
            noteControl.Width = 0;
            noteControl.Height = 0;
         }

         Debug.Assert(noteControl is HtmlPanel);

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath,
            _specialDiscussionNoteMarkdownPipeline, noteControl);
         noteControl.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);

         if (noteControl.Visible)
         {
            resizeFullSizeHtmlPanel(noteControl as HtmlPanel, prevWidth, ServiceNoteExtraWidth, ServiceNoteExtraHeight);
         }
      }

      private void resizeFullSizeHtmlPanel(HtmlPanel htmlPanel, int maxWidth, int extraWidth, int extraHeight)
      {
         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         htmlPanel.Width = 0;
         htmlPanel.Height = 0;

         // Use computed size as the control size. Height must be set BEFORE Width.
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + extraHeight;
         htmlPanel.Width = htmlPanel.AutoScrollMinSize.Width + extraWidth;

         if (htmlPanel.Width > maxWidth)
         {
            resizeLimitedWidthHtmlPanel(htmlPanel, maxWidth, extraHeight);
         }
      }

      private void resizeLimitedWidthHtmlPanel(HtmlPanel htmlPanel, int width, int extraHeight)
      {
         // Turn on AutoScroll to obtain relevant HorizontalScroll visibility property values after width change
         htmlPanel.AutoScroll = true;

         // Change width to a specific value
         htmlPanel.Width = width;

         // Check if horizontal scroll bar is needed if we have the specified width
         int horzScrollBarHeight = htmlPanel.HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0;

         // Turn off AutoScroll to avoid recalculating of AutoScrollMinSize on Height change.
         // htmlPanel must think that no scroll bars are needed to return full actual size.
         htmlPanel.AutoScroll = false;

         // Change height to the full actual size, leave a space for a horizontal scroll bar if needed
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + horzScrollBarHeight + extraHeight;

         // To enable scroll bars, AutoScroll property must be on
         htmlPanel.AutoScroll = horzScrollBarHeight > 0;
      }

      private bool isServiceDiscussionNote(DiscussionNote note)
      {
         if (note == null)
         {
            Debug.Assert(false);
            return false;
         }

         return note.Author.Username == Program.ServiceManager.GetServiceMessageUsername();
      }

      private MenuItem createMenuItem(object tag, string text, bool isEnabled, EventHandler onClick,
         Shortcut shortcut = Shortcut.None)
      {
         MenuItem menuItem = new MenuItem
         {
            Tag = tag,
            Text = text,
            Enabled = isEnabled,
            Shortcut = shortcut
         };
         menuItem.Click += onClick;
         return menuItem;
      }

      private ContextMenu createContextMenuForDiscussionNote(DiscussionNote note, Control noteControl,
         bool discussionResolved)
      {
         ContextMenu contextMenu = new ContextMenu();

         void addMenuItem(string text, bool isEnabled, EventHandler onClick, Shortcut shortcut = Shortcut.None) =>
            contextMenu.MenuItems.Add(createMenuItem(noteControl, text, isEnabled, onClick, shortcut));
         void addSeparator() => addMenuItem("-", true, null);

         string resolveThreadText = (discussionResolved ? "Unresolve" : "Resolve") + " Thread";
         addMenuItem(resolveThreadText, isDiscussionResolvable(), onMenuItemToggleResolveDiscussion);

         string resolveItemText = (note.Resolvable && note.Resolved ? "Unresolve" : "Resolve") + " Note";
         addMenuItem(resolveItemText, note.Resolvable, onMenuItemToggleResolveNote);

         addSeparator();

         addMenuItem("Delete note", canBeModified(note), onMenuItemDeleteNote);
         addMenuItem("Edit note", canBeModified(note), onMenuItemEditNote, Shortcut.F2);
         addMenuItem("Reply", canAddNotes(), onMenuItemReply);

         string replyText = "Reply and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread";
         addMenuItem(replyText, canAddNotes(), onMenuItemReplyAndResolve, Shortcut.F4);

         string replyDoneText = "Reply \"Done\"";
         addMenuItem(replyDoneText, canAddNotes(), onMenuItemReplyDone);

         string replyDoneAndResolveText = "Reply \"Done\" and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread";
         addMenuItem(replyDoneAndResolveText, canAddNotes() && isDiscussionResolvable(), onMenuItemReplyDoneAndResolve, Shortcut.ShiftF4);

         addSeparator();

         addMenuItem("View Note as plain text", true, onMenuItemViewNote, Shortcut.F6);

         return contextMenu;
      }

      private ContextMenu createContextMenuForAvatar(DiscussionNote note, Control avatarControl)
      {
         ContextMenu contextMenu = new ContextMenu();

         void addMenuItem(string text, bool isEnabled, EventHandler onClick, Shortcut shortcut = Shortcut.None) =>
            contextMenu.MenuItems.Add(createMenuItem(avatarControl, text, isEnabled, onClick, shortcut));
         void addSeparator() => addMenuItem("-", true, null);

         addMenuItem("Open profile", true, (s, e) => UrlHelper.OpenBrowser(note.Author.Web_Url));

         addSeparator();

         addMenuItem("Reply to", canAddNotes(), onMenuItemReplyTo);

         return contextMenu;
      }

      private string getNoteInformation(DiscussionNote note)
      {
         if (note == null)
         {
            return String.Empty;
         }

         return String.Format("{0} -- {1}",
            note.Author.Name, TimeUtils.DateTimeToStringAgo(note.Created_At));
      }

      private string formatNoteTooltipHtml(Control noteControl, DiscussionNote note)
      {
         if (note == null)
         {
            return String.Empty;
         }

         StringBuilder body = new StringBuilder();
         if (note.Resolvable)
         {
            string text = note.Resolved ? "Resolved." : "Not resolved.";
            string color = note.Resolved ? "seagreen" : "red";
            body.AppendFormat("<i style=\"color: {0}\">{1}&nbsp;&nbsp;&nbsp;</i>", color, text);
         }
         body.AppendFormat("Created by <b> {0} </b> at <span style=\"color: darkcyan\">{1}</span>",
            note.Author.Name, TimeUtils.DateTimeToString(note.Created_At));
         body.AppendFormat("<br><br>Use context menu to view note as <b>plain text</b>.");
         return formatTooltipHtml(noteControl, body.ToString());
      }

      private string getNoteHintHtml(DiscussionNote note)
      {
         if (note == null || String.IsNullOrEmpty(note.Body) || isServiceDiscussionNote(note))
         {
            return null;
         }

         if (StringUtils.DoesContainUnescapedSpecialCharacters(note.Body))
         {
            return Common.Constants.Constants.UnescapedMarkdownHtmlHint;
         }

         return null;
      }

      private static string formatTooltipHtml(Control noteControl, string text)
      {
         if (text == null)
         {
            return String.Empty;
         }

         // background-color attribute is needed for tooltips, but do not have effect on HtmlPanel
         string css = ResourceHelper.ApplyFontSizeAndColorsToCSS(noteControl);
         Color tooltipBackgroundColor = ThemeSupport.StockColors.GetThemeColors().TooltipBackground;
         css += String.Format(@"
         body {{
            background-color: {0};
         }}", HtmlUtils.ColorToRgb(tooltipBackgroundColor));
         return String.Format(
            @"<html>
               <head>
                  <style>
                     {0}
                  </style>
               </head>
               <body>
                  {1}
               </body>
             </html>",
            css, text);
      }

      private Tuple<Color, Color> getNoteColor(DiscussionNote note)
      {
         Tuple<Color, Color> getColorOrDefault(string colorName)
         {
            ColorSchemeItem item = ColorScheme.GetColor(colorName);
            Color textColor = item.TextName == null
               ? ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive
               : ColorScheme.GetColor(item.TextName).Color;
            return new Tuple<Color, Color>(textColor, item.Color);
         }

         if (isServiceDiscussionNote(note))
         {
            return getColorOrDefault("Discussions_ServiceMessages");
         }

         if (note.Resolvable)
         {
            if (note.Author.Id == _mergeRequestAuthor.Id)
            {
               return note.Resolved
                  ? getColorOrDefault("Discussions_Author_Notes_Resolved")
                  : getColorOrDefault("Discussions_Author_Notes_Unresolved");
            }
            else
            {
               return note.Resolved
                  ? getColorOrDefault("Discussions_NonAuthor_Notes_Resolved")
                  : getColorOrDefault("Discussions_NonAuthor_Notes_Unresolved");
            }
         }
         else
         {
            return getColorOrDefault("Discussions_Comments");
         }
      }

      protected override void OnFontChanged(EventArgs e)
      {
         _previousWidth = null;
         base.OnFontChanged(e);
      }

      internal void AdjustToWidth(int width)
      {
         if (_previousWidth.HasValue && _previousWidth == width || width < 0)
         {
            return;
         }

         resizeBoxContent(width);
         repositionBoxContent(width);
         resizeBox(width);
         makeRoundBorders();
         _previousWidth = width;
      }

      internal void SetDiffContextPosition(ConfigurationHelper.DiffContextPosition position)
      {
         _diffContextPosition = position;
         _previousWidth = null;
      }

      internal void SetDiscussionColumnWidth(ConfigurationHelper.DiscussionColumnWidth width)
      {
         _discussionColumnWidth = width;
         _previousWidth = null;
      }

      internal void SetNeedShiftReplies(bool value)
      {
         _needShiftReplies = value;
         _previousWidth = null;
      }

      internal void SetDiffContextDepth(ContextDepth contextDepth)
      {
         _diffContextDepth = contextDepth;
         _previousWidth = null;
         if (_panelContext != null)
         {
            setDiffContextText(_panelContext);
         }
      }

      private void onColorSchemeModified()
      {
         getNoteContainers()?
            .ToList()
            .ForEach(noteContainer =>
            {
               DiscussionNote note = getNoteFromControl(noteContainer.NoteContent);
               if (note != null)
               {
                  noteContainer.NoteContent.BackColor = getNoteColor(note).Item2;
                  updateStylesheet(noteContainer.NoteContent as HtmlPanel);
                  if (!isServiceDiscussionNote(note))
                  {
                     updateNoteTooltip(noteContainer.NoteContent);
                     updateNoteHintTooltip(noteContainer.NoteHint, getNoteHintHtml(note));
                  }
               }

               if (_textboxFilename != null)
               {
                  _textboxFilename.BackColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.Control;
                  _textboxFilename.ForeColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;
               }

               (noteContainer.NoteAvatar as AvatarBox).Image = _avatarImageCache.GetAvatar(note.Author);
            });

         if (_panelContext != null)
         {
            setDiffContextText(_panelContext);
         }
      }

      private int getColumnInterval(int width)
      {
         return width * ColumnInterval / 100;
      }

      private int getNoteWidth(int width)
      {
         bool isColumnWidthFixed = Program.Settings.IsDiscussionColumnWidthFixed;

         int getNoteWidthInUnits()
         {
            if (_diffContextPosition == ConfigurationHelper.DiffContextPosition.Top)
            {
               return isColumnWidthFixed
                  ? NoteWidth_Symbols_OneColumn[_discussionColumnWidth]
                  : NoteWidth_Percents_OneColumn[_discussionColumnWidth];
            }
            else
            {
               return isColumnWidthFixed
                  ? NoteWidth_Symbols_TwoColumns[_discussionColumnWidth]
                  : NoteWidth_Percents_TwoColumns[_discussionColumnWidth];
            }
         }

         int getMaxFixedWidth()
         {
            if (_diffContextPosition == ConfigurationHelper.DiffContextPosition.Top)
            {
               return width * NoteWidth_Percents_OneColumn[ConfigurationHelper.DiscussionColumnWidth.Wide] / 100;
            }
            return width * NoteWidth_Percents_TwoColumns[ConfigurationHelper.DiscussionColumnWidth.Wide] / 100;
         }

         int noteWidthInUnits = getNoteWidthInUnits();
         int noteWidth = isColumnWidthFixed
            ? Math.Min(noteWidthInUnits * Convert.ToInt32(Font.Size), getMaxFixedWidth())
            : width * noteWidthInUnits / 100;
         return noteWidth;
      }

      private int getDiffContextWidth(int width)
      {
         return getNoteWidth(width);
      }

      private int getNoteRepliesPadding(int width)
      {
         return _needShiftReplies ? width * RepliesPadding / 100 : 0;
      }

      private void resizeBoxContent(int width)
      {
         IEnumerable<NoteContainer> noteContainers = getNoteContainers();
         foreach (NoteContainer noteContainer in noteContainers)
         {
            bool needShrinkNote = noteContainer != noteContainers.First();
            int noteWidthDelta = needShrinkNote ? getNoteRepliesPadding(width) : 0;

            int noteAvatarHeight = (int)(noteContainer.NoteInfo.Height * 2);
            int noteAvatarWidth = noteAvatarHeight;
            noteContainer.NoteAvatar.Size = new Size(noteAvatarWidth, noteAvatarHeight);

            if (noteContainer.NoteHint != null)
            {
               int noteHintHeight = noteContainer.NoteInfo.Height;
               int noteHintWidth = noteHintHeight;
               noteContainer.NoteHint.Size = new Size(noteHintWidth, noteHintHeight);
            }

            Control noteControl = noteContainer.NoteContent;
            HtmlPanel htmlPanel = noteControl as HtmlPanel;
            DiscussionNote note = getNoteFromControl(noteControl);
            if (note != null && !isServiceDiscussionNote(note))
            {
               int limitedWidth = getNoteWidth(width) - noteWidthDelta - AvatarPaddingRight - noteAvatarWidth;
               resizeLimitedWidthHtmlPanel(htmlPanel, limitedWidth, NormalNoteExtraHeight);
            }
            else
            {
               int limitedWidth = getNoteWidth(width) - noteWidthDelta - AvatarPaddingRight - noteAvatarWidth;
               resizeFullSizeHtmlPanel(htmlPanel, limitedWidth, ServiceNoteExtraWidth, ServiceNoteExtraHeight);
            }
         }

         if (_textboxFilename != null)
         {
            _textboxFilename.Width = getDiffContextWidth(width)
               - (_showMoreContext == null ? 0 : _showMoreContext.Width)
               - (_copyToClipboard == null ? 0 : _copyToClipboard.Width)
               - (_copyToClipboard == null && _showMoreContext == null ? 0 : TextBoxFileNamePaddingRight);
            _textboxFilename.Height = (_textboxFilename as TextBoxEx).FullPreferredHeight;
         }

         if (_panelContext != null)
         {
            setDiffContextText(_panelContext, getDiffContextWidth(width));
         }
      }

      private void repositionBoxContent(int width)
      {
         if (_diffContextPosition != ConfigurationHelper.DiffContextPosition.Top)
         {
            repositionBoxContentInTwoColumns(width);
         }
         else
         {
            repositionBoxContentInOneColumn(width);
         }
      }

      private void repositionBoxContentInTwoColumns(int width)
      {
         Debug.Assert(_diffContextPosition == ConfigurationHelper.DiffContextPosition.Right
                   || _diffContextPosition == ConfigurationHelper.DiffContextPosition.Left);

         // column A
         {
            Point contextPos = new Point(0, 0);
            bool needOffsetContext = _diffContextPosition == ConfigurationHelper.DiffContextPosition.Right;
            int contextPosLeftOffset = needOffsetContext ? getNoteWidth(width) + getColumnInterval(width) : 0;
            contextPos.Offset(contextPosLeftOffset, 0);

            if (_textboxFilename != null)
            {
               _textboxFilename.Location = contextPos;
               contextPos.Offset(0, _textboxFilename.Height + TextBoxFileNamePaddingBottom);
            }

            if (_panelContext != null)
            {
               _panelContext.Location = contextPos;

               if (_showMoreContext != null)
               {
                  _showMoreContext.Location = new Point(
                     _panelContext.Location.X + _panelContext.Width - _showMoreContext.Width, 0);

                  if (_copyToClipboard != null)
                  {
                     _showMoreContext.Location = new Point(
                        _showMoreContext.Location.X - _copyToClipboard.Width - CopyLinkPaddingLeft, _showMoreContext.Location.Y);
                  }
               }

               if (_showMoreContextHint != null && _showMoreContext != null)
               {
                  _showMoreContextHint.Location = _showMoreContext.Location;
               }

               if (_copyToClipboard != null)
               {
                  _copyToClipboard.Location = new Point(
                     _panelContext.Location.X + _panelContext.Width - _copyToClipboard.Width, 0);
               }
            }
         }

         // column B
         {
            Point notePos = new Point(0, 0);
            bool needOffsetNotes = _diffContextPosition == ConfigurationHelper.DiffContextPosition.Left;
            int notePosLeftOffset = needOffsetNotes ? getDiffContextWidth(width) + getColumnInterval(width) : 0;
            notePos.Offset(notePosLeftOffset, 0);

            repositionNotes(width, notePos);
         }
      }

      private void repositionBoxContentInOneColumn(int width)
      {
         Debug.Assert(_diffContextPosition == ConfigurationHelper.DiffContextPosition.Top);

         Point controlPos = new Point(0, 0);
         if (_textboxFilename != null)
         {
            _textboxFilename.Location = controlPos;
            controlPos.Offset(0, _textboxFilename.Height + TextBoxFileNamePaddingBottom);
         }

         if (_panelContext != null)
         {
            _panelContext.Location = controlPos;
            controlPos.Offset(0, _panelContext.Height + DiffContextPaddingBottom);

            if (_showMoreContext != null)
            {
               _showMoreContext.Location = new Point(
                  _panelContext.Location.X + _panelContext.Width - _showMoreContext.Width, 0);

               if (_copyToClipboard != null)
               {
                  _showMoreContext.Location = new Point(
                     _showMoreContext.Location.X - _copyToClipboard.Width - CopyLinkPaddingLeft, _showMoreContext.Location.Y);
               }
            }

            if (_showMoreContextHint != null && _showMoreContext != null)
            {
               _showMoreContextHint.Location = _showMoreContext.Location;
            }

            if (_copyToClipboard != null)
            {
               _copyToClipboard.Location = new Point(
                  _panelContext.Location.X + _panelContext.Width - _copyToClipboard.Width, 0);
            }
         }

         repositionNotes(width, controlPos);
      }

      private void repositionNotes(int width, Point controlPos)
      {
         IEnumerable<NoteContainer> noteContainers = getNoteContainers();
         foreach (NoteContainer noteContainer in noteContainers)
         {
            bool needOffsetNote = noteContainer != noteContainers.First();
            int noteHorzOffset = needOffsetNote ? getNoteRepliesPadding(width) : 0;

            {
               Point noteAvatarPos = controlPos;
               noteAvatarPos.Offset(noteHorzOffset, AvatarPaddingTop);
               noteContainer.NoteAvatar.Location = noteAvatarPos;
            }

            {
               Point noteInfoPos = controlPos;
               noteInfoPos.Offset(noteHorzOffset + AvatarPaddingRight + noteContainer.NoteAvatar.Width, 0);
               noteContainer.NoteInfo.Location = noteInfoPos;
            }

            if (noteContainer.NoteHint != null)
            {
               Point noteHintPos = controlPos;
               noteHintPos.Offset(noteContainer.NoteInfo.Right + NoteHintPaddingRight, 0);
               noteContainer.NoteHint.Location = noteHintPos;
            }

            {
               Point noteBackPos = controlPos;
               noteBackPos.X += getNoteWidth(width) - noteContainer.NoteBack.Width - BackLinkPaddingRight - noteContainer.NoteLink.Width;
               noteContainer.NoteBack.Location = noteBackPos;
            }

            {
               Point noteLinkPos = controlPos;
               noteLinkPos.X += getNoteWidth(width) - noteContainer.NoteLink.Width;
               noteContainer.NoteLink.Location = noteLinkPos;
            }
            controlPos.Offset(0, noteContainer.NoteInfo.Height + scale(2));

            {
               Point noteContentPos = controlPos;
               noteContentPos.Offset(noteHorzOffset + AvatarPaddingRight + noteContainer.NoteAvatar.Width, 0);
               noteContainer.NoteContent.Location = noteContentPos;
            }
            controlPos.Offset(0, noteContainer.NoteContent.Height + NotePaddingBottom);
         }
      }

      private void makeRoundBorders()
      {
         List<Control> controls = new List<Control>();
         if (_panelContext != null)
         {
            controls.Add(_panelContext);
         }
         controls.AddRange(getNoteContents());
         controls.ForEach(control =>
         {
            HtmlPanelEx htmlPanelEx = control as HtmlPanelEx;
            Debug.Assert(htmlPanelEx != null);
            htmlPanelEx.UpdateRegion();
         });
      }

      private void resizeBox(int width)
      {
         if (_diffContextPosition != ConfigurationHelper.DiffContextPosition.Top)
         {
            resizeBoxInTwoColumns(width);
         }
         else
         {
            resizeBoxInOneColumn(width);
         }
      }

      private void resizeBoxInTwoColumns(int width)
      {
         Debug.Assert(_diffContextPosition == ConfigurationHelper.DiffContextPosition.Right
                   || _diffContextPosition == ConfigurationHelper.DiffContextPosition.Left);

         int boxWidth = getNoteWidth(width) + getColumnInterval(width) + getDiffContextWidth(width);

         int boxHeight;
         {
            int notesHeight = 0;
            IEnumerable<NoteContainer> noteContainers = getNoteContainers();
            if (noteContainers.Any())
            {
               notesHeight = noteContainers.Last().NoteContent.Location.Y
                           + noteContainers.Last().NoteContent.Height
                           - noteContainers.First().NoteInfo.Location.Y;
            }

            int ctxHeight = 0;
            if (_panelContext != null && _textboxFilename != null)
            {
               ctxHeight = _panelContext.Location.Y + _panelContext.Height - _textboxFilename.Location.Y;
            }
            else if (_panelContext != null)
            {
               ctxHeight = _panelContext.Height;
            }
            else if (_textboxFilename != null)
            {
               ctxHeight = _textboxFilename.Height;
            }
            boxHeight = Math.Max(notesHeight, ctxHeight);
         }

         Size = new Size(boxWidth, boxHeight);
      }

      private void resizeBoxInOneColumn(int width)
      {
         Debug.Assert(_diffContextPosition == ConfigurationHelper.DiffContextPosition.Top);

         int boxWidth = getNoteWidth(width);
         int boxHeight = 0;
         IEnumerable<NoteContainer> noteContainers = getNoteContainers();
         if (!noteContainers.Any())
         {
            Size = new Size(boxWidth, boxHeight);
            return;
         }

         if (_textboxFilename != null)
         {
            boxHeight = noteContainers.Last().NoteContent.Location.Y
                      + noteContainers.Last().NoteContent.Height
                      - _textboxFilename.Location.Y;
         }
         else
         {
            boxHeight = noteContainers.Last().NoteContent.Location.Y
                      + noteContainers.Last().NoteContent.Height
                      - noteContainers.First().NoteInfo.Location.Y;
         }
         Size = new Size(boxWidth, boxHeight);
      }

      async private Task onEditDiscussionNoteAsync(Control noteControl)
      {
         DiscussionNote note = getNoteFromControl(noteControl);
         if (note == null || !canBeModified(note))
         {
            return;
         }

         string currentBody = StringUtils.ConvertNewlineUnixToWindows(note.Body);
         using (TextEditBaseForm form = new EditNoteForm(currentBody, _imagePath, _fullUserList, _avatarImageCache))
         {
            Point locationAtScreen = noteControl.PointToScreen(new Point(0, 0));
            form.StartPosition = FormStartPosition.Manual;
            form.Location = locationAtScreen;

            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Note text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               string proposedBody = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
               await submitNewBodyAsync(noteControl, proposedBody);
            }
         }
      }

      private void onViewDiscussionNote(Control noteControl)
      {
         DiscussionNote note = getNoteFromControl(noteControl);
         if (note == null)
         {
            return;
         }

         string currentBody = StringUtils.ConvertNewlineUnixToWindows(note.Body);
         using (TextEditBaseForm form = new ViewNoteForm(currentBody, _imagePath))
         {
            Point locationAtScreen = noteControl.PointToScreen(new Point(0, 0));
            form.StartPosition = FormStartPosition.Manual;
            form.Location = locationAtScreen;
            form.ShowDialog();
         }
      }

      async private Task onReplyToDiscussionAsync(
         bool proposeUserToToggleResolveOnReply, string initialText = "")
      {
         if (!canAddNotes())
         {
            return;
         }

         bool isAlreadyResolved = isDiscussionResolved();
         string resolveText = String.Format("{0} Thread", (isAlreadyResolved ? "Unresolve" : "Resolve"));
         using (ReplyOnDiscussionNoteForm form = new ReplyOnDiscussionNoteForm(
            resolveText, initialText, proposeUserToToggleResolveOnReply, _imagePath, _fullUserList, _avatarImageCache))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, this) == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Reply text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               string proposedBody = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
               await onReplyAsync(proposedBody, form.IsResolveActionChecked);
            }
         }
      }

      async private Task onReplyAsync(string body, bool toggleResolve)
      {
         if (!canAddNotes())
         {
            return;
         }

         bool wasResolved = isDiscussionResolved();
         disableAllNoteControls();

         Discussion discussion = null;
         try
         {
            if (isDiscussionResolvable() && toggleResolve)
            {
               await _editor.ReplyAndResolveDiscussionAsync(body, !wasResolved);
            }
            else
            {
               await _editor.ReplyAsync(body);
            }
         }
         catch (DiscussionEditorException ex)
         {
            string message = getErrorMessage("Cannot create a reply to discussion", ex);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      async private Task submitNewBodyAsync(Control noteControl, string newText)
      {
         DiscussionNote oldNote = getNoteFromControl(noteControl);
         if (oldNote == null || newText == oldNote.Body)
         {
            // TextBox.Tag is equal to TextBox.Text ==> text was not changed
            return;
         }

         if (newText.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         disableAllNoteControls();

         Discussion discussion = null;
         try
         {
            await _editor.ModifyNoteBodyAsync(oldNote.Id, newText);
         }
         catch (DiscussionEditorException ex)
         {
            string message = getErrorMessage("Cannot update discussion text", ex);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      async private Task onDeleteNoteAsync(DiscussionNote note)
      {
         if (note == null || !canBeModified(note))
         {
            return;
         }

         disableAllNoteControls();

         Discussion discussion = null;
         try
         {
            await _editor.DeleteNoteAsync(note.Id);
         }
         catch (DiscussionEditorException ex)
         {
            string message = getErrorMessage("Cannot delete a note", ex);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      async private Task onToggleResolveNoteAsync(DiscussionNote note)
      {
         if (note == null)
         {
            return;
         }

         disableAllNoteControls();

         Discussion discussion = null;
         try
         {
            bool wasResolved = note.Resolved;
            await _editor.ResolveNoteAsync(note.Id, !wasResolved);
         }
         catch (DiscussionEditorException ex)
         {
            string message = getErrorMessage("Cannot toggle 'Resolved' state of a note", ex);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      async private Task onToggleResolveDiscussionAsync()
      {
         bool wasResolved = isDiscussionResolved();
         disableAllNoteControls();

         Discussion discussion;
         try
         {
            discussion = await _editor.ResolveDiscussionAsync(!wasResolved);
         }
         catch (DiscussionEditorException ex)
         {
            string message = getErrorMessage("Cannot toggle 'Resolved' state of a discussion", ex);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      private static string getErrorMessage(string defaultMessage, DiscussionEditorException ex)
      {
         return ex.IsNotFoundException()
            ? defaultMessage + " (note/discussion does not exist, use Refresh to update the list)"
            : defaultMessage;
      }

      private static ContextColorProvider getColorProvider()
      {
         return new ContextColorProvider(
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

      private void disableAllNoteControls()
      {
         void disableNoteControl(Control noteControl)
         {
            if (noteControl != null)
            {
               noteControl.BackColor = ColorScheme.GetColor("DiscussionBox_DisabledNote_Background").Color;
               noteControl.ContextMenu?.Dispose();
               noteControl.ContextMenu = new ContextMenu();
               noteControl.Tag = null;
            }
         }

         foreach (Control noteControl in getNoteContents())
         {
            disableNoteControl(noteControl);
         }

         void disableNoteAvatar(Control noteAvatar)
         {
            if (noteAvatar != null)
            {
               noteAvatar.ContextMenu?.Dispose();
               noteAvatar.ContextMenu = new ContextMenu();
               noteAvatar.Tag = null;
            }
         }

         foreach (Control noteAvatar in getNoteAvatars())
         {
            disableNoteAvatar(noteAvatar);
         }
      }

      async private Task refreshDiscussion(Discussion discussion = null)
      {
         if (Parent == null)
         {
            return;
         }

         // Load updated discussion
         try
         {
            Discussion = discussion ?? await _accessor.GetDiscussion();
         }
         catch (DiscussionEditorException ex)
         {
            ExceptionHandlers.Handle("Cannot refresh discussion", ex);
            Discussion = null;
         }

         // Get rid of old text boxes
         // #227:
         // It must be done before `await` because context menu shown for invisible control throws ArgumentException.
         // So if we hide text boxes in _onContentChanging() and process WM_MOUSEUP in `await` below we're in a trouble.
         removeNoteContainers();

         // To suspend layout and hide me
         _onContentChanging?.Invoke();

         if (Parent == null
          || Discussion == null
          || Discussion.Notes.Count() == 0
          || isIndividualSystem()
          || !initializeNoteContainers(Parent, Discussion.Notes)) // Create new text boxes
         {
            // Possible cases:
            // - deleted note was the only discussion item
            // - deleted note was the only visible discussion item but there are System notes like 'a line changed ...'
            Dispose();
            Parent?.Controls.Remove(this);
            _onContentChanged?.Invoke();
            return;
         }

         // To reposition new controls and unhide me back
         _onContentChanged?.Invoke();
         _onSetFocusToNoteProgramatically(getNoteContainers().First().NoteContent);
      }

      private bool isDiscussionResolved()
      {
         bool result = true;
         foreach (Control noteControl in getNoteContents())
         {
            DiscussionNote note = getNoteFromControl(noteControl);
            if (note != null && note.Resolvable && !note.Resolved)
            {
               result = false;
               break;
            }
         }
         return result;
      }

      private bool isDiscussionResolvable()
      {
         return Discussion != null && !Discussion.Individual_Note;
      }

      private DiscussionNote getNoteFromControl(Control control)
      {
         Debug.Assert(control is HtmlPanel || control is AvatarBox);
         return (control == null || control.Tag == null) ? null : (DiscussionNote)(control.Tag);
      }

      // Widths in %
      private readonly int RepliesPadding = 2;
      private readonly int ColumnInterval = 1;
      private readonly Dictionary<ConfigurationHelper.DiscussionColumnWidth, int> NoteWidth_Percents_OneColumn =
         new Dictionary<ConfigurationHelper.DiscussionColumnWidth, int>
      {
         { ConfigurationHelper.DiscussionColumnWidth.Narrow, 45 },
         { ConfigurationHelper.DiscussionColumnWidth.NarrowPlus, 50 },
         { ConfigurationHelper.DiscussionColumnWidth.Medium, 55 },
         { ConfigurationHelper.DiscussionColumnWidth.MediumPlus, 60 },
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   65 },
         { ConfigurationHelper.DiscussionColumnWidth.WidePlus,  70 },
         { ConfigurationHelper.DiscussionColumnWidth.SuperWide,  75 },
         { ConfigurationHelper.DiscussionColumnWidth.SuperWidePlus,  80 }
      };
      private readonly Dictionary<ConfigurationHelper.DiscussionColumnWidth, int> NoteWidth_Percents_TwoColumns =
         new Dictionary<ConfigurationHelper.DiscussionColumnWidth, int>
      {
         { ConfigurationHelper.DiscussionColumnWidth.Narrow, 36 },
         { ConfigurationHelper.DiscussionColumnWidth.NarrowPlus, 39 },
         { ConfigurationHelper.DiscussionColumnWidth.Medium, 42 },
         { ConfigurationHelper.DiscussionColumnWidth.MediumPlus, 45 },
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   48 },
         { ConfigurationHelper.DiscussionColumnWidth.WidePlus,  48 },
         { ConfigurationHelper.DiscussionColumnWidth.SuperWide,  48 },
         { ConfigurationHelper.DiscussionColumnWidth.SuperWidePlus,  48 }
      };
      private readonly Dictionary<ConfigurationHelper.DiscussionColumnWidth, int> NoteWidth_Symbols_OneColumn =
         new Dictionary<ConfigurationHelper.DiscussionColumnWidth, int>
      {
         { ConfigurationHelper.DiscussionColumnWidth.Narrow, 80 },
         { ConfigurationHelper.DiscussionColumnWidth.NarrowPlus, 85 },
         { ConfigurationHelper.DiscussionColumnWidth.Medium, 90 },
         { ConfigurationHelper.DiscussionColumnWidth.MediumPlus, 95 },
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   100 }
      };
      private readonly Dictionary<ConfigurationHelper.DiscussionColumnWidth, int> NoteWidth_Symbols_TwoColumns =
         new Dictionary<ConfigurationHelper.DiscussionColumnWidth, int>
      {
         { ConfigurationHelper.DiscussionColumnWidth.Narrow, 80 },
         { ConfigurationHelper.DiscussionColumnWidth.NarrowPlus, 85 },
         { ConfigurationHelper.DiscussionColumnWidth.Medium, 90 },
         { ConfigurationHelper.DiscussionColumnWidth.MediumPlus, 95 },
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   100 }
      };
      private int? _previousWidth;

      private readonly Padding PopupContextPadding = new Padding(0, 0, 0, 0);

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private int AvatarPaddingTop => scale(5);
      private int AvatarPaddingRight => scale(10);
      private int NoteHintPaddingRight => scale(10);
      private int BackLinkPaddingRight => scale(10);

      private int ServiceNoteExtraWidth => scale(4);
      private int ServiceNoteExtraHeight => scale(4);
      private int NormalNoteExtraHeight => scale(2);
      private int DiffContextExtraHeight => scale(0); // :)
      private int NoteHtmlPaddingLeft => scale(4);
      private int NoteHtmlPaddingRight => scale(20);

      private int TextBoxFileNamePaddingRight => scale(25);
      private int TextBoxFileNamePaddingBottom => scale(5);
      private int NotePaddingBottom => scale(15);
      private int DiffContextPaddingBottom => scale(15);
      private int CopyLinkPaddingLeft => scale(20);

      private Control _textboxFilename;
      private Control _showMoreContextHint;
      private Control _showMoreContext;
      private Control _copyToClipboard;
      private Control _panelContext;

      private class NoteContainer
      {
         public Control NoteInfo;
         public Control NoteHint;
         public Control NoteContent;
         public Control NoteAvatar;
         public Control NoteLink;
         public Control NoteBack;
         public NoteContainer Prev;
         public NoteContainer Next;
      }
      private IEnumerable<NoteContainer> _noteContainers;

      private readonly User _mergeRequestAuthor;
      private readonly User _currentUser;
      private readonly string _imagePath;
      private readonly AvatarImageCache _avatarImageCache;
      private readonly RoundedPathCache _pathCache;
      private readonly EstimateWidthCache _estimateWidthCache;
      private readonly string _webUrl;
      private readonly IEnumerable<User> _fullUserList;
      private ContextDepth _diffContextDepth;
      private readonly ContextDepth _popupDiffContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _popupContextMaker;
      private readonly IContextMaker _simpleContextMaker;
      private readonly GitLabClient.SingleDiscussionAccessor _accessor;
      private readonly IDiscussionEditor _editor;

      private ConfigurationHelper.DiffContextPosition _diffContextPosition;
      private ConfigurationHelper.DiscussionColumnWidth _discussionColumnWidth;
      private bool _needShiftReplies;
      private PopupWindow _popupWindow; // shared between other Discussion Boxes
      private HtmlPanelEx _popupContext; // specific for this instance

      private readonly Action _onContentChanging;
      private readonly Action _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;
      private readonly Action _undoFocusChangedOnClick;
      private readonly Action<string> _selectNoteUrl;
      private readonly Action<ENoteSelectionRequest, DiscussionBox> _selectNoteByPosition;
      private readonly Action<Control> _onSetFocusToNoteProgramatically;
      private readonly HtmlToolTipEx _htmlTooltip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

