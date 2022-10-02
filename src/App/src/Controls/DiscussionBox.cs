using System;
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
         Control parent,
         GitLabClient.SingleDiscussionAccessor accessor, IGitCommandService git,
         User currentUser, MergeRequestKey mergeRequestKey, Discussion discussion,
         User mergeRequestAuthor,
         ColorScheme colorScheme,
         Action<DiscussionBox> onContentChanging,
         Action<DiscussionBox> onContentChanged,
         Action<Control> onControlGotFocus,
         Action onSetFocusToNoteProgramatically,
         Action undoFocusChangedOnClick,
         HtmlToolTipEx htmlTooltip,
         PopupWindow popupWindow,
         ConfigurationHelper.DiffContextPosition diffContextPosition,
         ConfigurationHelper.DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies,
         ContextDepth diffContextDepth,
         AvatarImageCache avatarImageCache,
         RoundedPathCache pathCache,
         string webUrl,
         Action<string> selectNoteByUrl,
         Action<ENoteSelectionRequest, DiscussionBox> selectNoteByPosition,
         HtmlPanel htmlPanelForWidthCalculation)
      {
         Discussion = discussion;

         _accessor = accessor;
         _editor = accessor.GetDiscussionEditor();
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _mergeRequestKey = mergeRequestKey;
         _imagePath = StringUtils.GetUploadsPrefix(mergeRequestKey.ProjectKey);
         _avatarImageCache = avatarImageCache;
         _pathCache = pathCache;
         _webUrl = webUrl;

         _diffContextDepth = diffContextDepth;
         _popupDiffContextDepth = new ContextDepth(5, 5);
         if (git != null)
         {
            _panelContextMaker = new EnhancedContextMaker(git);
            _simpleContextMaker = new SimpleContextMaker(git);
            _popupContextMaker = _simpleContextMaker;
         }
         _colorScheme = colorScheme;
         _colorScheme.Changed += onColorSchemeChanged;

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
         _htmlPanelForWidthCalculation = htmlPanelForWidthCalculation;

         _specialDiscussionNoteMarkdownPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         onCreate(parent);
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         disposePopupContext();
         _popupWindow.Closed -= onPopupWindowClosed;
         _popupWindow = null;
         _htmlPanelForWidthCalculation = null;

         _colorScheme.Changed -= onColorSchemeChanged;

         foreach (NoteContainer noteContainer in getNoteContainers())
         {
            noteContainer.NoteInfo.ContextMenu?.Dispose();
            noteContainer.NoteContent.ContextMenu?.Dispose();
         }
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

      internal bool SelectNote(int noteId, int? prevNoteId)
      {
         foreach (Control noteControl in getNoteContents())
         {
            if (getNoteFromControl(noteControl).Id != noteId)
            {
               continue;
            }

            noteControl.Focus();
            _onSetFocusToNoteProgramatically();
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
               noteControl.Focus();
               _onSetFocusToNoteProgramatically();
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
               noteControl.Focus();
               _onSetFocusToNoteProgramatically();
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
                     current.Prev.NoteContent.Focus();
                     _onSetFocusToNoteProgramatically();
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
                     current.Next.NoteContent.Focus();
                     _onSetFocusToNoteProgramatically();
                  }
                  else
                  {
                     _selectNoteByPosition.Invoke(ENoteSelectionRequest.Next, this);
                  }
               }
               break;

            case ENoteSelectionRequest.First:
               getNoteContents().First().Focus();
               _onSetFocusToNoteProgramatically();
               break;

            case ENoteSelectionRequest.Last:
               getNoteContents().Last().Focus();
               _onSetFocusToNoteProgramatically();
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

         int currentOffset = 0;
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
            int newOffset = currentOffset;
            newOffset += step;
            string text = getPopupDiffContextText(
               _popupContext, _popupDiffContextDepth, newOffset, _panelContext.Width);
            if (text != _popupContext.Text)
            {
               _popupContext.SuspendLayout();
               setPopupWindowText(text);
               _popupContext.ResumeLayout();
               currentOffset = newOffset;
            }
         };

         setPopupWindowText(getPopupDiffContextText(
            _popupContext, _popupDiffContextDepth, currentOffset, _panelContext.Width));

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
         DiscussionNote note = noteLink.Tag as DiscussionNote;
         if (note == null)
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
         Timer copyToClipboardTimer = new Timer
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
            context = getContextSafe(_panelContextMaker, position, _diffContextDepth, 0);
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
         if (_showMoreContext != null)
         {
            _showMoreContext.Enabled = false;
         }
      }

      private void onPopupWindowClosed(object sender, ToolStripDropDownClosedEventArgs e)
      {
         _showMoreContextHint?.Hide();
         if (_showMoreContext != null)
         {
            _showMoreContext.Enabled = true;
         }
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
         diffContextControl.FontChanged += (sender, e) => setDiffContextText(diffContextControl);

         setDiffContextText(diffContextControl);

         return diffContextControl;
      }

      private void setDiffContextText(Control diffContextControl)
      {
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(diffContextControl);

         DiscussionNote note = getNoteFromControl(diffContextControl);
         Debug.Assert(note.Type == "DiffNote");
         DiffPosition position = PositionConverter.Convert(note.Position);

         Debug.Assert(diffContextControl is HtmlPanel);
         HtmlPanel htmlPanel = diffContextControl as HtmlPanel;

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         int prevWidth = htmlPanel.Width;

         if (htmlPanel.Visible)
         {
            htmlPanel.Width = 0;
            htmlPanel.Height = 0;
         }

         DiffContext? context = getContextSafe(_panelContextMaker, position, _diffContextDepth, 0);
         string html = context.HasValue
            ? getFormattedHtml(context.Value, fontSizePx, null)
            : getErrorHtml("Cannot create a diff context for discussion");
         htmlPanel.Text = html;

         if (htmlPanel.Visible)
         {
            resizeLimitedWidthHtmlPanel(htmlPanel, prevWidth, DiffContextExtraHeight);
         }
      }

      private string getPopupDiffContextText(Control popupContextControl, ContextDepth depth, int offset, int minWidth)
      {
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(popupContextControl);

         DiscussionNote note = getNoteFromControl(popupContextControl);
         Debug.Assert(note.Type == "DiffNote");
         DiffPosition position = PositionConverter.Convert(note.Position);

         DiffContext? context = getContextSafe(_popupContextMaker, position, depth, offset);
         if (context != null)
         {
            int? tableWidth = estimateHtmlWidth(context.Value, fontSizePx, minWidth);
            return getFormattedHtml(context.Value, fontSizePx, tableWidth);
         }
         return getErrorHtml("Cannot create a diff context for popup window");
      }

      private DiffContext? getContextSafe(IContextMaker contextMaker, DiffPosition position, ContextDepth depth, int offset)
      {
         DiffContext getContext()
         {
            try
            {
               return contextMaker.GetContext(position, depth, offset, UnchangedLinePolicy.TakeFromRight);
            }
            catch (ContextMakingException) // fallback
            {
               return _simpleContextMaker.GetContext(position, depth, offset, UnchangedLinePolicy.TakeFromRight);
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

      private int estimateHtmlWidth(DiffContext context, double fontSizePx, int minWidth)
      {
         string longestLine = context.Lines
            .Select(line => StringUtils.CodeToHtml(line.Text))
            .OrderBy(line => line.Length)
            .LastOrDefault();
         if (longestLine != null)
         {
            _htmlPanelForWidthCalculation.Width = minWidth;
            while (true)
            {
               string html = DiffContextFormatter.GetHtml(longestLine, fontSizePx, 0, null);
               _htmlPanelForWidthCalculation.Text = html;
               if (_htmlPanelForWidthCalculation.AutoScrollMinSize.Height <= fontSizePx + 2
                || _htmlPanelForWidthCalculation.Width >= 9999) // safety limit
               {
                  return Math.Max(_htmlPanelForWidthCalculation.AutoScrollMinSize.Width, minWidth);
               }
               else
               {
                  _htmlPanelForWidthCalculation.Width = Convert.ToInt32(_htmlPanelForWidthCalculation.Width * 1.1);
                  continue;
               }
            }
         }
         return minWidth;
      }

      private string getFormattedHtml(DiffContext context, double fontSizePx, int? tableWidth)
      {
         string errorMessage = "Cannot render HTML context.";
         try
         {
            return DiffContextFormatter.GetHtml(context, fontSizePx, 0, tableWidth);
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
            ForeColor = getFileNameColor(firstNote.Position),
            TabStop = false
         };
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

      private Color getFileNameColor(Position position)
      {
         if (position.Old_Line == null)
         {
            return Color.Green;
         }
         else if (position.New_Line == null)
         {
            return Color.Red;
         }
         else if (position.Old_Path == position.New_Path)
         {
            return Color.Black;
         }
         return Color.Blue;
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
            Text = "Scroll up/down with mouse wheel",
            ForeColor = Color.Olive,
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
            Text = "Copy to clipboard",
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

         noteContainer.NoteAvatar = new PictureBox
         {
            Image = _avatarImageCache.GetAvatar(note.Author, this.BackColor),
            SizeMode = PictureBoxSizeMode.StretchImage
         };

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

         void updateStylesheet(HtmlPanel htmlPanel)
         {
            htmlPanel.BaseStylesheet = String.Format(
               "{0} body div {{ font-size: {1}px; padding-left: {2}px; padding-right: {3}px; }}",
               Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanel),
               NoteHtmlPaddingLeft, NoteHtmlPaddingRight);
         }

         if (!isServiceDiscussionNote(note))
         {
            HtmlPanel noteControl = new SearchableHtmlPanel(parent as IHighlightListener, _pathCache)
            {
               BackColor = getNoteColor(note),
               Tag = note,
               Parent = this,
               IsContextMenuEnabled = false
            };
            noteControl.GotFocus += control_GotFocus;
            noteControl.ContextMenu = createContextMenuForDiscussionNote(note, noteControl, discussionResolved);
            noteControl.FontChanged += (sender, e) =>
            {
               updateStylesheet(noteControl as HtmlPanel);
               setDiscussionNoteText(noteControl, getNoteFromControl(noteControl));
               updateNoteTooltip(noteControl, getNoteFromControl(noteControl));
            };
            noteControl.LinkClicked += noteControl_LinkClicked;
            noteControl.KeyDown += (s, e) => noteControl_KeyDown(e);

            updateStylesheet(noteControl as HtmlPanel);
            setDiscussionNoteText(noteControl, note);
            updateNoteTooltip(noteControl, note);

            noteContainer.NoteContent = noteControl;
         }
         else
         {
            HtmlPanel noteControl = new HtmlPanelEx(_pathCache, true, true)
            {
               BackColor = getNoteColor(note),
               Tag = note,
               Parent = this
            };
            noteControl.GotFocus += control_GotFocus;
            noteControl.FontChanged += (sender, e) =>
            {
               updateStylesheet(noteControl as HtmlPanel);
               setServiceDiscussionNoteText(noteControl, getNoteFromControl(noteControl));
            };
            noteControl.LinkClicked += noteControl_LinkClicked;
            noteControl.KeyDown += (s, e) => noteControl_KeyDown(e);

            updateStylesheet(noteControl as HtmlPanel);
            setServiceDiscussionNoteText(noteControl, note);

            noteContainer.NoteContent = noteControl;
         }

         return noteContainer;
      }

      private void updateNoteTooltip(Control noteControl, DiscussionNote note)
      {
         _htmlTooltip.SetToolTip(noteControl, getNoteTooltipHtml(noteControl, note));
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

         string replyDoneText = "Reply \"Done\" and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread";
         addMenuItem(replyDoneText, canAddNotes() && isDiscussionResolvable(), onMenuItemReplyDone, Shortcut.ShiftF4);

         addSeparator();

         addMenuItem("View Note as plain text", true, onMenuItemViewNote, Shortcut.F6);

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

      private string getNoteTooltipHtml(Control noteControl, DiscussionNote note)
      {
         if (note == null)
         {
            return String.Empty;
         }

         System.Text.StringBuilder body = new System.Text.StringBuilder();
         if (note.Resolvable)
         {
            string text = note.Resolved ? "Resolved." : "Not resolved.";
            string color = note.Resolved ? "green" : "red";
            body.AppendFormat("<i style=\"color: {0}\">{1}&nbsp;&nbsp;&nbsp;</i>", color, text);
         }
         body.AppendFormat("Created by <b> {0} </b> at <span style=\"color: blue\">{1}</span>",
            note.Author.Name, TimeUtils.DateTimeToString(note.Created_At));
         body.AppendFormat("<br><br>Use context menu to view note as <b>plain text</b>.");

         string css = String.Format("{0} body div {{ font-size: {1}px; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(noteControl));

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
            css, body);
      }

      private Color getNoteColor(DiscussionNote note)
      {
         Color getColorOrDefault(string colorName)
         {
            Color defaultColor = Color.White;
            ColorSchemeItem colorOpt = _colorScheme?.GetColor(colorName);
            return colorOpt != null ? colorOpt.Color : defaultColor;
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

      private void onColorSchemeChanged()
      {
         getNoteContainers()?
            .ToList()
            .ForEach(noteContainer =>
               noteContainer.NoteContent.BackColor =
                  getNoteColor(noteContainer.NoteContent.Tag as DiscussionNote));
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

            int noteAvatarHeight = (int)Math.Ceiling(noteContainer.NoteInfo.Height * 2.25);
            int noteAvatarWidth = noteAvatarHeight;
            noteContainer.NoteAvatar.Size = new Size(noteAvatarWidth, noteAvatarHeight);

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
               - (_showMoreContextHint == null ? 0 : _showMoreContextHint.Width)
               - (_showMoreContext == null ? 0 : _showMoreContext.Width)
               - (_copyToClipboard == null ? 0 : _copyToClipboard.Width)
               - (_copyToClipboard == null && _showMoreContextHint == null && _showMoreContext == null ? 0 : 50);
            _textboxFilename.Height = (_textboxFilename as TextBoxEx).FullPreferredHeight;
         }

         if (_panelContext != null)
         {
            resizeLimitedWidthHtmlPanel(_panelContext as HtmlPanel, getDiffContextWidth(width), DiffContextExtraHeight);
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
               contextPos.Offset(0, _textboxFilename.Height + 2);
            }

            if (_panelContext != null)
            {
               _panelContext.Location = contextPos;

               if (_showMoreContextHint != null)
               {
                  _showMoreContextHint.Location = new Point(
                     _panelContext.Location.X + _panelContext.Width - _showMoreContextHint.Width, 0);

                  if (_showMoreContext != null)
                  {
                     _showMoreContextHint.Location = new Point(
                        _showMoreContextHint.Location.X - _showMoreContext.Width - 20, _showMoreContextHint.Location.Y);
                  }

                  if (_copyToClipboard != null)
                  {
                     _showMoreContextHint.Location = new Point(
                        _showMoreContextHint.Location.X - _copyToClipboard.Width - 20, _showMoreContextHint.Location.Y);
                  }
               }

               if (_showMoreContext != null)
               {
                  _showMoreContext.Location = new Point(
                     _panelContext.Location.X + _panelContext.Width - _showMoreContext.Width, 0);

                  if (_copyToClipboard != null)
                  {
                     _showMoreContext.Location = new Point(
                        _showMoreContext.Location.X - _copyToClipboard.Width - 20, _showMoreContext.Location.Y);
                  }
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
            controlPos.Offset(0, _textboxFilename.Height + 2);
         }

         if (_panelContext != null)
         {
            _panelContext.Location = controlPos;
            controlPos.Offset(0, _panelContext.Height + 5);

            if (_showMoreContextHint != null)
            {
               _showMoreContextHint.Location = new Point(
                  _panelContext.Location.X + _panelContext.Width - _showMoreContextHint.Width, 0);

               if (_showMoreContext != null)
               {
                  _showMoreContextHint.Location = new Point(
                     _showMoreContextHint.Location.X - _showMoreContext.Width - 20, _showMoreContextHint.Location.Y);
               }

               if (_copyToClipboard != null)
               {
                  _showMoreContextHint.Location = new Point(
                     _showMoreContextHint.Location.X - _copyToClipboard.Width - 20, _showMoreContextHint.Location.Y);
               }
            }

            if (_showMoreContext != null)
            {
               _showMoreContext.Location = new Point(
                  _panelContext.Location.X + _panelContext.Width - _showMoreContext.Width, 0);

               if (_copyToClipboard != null)
               {
                  _showMoreContext.Location = new Point(
                     _showMoreContext.Location.X - _copyToClipboard.Width - 20, _showMoreContext.Location.Y);
               }
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
            controlPos.Offset(0, noteContainer.NoteInfo.Height + 2);

            {
               Point noteContentPos = controlPos;
               noteContentPos.Offset(noteHorzOffset + AvatarPaddingRight + noteContainer.NoteAvatar.Width, 0);
               noteContainer.NoteContent.Location = noteContentPos;
            }
            controlPos.Offset(0, noteContainer.NoteContent.Height + 5);
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
         NoteEditPanel actions = new NoteEditPanel();
         using (TextEditForm form = new TextEditForm("Edit Discussion Note", currentBody, true, true, actions, _imagePath))
         {
            Point locationAtScreen = noteControl.PointToScreen(new Point(0, 0));
            form.StartPosition = FormStartPosition.Manual;
            form.Location = locationAtScreen;

            actions.SetTextbox(form.TextBox);
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
         using (TextEditForm form = new TextEditForm("View Discussion Note", currentBody, false, true, null, _imagePath))
         {
            Point locationAtScreen = noteControl.PointToScreen(new Point(0, 0));
            form.StartPosition = FormStartPosition.Manual;
            form.Location = locationAtScreen;
            form.ShowDialog();
         }
      }

      async private Task onReplyToDiscussionAsync(bool proposeUserToToggleResolveOnReply)
      {
         if (!canAddNotes())
         {
            return;
         }

         bool isAlreadyResolved = isDiscussionResolved();
         string resolveText = String.Format("{0} Thread", (isAlreadyResolved ? "Unresolve" : "Resolve"));
         NoteEditPanel actions = new NoteEditPanel(resolveText, proposeUserToToggleResolveOnReply);
         using (TextEditForm form = new TextEditForm("Reply to Discussion", "", true, true, actions, _imagePath))
         {
            actions.SetTextbox(form.TextBox);
            if (WinFormsHelpers.ShowDialogOnControl(form, this) == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Reply text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               string proposedBody = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
               await onReplyAsync(proposedBody, actions.IsResolveActionChecked);
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

      private void disableAllNoteControls()
      {
         void disableNoteControl(Control noteControl)
         {
            if (noteControl != null)
            {
               noteControl.BackColor = Color.LightGray;
               noteControl.ContextMenu?.Dispose();
               noteControl.ContextMenu = new ContextMenu();
               noteControl.Tag = null;
            }
         }

         foreach (Control noteControl in getNoteContents())
         {
            disableNoteControl(noteControl);
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
         getNoteContainers().First().NoteContent.Focus();
         _onSetFocusToNoteProgramatically();
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

      private DiscussionNote getNoteFromControl(Control noteControl)
      {
         Debug.Assert(noteControl is HtmlPanel);
         return (noteControl == null || noteControl.Tag == null) ? null : (DiscussionNote)(noteControl.Tag);
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
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   65 }
      };
      private readonly Dictionary<ConfigurationHelper.DiscussionColumnWidth, int> NoteWidth_Percents_TwoColumns =
         new Dictionary<ConfigurationHelper.DiscussionColumnWidth, int>
      {
         { ConfigurationHelper.DiscussionColumnWidth.Narrow, 36 },
         { ConfigurationHelper.DiscussionColumnWidth.NarrowPlus, 39 },
         { ConfigurationHelper.DiscussionColumnWidth.Medium, 42 },
         { ConfigurationHelper.DiscussionColumnWidth.MediumPlus, 45 },
         { ConfigurationHelper.DiscussionColumnWidth.Wide,   48 }
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

      private readonly int AvatarPaddingTop = 5;
      private readonly int AvatarPaddingRight = 10;
      private readonly int BackLinkPaddingRight = 10;

      private readonly int ServiceNoteExtraWidth = 4;
      private readonly int ServiceNoteExtraHeight = 4;
      private readonly int NormalNoteExtraHeight = 2;
      private readonly int DiffContextExtraHeight = 0;
      private readonly int NoteHtmlPaddingLeft = 4;
      private readonly int NoteHtmlPaddingRight = 20;

      private Control _textboxFilename;
      private Control _showMoreContextHint;
      private Control _showMoreContext;
      private Control _copyToClipboard;
      private Control _panelContext;

      private class NoteContainer
      {
         public Control NoteInfo;
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
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _imagePath;
      private readonly AvatarImageCache _avatarImageCache;
      private readonly RoundedPathCache _pathCache;
      private readonly string _webUrl;
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
      private HtmlPanel _htmlPanelForWidthCalculation; // shared between other Discussion Boxes

      private readonly ColorScheme _colorScheme;
      private readonly Action _onContentChanging;
      private readonly Action _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;
      private readonly Action _undoFocusChangedOnClick;
      private readonly Action<string> _selectNoteUrl;
      private readonly Action<ENoteSelectionRequest, DiscussionBox> _selectNoteByPosition;
      private readonly Action _onSetFocusToNoteProgramatically;
      private readonly HtmlToolTipEx _htmlTooltip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

