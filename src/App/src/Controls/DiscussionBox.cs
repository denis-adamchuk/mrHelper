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
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using mrHelper.CustomActions;

namespace mrHelper.App.Controls
{
   internal class DiscussionBox : Panel
   {
      internal DiscussionBox(
         CustomFontForm parent,
         SingleDiscussionAccessor accessor, IGitCommandService git,
         User currentUser, ProjectKey projectKey, Discussion discussion,
         User mergeRequestAuthor,
         int diffContextDepth, ColorScheme colorScheme,
         Action<DiscussionBox> onContentChanging,
         Action<DiscussionBox> onContentChanged,
         Action<Control> onControlGotFocus,
         HtmlToolTip htmlTooltip,
         Action onAddComment,
         Action onAddThread,
         IEnumerable<ICommand> commands,
         Action<ICommand> onCommand)
      {
         Parent = parent;

         Discussion = discussion;

         _accessor = accessor;
         _editor = accessor.GetDiscussionEditor();
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _imagePath = StringUtils.GetUploadsPrefix(projectKey);

         _diffContextDepth = new ContextDepth(0, diffContextDepth);
         _tooltipContextDepth = new ContextDepth(5, 5);
         if (git != null)
         {
            _panelContextMaker = new EnhancedContextMaker(git);
            _tooltipContextMaker = new CombinedContextMaker(git);
         }
         _colorScheme = colorScheme;

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

         _onAddComment = onAddComment;
         _onAddThread = onAddThread;
         _commands = commands;
         _onCommand = onCommand;

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
         _firstNoteAuthor = firstNote.Author;

         _labelAuthor = createLabelAuthor(firstNote);
         _textboxFilename = createTextboxFilename(firstNote);
         _panelContext = createDiffContext(firstNote);
         _textboxesNotes = createTextBoxes(Discussion.Notes);

         Controls.Add(_labelAuthor);
         Controls.Add(_textboxFilename);
         Controls.Add(_panelContext);
         foreach (Control note in _textboxesNotes)
         {
            Controls.Add(note);
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

         string html = getContext(_panelContextMaker, position, _diffContextDepth, fontSizePx, 2, true);
         htmlPanel.Text = html;

         if (htmlPanel.Visible)
         {
            resizeLimitedWidthHtmlPanel(htmlPanel, prevWidth);
         }

         string tooltipHtml = getContext(_tooltipContextMaker, position, _tooltipContextDepth, fontSizePx, 2, false);
         _htmlTooltip.SetToolTip(htmlPanel, tooltipHtml);
      }

      private string getContext(IContextMaker contextMaker, DiffPosition position, ContextDepth depth,
         double fontSizePx, int rowsVPaddingPx, bool fullWidth)
      {
         if (contextMaker == null)
         {
            return "<html><body>Cannot access file storage and render diff context</body></html>";
         }

         try
         {
            DiffContext context = contextMaker.GetContext(position, depth);
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

      private Control createTextboxFilename(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         string oldPath = firstNote.Position.Old_Path + " (line " + firstNote.Position.Old_Line + ")";
         string newPath = firstNote.Position.New_Path + " (line " + firstNote.Position.New_Line + ")";

         string result;
         if (firstNote.Position.Old_Line == null)
         {
            result = newPath;
         }
         else if (firstNote.Position.New_Line == null)
         {
            result = oldPath;
         }
         else if (firstNote.Position.Old_Path == firstNote.Position.New_Path)
         {
            result = newPath;
         }
         else
         {
            result = newPath + "\r\n(was " + oldPath + ")";
         }

         TextBox textBox = new SearchableTextBox
         {
            ReadOnly = true,
            Text = result,
            Multiline = true,
            WordWrap = false
         };
         textBox.GotFocus += Control_GotFocus;
         textBox.ContextMenu = createContextMenuForFilename(textBox);
         return textBox;
      }

      // Create a label that shows discussion author
      private Control createLabelAuthor(DiscussionNote firstNote)
      {
         Label labelAuthor = new Label
         {
            Text = firstNote.Author.Name,
            AutoEllipsis = true
         };
         return labelAuthor;
      }

      private IEnumerable<Control> createTextBoxes(IEnumerable<DiscussionNote> notes)
      {
         bool discussionResolved = notes.Cast<DiscussionNote>().All(x => (!x.Resolvable || x.Resolved));

         List<Control> boxes = new List<Control>();
         foreach (DiscussionNote note in notes)
         {
            if (note == null || note.System)
            {
               // skip spam
               continue;
            }

            Control textBox = createTextBox(note, discussionResolved);
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
         return note.Author.Id == _currentUser.Id && (!note.Resolvable || !note.Resolved);
      }

      private string addPrefix(string body, DiscussionNote note, User firstNoteAuthor)
      {
         bool appendNoteAuthor = note.Author.Id != _currentUser.Id && note.Author.Id != firstNoteAuthor.Id;
         Debug.Assert(!appendNoteAuthor || !canBeModified(note));

         string prefix = appendNoteAuthor ? String.Format("({0}) ", note.Author.Name) : String.Empty;
         return prefix + body;
      }

      private Control createTextBox(DiscussionNote note, bool discussionResolved)
      {
         if (!isServiceDiscussionNote(note))
         {
            Control noteControl = new SearchableHtmlPanel
            {
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

            return noteControl;
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

            return noteControl;
         }
      }

      private void updateStylesheet(HtmlPanel htmlPanel)
      {
         htmlPanel.BaseStylesheet = String.Format(
            "{0} body div {{ font-size: {1}px; padding-left: 4px; padding-right: {2}px; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPixels(htmlPanel),
            SystemInformation.VerticalScrollBarWidth * 2); // this is really weird
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
         noteControl.Text = String.Format(MarkDownUtils.HtmlPageTemplate, addPrefix(body, note, _firstNoteAuthor));

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
         htmlPanel.Width = width;
         int extraHeight = htmlPanel.HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0;
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + 2 + extraHeight;
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

      private MenuItem createMenuItem(ContextMenu contextMenu, object tag, string text, bool isEnabled,
         EventHandler onClick, Shortcut shortcut = Shortcut.None)
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

      MenuItem[] getCommandItems(ContextMenu contextMenu)
      {
         return _commands
            .Select(command => createMenuItem(contextMenu, null, command.GetName(), true,
               (s, e) => _onCommand?.Invoke(command)))
            .ToArray();
      }

      private ContextMenu createContextMenuForDiscussionNote(DiscussionNote note, Control noteControl,
         bool discussionResolved)
      {
         ContextMenu contextMenu = new ContextMenu();

         void addMenuItem(string text, bool isEnabled, EventHandler onClick, Shortcut shortcut = Shortcut.None) =>
            contextMenu.MenuItems.Add(createMenuItem(contextMenu, noteControl, text, isEnabled, onClick, shortcut));

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
         addMenuItem("Add a comment", true, (s, e) => _onAddComment?.Invoke());
         addMenuItem("Start a thread", true, (s, e) => _onAddThread?.Invoke());
         contextMenu.MenuItems.Add(new MenuItem("Commands", getCommandItems(contextMenu)));

         return contextMenu;
      }

      private ContextMenu createContextMenuForFilename(TextBox textBox)
      {
         ContextMenu contextMenu = new ContextMenu();

         void addMenuItem(string text, bool isEnabled, EventHandler onClick, Shortcut shortcut = Shortcut.None) =>
            contextMenu.MenuItems.Add(createMenuItem(contextMenu, textBox, text, isEnabled, onClick, shortcut));

         addMenuItem("Resolve/Unresolve Thread", isDiscussionResolvable(), onMenuItemToggleResolveDiscussion);
         addMenuItem("-", true, null);
         addMenuItem("Reply", true, onMenuItemReply);
         addMenuItem("Reply and Resolve/Unresolve Thread", true, onMenuItemReplyAndResolve, Shortcut.F4);
         addMenuItem("Reply \"Done\" and Resolve/Unresolve Thread", isDiscussionResolvable(), onMenuItemReplyDone, Shortcut.ShiftF4);
         addMenuItem("-", true, null);
         addMenuItem("Add a comment", true, (s, e) => _onAddComment?.Invoke());
         addMenuItem("Start a thread", true, (s, e) => _onAddThread?.Invoke());
         contextMenu.MenuItems.Add(new MenuItem("Commands", getCommandItems(contextMenu)));

         return contextMenu;
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
            note.Author.Name, note.Created_At.ToLocalTime().ToString(Constants.TimeStampFormat));
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
         _previousWidth = width;
      }

      private void resizeBoxContent(int width)
      {
         if (_textboxesNotes != null)
         {
            foreach (Control noteControl in _textboxesNotes)
            {
               HtmlPanel htmlPanel = noteControl as HtmlPanel;
               DiscussionNote note = getNoteFromControl(noteControl);
               if (note != null && !isServiceDiscussionNote(note))
               {
                  resizeLimitedWidthHtmlPanel(htmlPanel, width * NotesWidth / 100);
               }
               else
               {
                  resizeFullSizeHtmlPanel(htmlPanel);
               }
            }
         }

         int realLabelAuthorPercents = Convert.ToInt32(
            LabelAuthorWidth * ((Parent as CustomFontForm).CurrentFontMultiplier * LabelAuthorWidthMultiplier));

         if (_labelAuthor != null)
         {
            _labelAuthor.Width = width * realLabelAuthorPercents / 100;
         }

         if (_textboxFilename != null)
         {
            _textboxFilename.Width = width * LabelFilenameWidth / 100;
            _textboxFilename.Height = (_textboxFilename as TextBoxEx).FullPreferredHeight;
         }

         int remainingPercents = 100
            - HorzMarginWidth - realLabelAuthorPercents
            - HorzMarginWidth - NotesWidth
            - HorzMarginWidth
            - HorzMarginWidth
            - HorzMarginWidth;

         if (_panelContext != null)
         {
            resizeLimitedWidthHtmlPanel(_panelContext as HtmlPanel, width * remainingPercents / 100);
         }
      }

      private void repositionBoxContent(int width)
      {
         int interControlVertMargin = 5;
         int interControlHorzMargin = width * HorzMarginWidth / 100;

         // the LabelAuthor is placed at the left side
         Point labelPos = new Point(interControlHorzMargin, interControlVertMargin);
         if (_labelAuthor != null)
         {
            _labelAuthor.Location = labelPos;
         }

         // the Context is an optional control to the right of the Label
         int ctxX = (_labelAuthor != null ? _labelAuthor.Location.X + _labelAuthor.Width : 0) + interControlHorzMargin;
         Point ctxPos = new Point(ctxX, interControlVertMargin);
         if (_panelContext != null)
         {
            _panelContext.Location = ctxPos;
         }

         // prepare initial position for controls that places to the right of the Context
         int nextNoteX = ctxPos.X + (_panelContext != null ? _panelContext.Width + interControlHorzMargin : 0);
         Point nextNotePos = new Point(nextNoteX, ctxPos.Y);

         // the LabelFilename is placed to the right of the Context and vertically aligned with Notes
         if (_textboxFilename != null)
         {
            _textboxFilename.Location = nextNotePos;
            nextNotePos.Offset(0, _textboxFilename.Height + interControlVertMargin);
         }

         // a list of Notes is to the right of the Context
         if (_textboxesNotes != null)
         {
            foreach (Control note in _textboxesNotes)
            {
               note.Location = nextNotePos;
               nextNotePos.Offset(0, note.Height + interControlVertMargin);
            }
         }

         int lblAuthorHeight = _labelAuthor != null ? _labelAuthor.Location.Y + _labelAuthor.PreferredSize.Height : 0;
         int lblFNameHeight = _textboxFilename != null ? _textboxFilename.Location.Y + _textboxFilename.Height : 0;
         int ctxHeight = _panelContext != null ? _panelContext.Location.Y + _panelContext.Height : 0;
         int notesHeight = _textboxesNotes != null ? _textboxesNotes.Last().Location.Y + _textboxesNotes.Last().Height : 0;

         int boxContentWidth = nextNoteX + (_textboxesNotes != null ? _textboxesNotes.First().Width : 0);
         int boxContentHeight = new[] { lblAuthorHeight, lblFNameHeight, ctxHeight, notesHeight }.Max();
         Size = new Size(boxContentWidth + interControlHorzMargin, boxContentHeight + interControlVertMargin);
      }

      async private Task onEditDiscussionNoteAsync(Control noteControl)
      {
         DiscussionNote note = getNoteFromControl(noteControl);
         if (note == null || !canBeModified(note))
         {
            return;
         }

         string currentBody = StringUtils.ConvertNewlineUnixToWindows(note.Body);
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel();
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
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel(resolveText, proposeUserToToggleResolveOnReply);
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

         foreach (Control textBox in _textboxesNotes)
         {
            disableNoteControl(textBox);
         }
         disableNoteControl(_textboxFilename);
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
               if (_textboxesNotes?.Any(x => x == Controls[iControl]) ?? false)
               {
                  Controls.Remove(Controls[iControl]);
               }
            }
            _textboxesNotes = null;
            if (_textboxFilename != null)
            {
               Controls.Remove(_textboxFilename);
               _textboxFilename = null;
            }

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
         _textboxesNotes = createTextBoxes(Discussion.Notes);
         foreach (Control note in _textboxesNotes)
         {
            Controls.Add(note);
         }
         _textboxFilename = createTextboxFilename(Discussion.Notes.First());
         Controls.Add(_textboxFilename);

         // To reposition new controls and unhide me back
         _onContentChanged();
      }

      private bool isDiscussionResolved()
      {
         bool result = true;
         if (_textboxesNotes != null)
         {
            foreach (Control noteControl in _textboxesNotes)
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
      private readonly int HorzMarginWidth = 1;
      private readonly int LabelAuthorWidth = 5;
      private readonly double LabelAuthorWidthMultiplier = 1.15;
      private readonly int NotesWidth = 40;
      private readonly int LabelFilenameWidth = 40;

      private Control _labelAuthor;
      private Control _textboxFilename;
      private Control _panelContext;
      private IEnumerable<Control> _textboxesNotes;
      private int? _previousWidth;

      private readonly User _mergeRequestAuthor;
      private readonly User _currentUser;
      private readonly string _imagePath;
      private User _firstNoteAuthor; // may change on Refresh

      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly SingleDiscussionAccessor _accessor;
      private readonly IDiscussionEditor _editor;

      private readonly ColorScheme _colorScheme;

      private readonly Action _onContentChanging;
      private readonly Action _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;
      private readonly Action _onAddComment;
      private readonly Action _onAddThread;
      private readonly IEnumerable<ICommand> _commands;
      private readonly Action<ICommand> _onCommand;
      private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip _htmlTooltip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

