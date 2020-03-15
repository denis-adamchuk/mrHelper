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
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Discussions;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   internal class DiscussionBox : Panel
   {
      internal DiscussionBox(Discussion discussion, DiscussionEditor editor, User mergeRequestAuthor, User currentUser,
         int diffContextDepth, IGitRepository gitRepository, ColorScheme colorScheme,
         Action<DiscussionBox> preContentChange, Action<DiscussionBox, bool> onContentChanged,
         Action<Control> onControlGotFocus, CustomFontForm parent)
      {
         Parent = parent;

         Discussion = discussion;

         _editor = editor;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;

         _diffContextDepth = new ContextDepth(0, diffContextDepth);
         _tooltipContextDepth = new ContextDepth(5, 5);
         _formatter = new DiffContextFormatter();
         if (gitRepository != null)
         {
            _panelContextMaker = new EnhancedContextMaker(gitRepository);
            _tooltipContextMaker = new CombinedContextMaker(gitRepository);
         }
         _gitRepository = gitRepository;
         _colorScheme = colorScheme;

         _preContentChange = preContentChange;
         _onContentChanged = onContentChanged;
         _onControlGotFocus = onControlGotFocus;

         _toolTip = new ToolTip
         {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 100
         };

         _toolTipNotifier = new ToolTip();

         _htmlToolTip = new HtmlToolTip
         {
            AutoPopDelay = 10000, // 10s
            BaseStylesheet = ".htmltooltip { padding: 1px; }"
         };

         Markdig.Extensions.Tables.PipeTableOptions options = new Markdig.Extensions.Tables.PipeTableOptions
         {
            RequireHeaderSeparator = false
         };
         _specialDiscussionNoteMarkdownPipeline = Markdig.MarkdownExtensions
            .UsePipeTables(new Markdig.MarkdownPipelineBuilder(), options)
            .Build();

         onCreate();
      }

      internal Discussion Discussion { get; private set; }

      async private void FilenameTextBox_KeyDown(object sender, KeyEventArgs e)
      {
         TextBox textBox = (TextBox)(sender);

         if (e.KeyCode == Keys.F4)
         {
            if (!Discussion.Individual_Note)
            {
               if (Control.ModifierKeys == Keys.Shift)
               {
                  await onReplyAsyncDone();
               }
               else if (textBox?.Parent?.Parent != null)
               {
                  await onReplyToDiscussionAsync();
               }
            }
         }
      }

      async private void DiscussionNoteTextBox_KeyDown(object sender, KeyEventArgs e)
      {
         TextBox textBox = (TextBox)(sender);

         if (e.KeyCode == Keys.F2 && textBox.ReadOnly)
         {
            DiscussionNote note = (DiscussionNote)(textBox.Tag);
            if (canBeModified(note))
            {
               onStartEditNote(textBox);
            }
         }
         else if (e.KeyCode == Keys.Enter && !textBox.ReadOnly)
         {
            if (Control.ModifierKeys == Keys.Control)
            {
               await onSubmitNewBodyAsync(textBox);
            }
         }
         else if (e.KeyCode == Keys.F4)
         {
            if (!Discussion.Individual_Note)
            {
               if (!textBox.ReadOnly)
               {
                  onCancelEditNote(textBox);
                  updateTextboxHeight(textBox);
               }
               if (Control.ModifierKeys == Keys.Shift)
               {
                  await onReplyAsyncDone();
               }
               else if (textBox.Parent?.Parent != null)
               {
                  await onReplyToDiscussionAsync();
               }
            }
         }
         else if (e.KeyCode == Keys.Escape && !textBox.ReadOnly)
         {
            onCancelEditNote(textBox);
            updateTextboxHeight(textBox);
         }
      }

      private void DiscussionNoteTextBox_KeyUp(object sender, KeyEventArgs e)
      {
         TextBox textBox = (TextBox)(sender);

         if (!textBox.ReadOnly)
         {
            updateTextboxHeight(textBox);
         }
      }

      private void updateTextboxHeight(Control textBox)
      {
         int newHeight = (textBox as TextBoxNoWheel).PreferredHeight;
         if (newHeight != textBox.Height)
         {
            textBox.Height = newHeight;
            _onContentChanged(this, true);
         }
      }

      async private void TextBox_LostFocus(object sender, EventArgs e)
      {
         TextBox textBox = (TextBox)(sender);
         if (textBox.ReadOnly)
         {
            return;
         }

         await onSubmitNewBodyAsync(textBox);
      }

      async private Task onReplyToDiscussionAsync()
      {
         using (NewDiscussionItemForm form = new NewDiscussionItemForm("Reply to Discussion"))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Reply text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               await onReplyAsync(form.Body);
            }
         }
      }

      async private void MenuItemReply_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent != null)
         {
            await onReplyToDiscussionAsync();
         }
      }

      async private void MenuItemReplyDone_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent != null)
         {
            await onReplyAsyncDone();
         }
      }

      private void MenuItemEditNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent == null)
         {
            return;
         }

         onStartEditNote(textBox);
      }

      async private void MenuItemDeleteNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent == null)
         {
            return;
         }

         stopEdit(textBox); // prevent submitting body modifications in the current handler

         if (MessageBox.Show("This discussion note will be deleted. Are you sure?", "Confirm deletion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
         {
            return;
         }

         await onDeleteNoteAsync(textBox);
      }

      async private void MenuItemToggleResolveNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent == null)
         {
            return;
         }

         stopEdit(textBox); // prevent submitting body modifications in the current handler

         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         Debug.Assert(note.Resolvable);

         await onToggleResolveNoteAsync((DiscussionNote)textBox.Tag);
      }

      async private void MenuItemToggleResolveDiscussion_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (textBox?.Parent?.Parent == null)
         {
            return;
         }

         stopEdit(textBox); // prevent submitting body modifications in the current handler

         await onToggleResolveDiscussionAsync();
      }

      internal Size AdjustToWidth(int width)
      {
         resizeBoxContent(width);
         repositionBoxContent(width);
         return Size;
      }

      private void onCreate()
      {
         Debug.Assert(Discussion.Notes.Count() > 0);

         DiscussionNote firstNote = Discussion.Notes.First();

         _labelAuthor = createLabelAuthor(firstNote);
         _textboxFilename = createTextboxFilename(firstNote);
         _panelContext = createDiffContext(firstNote);
         _textboxesNotes = createTextBoxes(Discussion.Notes).ToArray();

         Controls.Add(_labelAuthor);
         Controls.Add(_textboxFilename);
         Controls.Add(_panelContext);
         foreach (Control note in _textboxesNotes)
         {
            Controls.Add(note);
         }
      }

      private HtmlPanel createDiffContext(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         HtmlPanel htmlPanel = new HtmlPanel
         {
            BorderStyle = BorderStyle.FixedSingle,
            TabStop = false,
            Tag = firstNote,
            Parent = this
         };
         htmlPanel.GotFocus += Control_GotFocus;
         htmlPanel.FontChanged += (sender, e) => setDiffContextText(sender as HtmlPanel);

         setDiffContextText(htmlPanel);

         return htmlPanel;
      }

      private void setDiffContextText(HtmlPanel htmlPanel)
      {
         DiscussionNote note = (DiscussionNote)htmlPanel.Tag;
         Debug.Assert(note.Type == "DiffNote");

         DiffPosition position = convertToDiffPosition(note.Position);

         htmlPanel.Text = "Loading...\n\n\n";
         _htmlToolTip.SetToolTip(htmlPanel, "Loading...");

         if (IsHandleCreated)
         {
            HtmlPanel_HandleCreated(htmlPanel, position);
         }
         else
         {
            HandleCreated += (s, e) => HtmlPanel_HandleCreated(htmlPanel, position);
         }
         //BeginInvoke(new Action(
         //   async () =>
         //{
         //   htmlPanel.Text = await getContext(_panelContextMaker, position,
         //      _diffContextDepth, htmlPanel.Font.Height);
         //   _htmlToolTip.SetToolTip(htmlPanel, await getContext(_tooltipContextMaker, position,
         //      _tooltipContextDepth, htmlPanel.Font.Height));
         //}), null);
      }

      private void HtmlPanel_HandleCreated(HtmlPanel htmlPanel, DiffPosition position)
      {
         BeginInvoke(new Action(
            async () =>
         {
            htmlPanel.Text = await getContext(_panelContextMaker, position,
               _diffContextDepth, htmlPanel.Font.Height);
            _htmlToolTip.SetToolTip(htmlPanel, await getContext(_tooltipContextMaker, position,
               _tooltipContextDepth, htmlPanel.Font.Height));
         }), null);
      }

      async private Task<string> getContext(IContextMaker contextMaker, DiffPosition position,
         ContextDepth depth, int fontSizePx)
      {
         if (contextMaker == null || _formatter == null)
         {
            return "<html><body>Cannot access git repository and render diff context</body></html>";
         }

         try
         {
            DiffContext context = await contextMaker.GetContext(position, depth);
            return _formatter.FormatAsHTML(context, fontSizePx);
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

      // Create a textbox that shows filename
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

         TextBox textBox = new TextBoxNoWheel
         {
            ReadOnly = true,
            Text = result,
            Multiline = true
         };
         textBox.GotFocus += Control_GotFocus;
         textBox.KeyDown += FilenameTextBox_KeyDown;
         textBox.ContextMenu = createContextMenuForFilename(firstNote, textBox);
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
         foreach (var note in notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            Control textBox = createTextBox(note, discussionResolved, notes.First().Author);
            boxes.Add(textBox);
         }
         return boxes;
      }

      private bool canBeModified(DiscussionNote note)
      {
         return note.Author.Id == _currentUser.Id && (!note.Resolvable || !note.Resolved);
      }

      private string getNoteText(DiscussionNote note, User firstNoteAuthor)
      {
         bool appendNoteAuthor = note.Author.Id != _currentUser.Id && note.Author.Id != firstNoteAuthor.Id;
         Debug.Assert(!appendNoteAuthor || !canBeModified(note));

         string prefix = appendNoteAuthor ? String.Format("({0}) ", note.Author.Name) : String.Empty;
         string body = note.Body.Replace("\n", "\r\n");
         return prefix + body;
      }

      private string getHtmlDiscussionNoteText(ref DiscussionNote note, int fontSizePx)
      {
         string css = mrHelper.App.Properties.Resources.DiscussionNoteCSS;
         css += String.Format("body div {{ font-size: {0}px; }}", fontSizePx);

         string commonBegin = string.Format(@"
            <html>
               <head>
                  <style>{0}</style>
               </head>
               <body>
                  <div>", css);

         string commonEnd = @"
                  </div>
               </body>
            </html>";

         string htmlbody =
            System.Net.WebUtility.HtmlDecode(
               Markdig.Markdown.ToHtml(
                  System.Net.WebUtility.HtmlEncode(note.Body), _specialDiscussionNoteMarkdownPipeline));

         return commonBegin + htmlbody + commonEnd;
      }

      private Control createTextBox(DiscussionNote note, bool discussionResolved, User firstNoteAuthor)
      {
         if (!isServiceDiscussionNote(note))
         {
            TextBox textBox = new TextBoxNoWheel()
            {
               ReadOnly = true,
               Text = getNoteText(note, firstNoteAuthor),
               Multiline = true,
               BackColor = getNoteColor(note),
               Tag = note,
               AutoSize = false,
            };
            textBox.GotFocus += Control_GotFocus;
            textBox.LostFocus += TextBox_LostFocus;
            textBox.KeyDown += DiscussionNoteTextBox_KeyDown;
            textBox.KeyUp += DiscussionNoteTextBox_KeyUp;
            textBox.ContextMenu = createContextMenuForDiscussionNote(note, discussionResolved, textBox);
            _toolTip.SetToolTip(textBox, getNoteTooltipText(note));

            return textBox;
         }
         else
         {
            HtmlPanel htmlPanel = new HtmlPanel
            {
               BackColor = getNoteColor(note),
               BorderStyle = BorderStyle.FixedSingle,
               Tag = note,
               Parent = this
            };
            htmlPanel.GotFocus += Control_GotFocus;
            htmlPanel.FontChanged += (sender, e) => setNoteHtmlText(htmlPanel);

            setNoteHtmlText(htmlPanel);

            return htmlPanel;
         }
      }

      private void setNoteHtmlText(HtmlPanel htmlPanel)
      {
         DiscussionNote note = (DiscussionNote)htmlPanel.Tag;

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         htmlPanel.Width = 0;
         htmlPanel.Height = 0;

         htmlPanel.Text = getHtmlDiscussionNoteText(ref note, htmlPanel.Font.Height);

         // Use computed size as the control size. Height must be set BEFORE Width.
         htmlPanel.Height = htmlPanel.AutoScrollMinSize.Height + 2;
         htmlPanel.Width = htmlPanel.AutoScrollMinSize.Width + 2;
      }

      private bool isServiceDiscussionNote(DiscussionNote note)
      {
         return note.Author.Username == Program.ServiceManager.GetServiceMessageUsername();
      }

      private void Control_GotFocus(object sender, EventArgs e)
      {
         _onControlGotFocus(sender as Control);
      }

      private ContextMenu createContextMenuForDiscussionNote(DiscussionNote note,
         bool discussionResolved, TextBox textBox)
      {
         var contextMenu = new ContextMenu();

         MenuItem menuItemToggleDiscussionResolve = new MenuItem
         {
            Tag = textBox,
            Text = (discussionResolved ? "Unresolve" : "Resolve") + " Discussion",
            Enabled = note.Resolvable
         };
         menuItemToggleDiscussionResolve.Click += MenuItemToggleResolveDiscussion_Click;
         contextMenu.MenuItems.Add(menuItemToggleDiscussionResolve);

         MenuItem menuItemToggleResolve = new MenuItem
         {
            Tag = textBox,
            Text = (note.Resolvable && note.Resolved ? "Unresolve" : "Resolve") + " Note",
            Enabled = note.Resolvable
         };
         menuItemToggleResolve.Click += MenuItemToggleResolveNote_Click;
         contextMenu.MenuItems.Add(menuItemToggleResolve);

         MenuItem menuItemDeleteNote = new MenuItem
         {
            Tag = textBox,
            Enabled = canBeModified(note),
            Text = "Delete Note"
         };
         menuItemDeleteNote.Click += MenuItemDeleteNote_Click;
         contextMenu.MenuItems.Add(menuItemDeleteNote);

         MenuItem menuItemEditNote = new MenuItem
         {
            Tag = textBox,
            Enabled = canBeModified(note),
            Text = "Edit Note\t(F2)"
         };
         menuItemEditNote.Click += MenuItemEditNote_Click;
         contextMenu.MenuItems.Add(menuItemEditNote);

         MenuItem menuItemReply = new MenuItem
         {
            Tag = textBox,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply\t(F4)"
         };
         menuItemReply.Click += MenuItemReply_Click;
         contextMenu.MenuItems.Add(menuItemReply);

         MenuItem menuItemReplyDone = new MenuItem
         {
            Tag = textBox,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply \"Done\"\t(Shift-F4)"
         };
         menuItemReplyDone.Click += MenuItemReplyDone_Click;
         contextMenu.MenuItems.Add(menuItemReplyDone);

         return contextMenu;
      }

      private ContextMenu createContextMenuForFilename(DiscussionNote firstNote, TextBox textBox)
      {
         var contextMenu = new ContextMenu();

         MenuItem menuItemToggleDiscussionResolve = new MenuItem
         {
            Tag = textBox,
            Text = "Resolve/Unresolve Discussion",
            Enabled = firstNote.Resolvable
         };
         menuItemToggleDiscussionResolve.Click += MenuItemToggleResolveDiscussion_Click;
         contextMenu.MenuItems.Add(menuItemToggleDiscussionResolve);

         MenuItem menuItemReply = new MenuItem
         {
            Tag = textBox,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply\t(F4)"
         };
         menuItemReply.Click += MenuItemReply_Click;
         contextMenu.MenuItems.Add(menuItemReply);

         MenuItem menuItemReplyDone = new MenuItem
         {
            Tag = textBox,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply \"Done\"\t(Shift-F4)"
         };
         menuItemReplyDone.Click += MenuItemReplyDone_Click;
         contextMenu.MenuItems.Add(menuItemReplyDone);

         return contextMenu;
      }

      private string getNoteTooltipText(DiscussionNote note)
      {
         string result = string.Empty;
         if (note.Resolvable)
         {
            result += note.Resolved ? "Resolved." : "Not resolved.";
         }
         result += " Created by " + note.Author.Name + " at " + note.Created_At.ToLocalTime().ToString("g");
         return result;
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

      private void resizeBoxContent(int width)
      {
         if (_textboxesNotes != null)
         {
            foreach (Control textbox in _textboxesNotes)
            {
               if (textbox is TextBoxNoWheel)
               {
                  textbox.Width = width * NotesWidth / 100;
                  textbox.Height = (textbox as TextBoxNoWheel).PreferredHeight;
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
            _textboxFilename.Height = (_textboxFilename as TextBoxNoWheel).PreferredHeight;
         }

         int remainingPercents = 100
            - HorzMarginWidth - realLabelAuthorPercents
            - HorzMarginWidth - NotesWidth
            - HorzMarginWidth
            - HorzMarginWidth
            - HorzMarginWidth;

         if (_panelContext != null)
         {
            _panelContext.Width = width * remainingPercents / 100;
            _panelContext.Height = (_panelContext as HtmlPanel).AutoScrollMinSize.Height + 2;
            _htmlToolTip.MaximumSize = new Size(_panelContext.Width, 0 /* auto-height */);
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

      async private Task onReplyAsyncDone()
      {
         await onReplyAsync("Done");
      }

      async private Task onReplyAsync(string body)
      {
         try
         {
            await _editor.ReplyAsync(body);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot create a reply to discussion";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         await refreshDiscussion();
      }

      private void onStartEditNote(TextBox textBox)
      {
         textBox.ReadOnly = false;
         textBox.BackColor = Color.White;
         textBox.Focus();
      }

      private void stopEdit(TextBox textBox)
      {
         textBox.ReadOnly = true;
         if (_textboxesNotes.Contains(textBox))
         {
            textBox.BackColor = getNoteColor((DiscussionNote)textBox.Tag);
         }
      }

      private void onCancelEditNote(TextBox textBox)
      {
         stopEdit(textBox);

         DiscussionNote note = (DiscussionNote)(textBox.Tag);

         Debug.Assert(Discussion.Notes.Count() > 0);
         textBox.Text = getNoteText(note, Discussion.Notes.First().Author);
      }

      async private Task onSubmitNewBodyAsync(TextBox textBox)
      {
         stopEdit(textBox);

         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         if (textBox.Text == note.Body)
         {
            return;
         }

         if (textBox.Text.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            onCancelEditNote(textBox);
            return;
         }

         note.Body = textBox.Text;

         try
         {
            note = await _editor.ModifyNoteBodyAsync(note.Id, note.Body);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot update discussion text";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         _onContentChanged(this, true);

         if (!textBox.IsDisposed)
         {
            textBox.Tag = note;
            _toolTipNotifier.Show("Discussion note was edited", textBox, textBox.Width + 20, 0, 2000 /* ms */);
         }
      }

      async private Task onDeleteNoteAsync(TextBox textBox)
      {
         DiscussionNote note = (DiscussionNote)(textBox.Tag);

         try
         {
            await _editor.DeleteNoteAsync(note.Id);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot delete a note";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         await refreshDiscussion();
      }

      async private Task onToggleResolveNoteAsync(DiscussionNote note)
      {
         bool wasResolved = note.Resolved;

         try
         {
            await _editor.ResolveNoteAsync(note.Id, !wasResolved);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot toggle 'Resolved' state of a note";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         await refreshDiscussion();
      }

      async private Task onToggleResolveDiscussionAsync()
      {
         bool wasResolved = isDiscussionResolved();

         Discussion discussion;
         try
         {
            discussion = await _editor.ResolveDiscussionAsync(!wasResolved);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot toggle 'Resolved' state of a discussion";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         await refreshDiscussion(discussion);
      }

      async private Task refreshDiscussion(Discussion? discussion = null)
      {
         if (Parent == null)
         {
            return;
         }

         void removeTextBoxes()
         {
            for (int iControl = Controls.Count - 1; iControl >= 0; --iControl)
            {
               if (_textboxesNotes.Any(x => x == Controls[iControl]))
               {
                  Controls.Remove(Controls[iControl]);
               }
            }
            _textboxesNotes = null;
         }

         _preContentChange(this);

         // Load updated discussion
         try
         {
            Discussion = discussion ?? await _editor.GetDiscussion();
         }
         catch (DiscussionEditorException ex)
         {
            ExceptionHandlers.Handle("Not an error - last discussion item has bee deleted", ex);

            removeTextBoxes();
            // it is not an error here, we treat it as 'last discussion item has been deleted'
            // Seems it was the only note in the discussion, remove ourselves from parents controls
            Parent.Controls.Remove(this);
            _onContentChanged(this, false);
            return;
         }

         // Get rid of old text boxes
         removeTextBoxes();

         if (Discussion.Notes.Count() == 0 || Discussion.Notes.First().System)
         {
            // It happens when Discussion has System notes like 'a line changed ...'
            // along with a user note that has been just deleted
            Parent.Controls.Remove(this);
            _onContentChanged(this, false);
            return;
         }

         // Create controls
         _textboxesNotes = createTextBoxes(Discussion.Notes).ToArray();
         foreach (Control note in _textboxesNotes)
         {
            Controls.Add(note);
         }

         // To reposition new controls
         _onContentChanged(this, false);
      }

      private bool isDiscussionResolved()
      {
         bool result = true;
         foreach (Control textBox in _textboxesNotes)
         {
            DiscussionNote note = (DiscussionNote)(textBox.Tag);
            if (note.Resolvable && !note.Resolved)
            {
               result = false;
            }
         }
         return result;
      }

      private DiffPosition convertToDiffPosition(Position position)
      {
         return new DiffPosition
         {
            LeftLine = position.Old_Line,
            LeftPath = position.Old_Path,
            RightLine = position.New_Line,
            RightPath = position.New_Path,
            Refs = new mrHelper.Core.Matching.DiffRefs
            {
               LeftSHA = GitTools.AdjustSHA(position.Base_SHA, _gitRepository),
               RightSHA = GitTools.AdjustSHA(position.Head_SHA, _gitRepository)
            }
         };
      }

      // Widths in %
      private readonly int HorzMarginWidth = 1;
      private readonly int LabelAuthorWidth = 5;
      private readonly double LabelAuthorWidthMultiplier = 1.15;
      private readonly int NotesWidth = 34;
      private readonly int LabelFilenameWidth = 34;

      private Control _labelAuthor;
      private Control _textboxFilename;
      private Control _panelContext;
      private IEnumerable<Control> _textboxesNotes;

      private readonly User _mergeRequestAuthor;
      private readonly User _currentUser;

      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly DiffContextFormatter _formatter;
      private readonly DiscussionEditor _editor;

      private readonly ColorScheme _colorScheme;

      private readonly IGitRepository _gitRepository;

      private readonly Action<DiscussionBox> _preContentChange;
      private readonly Action<DiscussionBox, bool> _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;

      private readonly System.Windows.Forms.ToolTip _toolTip;
      private readonly System.Windows.Forms.ToolTip _toolTipNotifier;
      private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip _htmlToolTip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}
