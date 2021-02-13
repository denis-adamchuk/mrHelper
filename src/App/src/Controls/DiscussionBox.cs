using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
         SingleDiscussionAccessor accessor, IGitCommandService git,
         User currentUser, ProjectKey projectKey, Discussion discussion,
         User mergeRequestAuthor,
         ColorScheme colorScheme,
         Action<DiscussionBox> onContentChanging,
         Action<DiscussionBox> onContentChanged,
         Action<Control> onControlGotFocus,
         HtmlToolTipEx htmlTooltip,
         ConfigurationHelper.DiffContextPosition diffContextPosition,
         ConfigurationHelper.DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies)
      {
         Parent = parent;

         Discussion = discussion;

         _accessor = accessor;
         _editor = accessor.GetDiscussionEditor();
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _imagePath = StringUtils.GetUploadsPrefix(projectKey);

         _diffContextDepth = new ContextDepth(0, ConfigurationHelper.GetDiffContextDepth(Program.Settings));
         _tooltipContextDepth = new ContextDepth(5, 5);
         if (git != null)
         {
            _panelContextMaker = new EnhancedContextMaker(git);
            _tooltipContextMaker = new CombinedContextMaker(git);
            _simpleContextMaker = new SimpleContextMaker(git);
         }
         _colorScheme = colorScheme;
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

         _specialDiscussionNoteMarkdownPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         onCreate();
      }

      internal Discussion Discussion { get; private set; }

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
         _onControlGotFocus(sender as Control);
      }

      private void onCreate()
      {
         Debug.Assert(Discussion.Notes.Any());

         DiscussionNote firstNote = Discussion.Notes.First();
         _textboxFilename = createTextboxFilename(firstNote);
         _panelContext = createDiffContext(firstNote);
         _noteContainers = createTextBoxes(Discussion.Notes);

         Controls.Add(_textboxFilename);
         Controls.Add(_panelContext);
         foreach (NoteContainer noteContainer in _noteContainers)
         {
            Controls.Add(noteContainer.NoteInfo);
            Controls.Add(noteContainer.NoteContent);
         }
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

         string html = getFormattedHtml(_panelContextMaker, position, _diffContextDepth, fontSizePx, 2, true);
         htmlPanel.Text = html;

         if (htmlPanel.Visible)
         {
            resizeLimitedWidthHtmlPanel(htmlPanel, prevWidth);
         }

         string tooltipHtml = getFormattedHtml(_tooltipContextMaker, position, _tooltipContextDepth, fontSizePx, 2, false);
         _htmlTooltip.SetToolTip(htmlPanel, tooltipHtml);
      }

      private string getFormattedHtml(IContextMaker contextMaker, DiffPosition position, ContextDepth depth,
         double fontSizePx, int rowsVPaddingPx, bool fullWidth)
      {
         if (contextMaker == null)
         {
            return "<html><body>Cannot access file storage and render diff context</body></html>";
         }

         try
         {
            DiffContext context = getContext(contextMaker, position, depth);
            return DiffContextFormatter.GetHtml(context, fontSizePx, rowsVPaddingPx, fullWidth);
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
      }

      private DiffContext getContext(IContextMaker contextMaker, DiffPosition position, ContextDepth depth)
      {
         try
         {
            return contextMaker.GetContext(position, depth, UnchangedLinePolicy.TakeFromRight);
         }
         catch (Exception ex)
         {
            if (ex is ContextMakingException)
            {
               return _simpleContextMaker.GetContext(position, depth, UnchangedLinePolicy.TakeFromRight);
            }
            throw;
         }
      }

      private Control createTextboxFilename(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         string oldPath = firstNote.Position.Old_Path + " (line " + firstNote.Position.Old_Line + ")";
         string newPath = firstNote.Position.New_Path + " (line " + firstNote.Position.New_Line + ")";

         Color textColor;
         string result;
         if (firstNote.Position.Old_Line == null)
         {
            result = newPath;
            textColor = Color.Green;
         }
         else if (firstNote.Position.New_Line == null)
         {
            result = oldPath;
            textColor = Color.Red;
         }
         else if (firstNote.Position.Old_Path == firstNote.Position.New_Path)
         {
            result = newPath;
            textColor = Color.Black;
         }
         else
         {
            result = newPath + "\r\n(was " + oldPath + ")";
            textColor = Color.Blue;
         }

         TextBox textBox = new SearchableTextBox(Parent as IHighlightListener)
         {
            ReadOnly = true,
            Text = result,
            Multiline = true,
            WordWrap = false,
            BorderStyle = BorderStyle.None,
            ForeColor = textColor
         };
         textBox.GotFocus += Control_GotFocus;
         return textBox;
      }

      private IEnumerable<NoteContainer> createTextBoxes(IEnumerable<DiscussionNote> notes)
      {
         bool discussionResolved = notes.Cast<DiscussionNote>().All(x => (!x.Resolvable || x.Resolved));

         List<NoteContainer> boxes = new List<NoteContainer>();
         foreach (DiscussionNote note in notes)
         {
            if (note == null || note.System)
            {
               // skip spam
               continue;
            }

            NoteContainer textBox = createNoteContainer(note, discussionResolved);
            boxes.Add(textBox);
         }
         return boxes;
      }

      private bool canBeModified(DiscussionNote note)
      {
         if (note == null)
         {
            return false;
         }
         return note.Author.Id == _currentUser.Id;
      }

      private NoteContainer createNoteContainer(DiscussionNote note, bool discussionResolved)
      {
         NoteContainer noteContainer = new NoteContainer
         {
            NoteInfo = new Label
            {
               Text = getNoteInformation(note),
               AutoSize = true
            }
         };

         if (!isServiceDiscussionNote(note))
         {
            Control noteControl = new SearchableHtmlPanel(Parent as IHighlightListener)
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

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath, _specialDiscussionNoteMarkdownPipeline);
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

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath, _specialDiscussionNoteMarkdownPipeline);
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

         addMenuItem((discussionResolved ? "Unresolve" : "Resolve") + " Thread", isDiscussionResolvable(), onMenuItemToggleResolveDiscussion);
         addMenuItem((note.Resolvable && note.Resolved ? "Unresolve" : "Resolve") + " Note", note.Resolvable, onMenuItemToggleResolveNote);
         addMenuItem("-", true, null);
         addMenuItem("Delete note", canBeModified(note), onMenuItemDeleteNote);
         addMenuItem("Edit note", canBeModified(note), onMenuItemEditNote, Shortcut.F2);
         addMenuItem("Reply", true, onMenuItemReply);
         addMenuItem("Reply and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread", true, onMenuItemReplyAndResolve, Shortcut.F4);
         addMenuItem("Reply \"Done\" and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread", isDiscussionResolvable(), onMenuItemReplyDone, Shortcut.ShiftF4);
         addMenuItem("-", true, null);
         addMenuItem("View Note as plain text", true, onMenuItemViewNote, Shortcut.F6);
         addMenuItem("-", true, null);

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
         Color defaultColor = Color.White;

         if (isServiceDiscussionNote(note))
         {
            return _colorScheme.GetColorOrDefault("Discussions_ServiceMessages",
               _colorScheme.GetColorOrDefault("Discussions_Comments", defaultColor));
         }

         if (note.Resolvable)
         {
            if (note.Author.Id == _mergeRequestAuthor.Id)
            {
               return note.Resolved
                  ? _colorScheme.GetColorOrDefault("Discussions_Author_Notes_Resolved", defaultColor)
                  : _colorScheme.GetColorOrDefault("Discussions_Author_Notes_Unresolved", defaultColor);
            }
            else
            {
               return note.Resolved
                  ? _colorScheme.GetColorOrDefault("Discussions_NonAuthor_Notes_Resolved", defaultColor)
                  : _colorScheme.GetColorOrDefault("Discussions_NonAuthor_Notes_Unresolved", defaultColor);
            }
         }
         else
         {
            return _colorScheme.GetColorOrDefault("Discussions_Comments", defaultColor);
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
         if (_noteContainers != null)
         {
            IEnumerable<Control> noteContentControls = _noteContainers.Select(container => container.NoteContent);
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
         }

         if (_textboxFilename != null)
         {
            _textboxFilename.Width = getDiffContextWidth(width);
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
         }

         repositionNotes(width, controlPos);
      }

      private void repositionNotes(int width, Point controlPos)
      {
         if (_noteContainers != null)
         {
            foreach (NoteContainer noteContainer in _noteContainers)
            {
               bool needOffsetNote = noteContainer != _noteContainers.First();
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
            if (_noteContainers != null)
            {
               notesHeight = _noteContainers.Last().NoteContent.Location.Y
                           + _noteContainers.Last().NoteContent.Height
                           - _noteContainers.First().NoteInfo.Location.Y;
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
         if (_noteContainers != null && _textboxFilename != null)
         {
            boxHeight = _noteContainers.Last().NoteContent.Location.Y
                      + _noteContainers.Last().NoteContent.Height
                      - _textboxFilename.Location.Y;
         }
         else if (_noteContainers != null)
         {
            boxHeight = _noteContainers.Last().NoteContent.Location.Y
                      + _noteContainers.Last().NoteContent.Height
                      - _noteContainers.First().NoteInfo.Location.Y;
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
         bool isAlreadyResolved = isDiscussionResolved();
         string resolveText = String.Format("{0} Thread", (isAlreadyResolved ? "Unresolve" : "Resolve"));
         NoteEditPanel actions = new NoteEditPanel(resolveText, proposeUserToToggleResolveOnReply);
         using (TextEditForm form = new TextEditForm("Reply to Discussion", "", true, true, actions, _imagePath))
         {
            actions.SetTextbox(form.TextBox);
            if (form.ShowDialog() == DialogResult.OK)
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
            string message = "Cannot create a reply to discussion";
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
            string message = "Cannot update discussion text";
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
            string message = "Cannot delete a note";
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
            string message = "Cannot toggle 'Resolved' state of a note";
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
            string message = "Cannot toggle 'Resolved' state of a thread";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            discussion = Discussion;
         }

         await refreshDiscussion(discussion);
      }

      private void disableAllNoteControls()
      {
         void disableNoteControl(Control noteControl)
         {
            if (noteControl != null)
            {
               noteControl.BackColor = Color.LightGray;
               noteControl.ContextMenu = new ContextMenu();
               noteControl.Tag = null;
            }
         }

         foreach (Control textBox in _noteContainers.Select(container => container.NoteContent))
         {
            disableNoteControl(textBox);
         }
      }

      async private Task refreshDiscussion(Discussion discussion = null)
      {
         if (Parent == null)
         {
            return;
         }

         void prepareToRefresh()
         {
            // Get rid of old text boxes
            // #227:
            // It must be done before `await` because context menu shown for invisible control throws ArgumentException.
            // So if we hide text boxes in _onContentChanging() and process WM_MOUSEUP in `await` below we're in a trouble.
            for (int iControl = Controls.Count - 1; iControl >= 0; --iControl)
            {
               foreach (NoteContainer container in (_noteContainers ?? Array.Empty<NoteContainer>()))
               {
                  if (container.NoteContent == Controls[iControl] || container.NoteInfo == Controls[iControl])
                  {
                     Controls.Remove(Controls[iControl]);
                     break;
                  }
               }
            }
            _noteContainers = null;

            // To suspend layout and hide me
            _onContentChanging();
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

         if (Discussion == null || Discussion.Notes.Count() == 0 || Discussion.Notes.First().System)
         {
            // Possible cases:
            // - deleted note was the only discussion item
            // - deleted note was the only visible discussion item but there are System notes like 'a line changed ...'
            prepareToRefresh();
            Parent?.Controls.Remove(this);
            _onContentChanged();
            return;
         }

         prepareToRefresh();

         // Create controls
         _noteContainers = createTextBoxes(Discussion.Notes);
         foreach (NoteContainer noteContainer  in _noteContainers)
         {
            Controls.Add(noteContainer.NoteInfo);
            Controls.Add(noteContainer.NoteContent);
         }

         // To reposition new controls and unhide me back
         _onContentChanged();
         _noteContainers.First().NoteContent.Focus();
      }

      private bool isDiscussionResolved()
      {
         bool result = true;
         if (_noteContainers != null)
         {
            foreach (Control noteControl in _noteContainers.Select(container => container.NoteContent))
            {
               if (noteControl != null)
               {
                  DiscussionNote note = getNoteFromControl(noteControl);
                  if (note != null && note.Resolvable && !note.Resolved)
                  {
                     result = false;
                  }
               }
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

      private Control _textboxFilename;
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

      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly IContextMaker _simpleContextMaker;
      private readonly SingleDiscussionAccessor _accessor;
      private readonly IDiscussionEditor _editor;

      private ConfigurationHelper.DiffContextPosition _diffContextPosition;
      private ConfigurationHelper.DiscussionColumnWidth _discussionColumnWidth;
      private bool _needShiftReplies;

      private readonly ColorScheme _colorScheme;
      private readonly Action _onContentChanging;
      private readonly Action _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;
      private readonly HtmlToolTipEx _htmlTooltip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

