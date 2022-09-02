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
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal class DiscussionBox : Panel
   {
      internal DiscussionBox(
         Control parent,
         GitLabClient.SingleDiscussionAccessor accessor, IGitCommandService git,
         User currentUser, ProjectKey projectKey, Discussion discussion,
         User mergeRequestAuthor,
         ColorScheme colorScheme,
         Action<DiscussionBox> onContentChanging,
         Action<DiscussionBox> onContentChanged,
         Action<Control> onControlGotFocus,
         HtmlToolTipEx htmlTooltip,
         PopupWindow popupWindow,
         ConfigurationHelper.DiffContextPosition diffContextPosition,
         ConfigurationHelper.DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies,
         ContextDepth diffContextDepth)
      {
         Discussion = discussion;

         _accessor = accessor;
         _editor = accessor.GetDiscussionEditor();
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _imagePath = StringUtils.GetUploadsPrefix(projectKey);

         _diffContextDepth = diffContextDepth;
         _popupDiffContextDepth = new ContextDepth(5, 5);
         if (git != null)
         {
            _panelContextMaker = new EnhancedContextMaker(git);
            _popupContextMaker = new CombinedContextMaker(git);
            _simpleContextMaker = new SimpleContextMaker(git);
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

         _htmlTooltip = htmlTooltip;
         _popupWindow = popupWindow;
         _popupWindow.Closed += onPopupWindowClosed;

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

      private void Control_GotFocus(object sender, EventArgs e)
      {
         _onControlGotFocus?.Invoke(sender as Control);
      }

      private void onShowMoreContextClick(object sender, EventArgs e)
      {
         DiscussionNote note = _panelContext == null ? null : getNoteFromControl(_panelContext);
         if (note == null )
         {
            return;
         }
         
         void setPopupWindowText(string text)
         {
            _popupContext.Text = text;
            resizeLimitedWidthHtmlPanel(_popupContext, _panelContext.Width);
         }

         int currentOffset = 0;
         Debug.Assert(_popupContext == null); // it should have been disposed and reset when popup window closes
         _popupContext = new HtmlPanel
         {
            BorderStyle = BorderStyle.FixedSingle,
            TabStop = false,
            Font = Font,
            Tag = note
         };
         _popupContext.MouseWheel += (sender2, e2) =>
         {
            int step = e2.Delta > 0 ? -1 : 1;
            int newOffset = currentOffset;
            newOffset += step;
            string text = getPopupDiffContextText(_popupContext, _popupDiffContextDepth, newOffset);
            if (text != _popupContext.Text)
            {
               setPopupWindowText(text);
               currentOffset = newOffset;
            }
         };

         setPopupWindowText(getPopupDiffContextText(_popupContext, _popupDiffContextDepth, currentOffset));

         _popupWindow.SetContent(_popupContext, PopupContextPadding);
         showPopupWindow();
      }

      private void onCopyToClipboardContextClick(object sender, EventArgs e)
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

         disableCopyToClipboard();
         scheduleCopyToClipboardStateChange();
      }

      private void scheduleCopyToClipboardStateChange()
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
            enableCopyToClipboard();
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
            context = getContext(_panelContextMaker, position, _diffContextDepth, 0);
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
      }

      private void onPopupWindowClosed(object sender, ToolStripDropDownClosedEventArgs e)
      {
         _showMoreContextHint?.Hide();
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

         Control diffContextControl = new HtmlPanel
         {
            BorderStyle = BorderStyle.FixedSingle,
            TabStop = false,
            Tag = firstNote,
            Parent = this
         };
         diffContextControl.GotFocus += Control_GotFocus;
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

         string html = getFormattedHtml(_panelContextMaker, position, _diffContextDepth, fontSizePx, 2, true, 0);
         htmlPanel.Text = html;

         if (htmlPanel.Visible)
         {
            resizeLimitedWidthHtmlPanel(htmlPanel, prevWidth);
         }
      }

      private string getPopupDiffContextText(Control popupContextControl, ContextDepth depth, int offset)
      {
         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(popupContextControl);

         DiscussionNote note = getNoteFromControl(popupContextControl);
         Debug.Assert(note.Type == "DiffNote");
         DiffPosition position = PositionConverter.Convert(note.Position);

         return getFormattedHtml(_popupContextMaker, position, depth, fontSizePx, 2, true, offset);
      }

      private string getFormattedHtml(IContextMaker contextMaker, DiffPosition position, ContextDepth depth,
         double fontSizePx, int rowsVPaddingPx, bool fullWidth, int offset)
      {
         if (contextMaker == null)
         {
            return "<html><body>Cannot access file storage and render diff context</body></html>";
         }

         string errorMessage = "Cannot render HTML context.";
         try
         {
            DiffContext context = getContext(contextMaker, position, depth, offset);
            return DiffContextFormatter.GetHtml(context, fontSizePx, rowsVPaddingPx, fullWidth);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(errorMessage, ex);
         }
         catch (ContextMakingException ex)
         {
            ExceptionHandlers.Handle(errorMessage, ex);
         }
         return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
      }

      private DiffContext getContext(IContextMaker contextMaker, DiffPosition position, ContextDepth depth, int offset)
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
            ForeColor = getFileNameColor(firstNote.Position)
         };
         textBox.GotFocus += Control_GotFocus;
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
            BorderStyle = BorderStyle.None
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
            BorderStyle = BorderStyle.None
         };
         linkLabel.Click += onCopyToClipboardContextClick;
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
               if (container.NoteContent == control || container.NoteInfo == control)
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

      private IEnumerable<NoteContainer> createNoteContainers(Control parent, IEnumerable<DiscussionNote> allNotes)
      {
         if (parent == null)
         {
            return null;
         }

         bool discussionResolved = allNotes
            .Cast<DiscussionNote>()
            .All(note => !note.Resolvable || note.Resolved);

         List<NoteContainer> boxes = new List<NoteContainer>();
         IEnumerable<DiscussionNote> notes = allNotes.Where(item => shouldCreateNoteContainer(item));
         foreach (DiscussionNote note in notes)
         {
            boxes.Add(createNoteContainer(parent, note, discussionResolved));
         }
         return boxes;
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
         NoteContainer noteContainer = new NoteContainer
         {
            NoteInfo = new Label
            {
               Text = getNoteInformation(note),
               AutoSize = true
            }
         };
         noteContainer.NoteInfo.Invalidated += (_, __) =>
            noteContainer.NoteInfo.Text = getNoteInformation(note);

         if (!isServiceDiscussionNote(note))
         {
            Control noteControl = new SearchableHtmlPanel(parent as IHighlightListener)
            {
               AutoScroll = false,
               BackColor = getNoteColor(note),
               BorderStyle = BorderStyle.FixedSingle,
               Tag = note,
               Parent = this,
               IsContextMenuEnabled = false
            };
            noteControl.GotFocus += Control_GotFocus;
            noteControl.ContextMenu = createContextMenuForDiscussionNote(note, noteControl, discussionResolved);
            noteControl.FontChanged += (sender, e) =>
            {
               updateStylesheet(noteControl as HtmlPanel);
               setDiscussionNoteText(noteControl, getNoteFromControl(noteControl));
               updateNoteTooltip(noteControl, getNoteFromControl(noteControl));
            };

            updateStylesheet(noteControl as HtmlPanel);
            setDiscussionNoteText(noteControl, note);
            updateNoteTooltip(noteControl, note);

            noteContainer.NoteContent = noteControl;
         }
         else
         {
            Control noteControl = new HtmlPanel
            {
               BackColor = getNoteColor(note),
               BorderStyle = BorderStyle.FixedSingle,
               Tag = note,
               Parent = this
            };
            noteControl.GotFocus += Control_GotFocus;
            noteControl.FontChanged += (sender, e) =>
            {
               updateStylesheet(noteControl as HtmlPanel);
               setServiceDiscussionNoteText(noteControl, getNoteFromControl(noteControl));
            };

            updateStylesheet(noteControl as HtmlPanel);
            setServiceDiscussionNoteText(noteControl, note);

            noteContainer.NoteContent = noteControl;
         }

         return noteContainer;
      }

      private void updateStylesheet(HtmlPanel htmlPanel)
      {
         htmlPanel.BaseStylesheet = String.Format(
            "{0} body div {{ font-size: {1}px; padding-left: 4px; padding-right: {2}px; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanel), 20);
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
            resizeLimitedWidthHtmlPanel(noteControl as HtmlPanel, prevWidth);
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
            resizeFullSizeHtmlPanel(noteControl as HtmlPanel);
         }
      }

      private void resizeFullSizeHtmlPanel(HtmlPanel htmlPanel)
      {
         // Use computed size as the control size. Height must be set BEFORE Width.
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + 2;
         htmlPanel.Width = htmlPanel.AutoScrollMinSize.Width + 2;
      }

      private void resizeLimitedWidthHtmlPanel(HtmlPanel htmlPanel, int width)
      {
         // Turn on AutoScroll to obtain relevant HorizontalScroll visibility property values after width change
         htmlPanel.AutoScroll = true;

         // Change width to a specific value
         htmlPanel.Width = width;

         // Check if horizontal scroll bar is needed if we have the specified width
         int extraHeight = htmlPanel.HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0;

         // Turn off AutoScroll to avoid recalculating of AutoScrollMinSize on Height change.
         // htmlPanel must think that no scroll bars are needed to return full actual size.
         htmlPanel.AutoScroll = false;

         // Change height to the full actual size, leave a space for a horizontal scroll bar if needed
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + 2 + extraHeight;

         // To enable scroll bars, AutoScroll property must be on
         htmlPanel.AutoScroll = extraHeight > 0;
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
               <body>
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
         return isColumnWidthFixed
            ? Math.Min(noteWidthInUnits * Convert.ToInt32(Font.Size), getMaxFixedWidth())
            : width * noteWidthInUnits / 100;
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
         IEnumerable<Control> noteContentControls = getNoteContainers().Select(container => container.NoteContent);
         foreach (Control noteControl in noteContentControls)
         {
            bool needShrinkNote = noteControl != noteContentControls.First();
            int noteWidthDelta = needShrinkNote ? getNoteRepliesPadding(width) : 0;

            HtmlPanel htmlPanel = noteControl as HtmlPanel;
            DiscussionNote note = getNoteFromControl(noteControl);
            if (note != null && !isServiceDiscussionNote(note))
            {
               resizeLimitedWidthHtmlPanel(htmlPanel, getNoteWidth(width) - noteWidthDelta);
            }
            else
            {
               resizeFullSizeHtmlPanel(htmlPanel);
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
            resizeLimitedWidthHtmlPanel(_panelContext as HtmlPanel, getDiffContextWidth(width));
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

            Point noteInfoPos = controlPos;
            noteInfoPos.Offset(noteHorzOffset, 0);
            noteContainer.NoteInfo.Location = noteInfoPos;
            controlPos.Offset(0, noteContainer.NoteInfo.Height + 2);

            Point noteContentPos = controlPos;
            noteContentPos.Offset(noteHorzOffset, 0);
            noteContainer.NoteContent.Location = noteContentPos;
            controlPos.Offset(0, noteContainer.NoteContent.Height + 5);
         }
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

         IEnumerable<Control> noteControls = getNoteContainers().Select(container => container.NoteContent);
         foreach (Control noteControl in noteControls)
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
      }

      private bool isDiscussionResolved()
      {
         IEnumerable<Control> noteControls = getNoteContainers()
            .Select(container => container.NoteContent)
            .Where(noteControl => noteControl != null);

         bool result = true;
         foreach (Control noteControl in noteControls)
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

      private readonly Padding PopupContextPadding = new Padding(2, 1, 2, 3);

      private Control _textboxFilename;
      private Control _showMoreContextHint;
      private Control _showMoreContext;
      private Control _copyToClipboard;
      private Control _panelContext;

      private class NoteContainer
      {
         public Control NoteInfo;
         public Control NoteContent;
      }
      private IEnumerable<NoteContainer> _noteContainers;

      private readonly User _mergeRequestAuthor;
      private readonly User _currentUser;
      private readonly string _imagePath;

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
      private HtmlPanel _popupContext; // specific for this instance
      private readonly ColorScheme _colorScheme;
      private readonly Action _onContentChanging;
      private readonly Action _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;
      private readonly HtmlToolTipEx _htmlTooltip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

