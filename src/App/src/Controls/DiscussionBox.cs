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
using mrHelper.Client.Discussions;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal class DiscussionBox : Panel
   {
      internal DiscussionBox(
         CustomFontForm parent,
         IDiscussionEditor editor, IGitRepository gitRepository,
         User currentUser, ProjectKey projectKey, Discussion discussion,
         User mergeRequestAuthor,
         int diffContextDepth, ColorScheme colorScheme,
         Action<DiscussionBox> preContentChange,
         Action<DiscussionBox, bool> onContentChanged,
         Action<Control> onControlGotFocus)
      {
         Parent = parent;

         Discussion = discussion;

         _editor = editor;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _imagePath = StringUtils.GetHostWithPrefix(projectKey.HostName) + "/" + projectKey.ProjectName;

         _diffContextDepth = new ContextDepth(0, diffContextDepth);
         _tooltipContextDepth = new ContextDepth(5, 5);
         if (gitRepository != null)
         {
            _panelContextMaker = new EnhancedContextMaker(gitRepository);
            _tooltipContextMaker = new CombinedContextMaker(gitRepository);
         }
         _colorScheme = colorScheme;

         _preContentChange = preContentChange;
         _onContentChanged = onContentChanged;
         _onControlGotFocus = onControlGotFocus;

         _htmlDiffContextToolTip = new HtmlToolTip
         {
            AutoPopDelay = 20000, // 20s
            InitialDelay = 150
         };

         _htmlDiscussionNoteToolTip = new HtmlToolTip
         {
            AutoPopDelay = 20000, // 20s
            InitialDelay = 150,
         };

         _specialDiscussionNoteMarkdownPipeline = MarkDownUtils.CreatePipeline();

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

      async private void DiscussionNoteHtmlPanel_KeyDown(object sender, KeyEventArgs e)
      {
         HtmlPanel htmlPanel = (HtmlPanel)(sender);

         if (e.KeyCode == Keys.F2)
         {
            await onEditDiscussionNoteAsync(htmlPanel);
         }
         else if (e.KeyCode == Keys.F4)
         {
            if (!Discussion.Individual_Note)
            {
               if (Control.ModifierKeys == Keys.Shift)
               {
                  await onReplyAsyncDone();
               }
               else if (htmlPanel.Parent?.Parent != null)
               {
                  await onReplyToDiscussionAsync();
               }
            }
         }
      }

      async private Task onEditDiscussionNoteAsync(HtmlPanel htmlPanel)
      {
         DiscussionNote note = (DiscussionNote)htmlPanel.Tag;
         if (note == null)
         {
            return;
         }

         using (NewDiscussionItemForm form = new NewDiscussionItemForm("Edit Discussion Note", note.Body))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Note text cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               await submitNewBodyAsync(htmlPanel, form.Body);
            }
         }
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
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyToDiscussionAsync();
         }
      }

      async private void MenuItemReplyDone_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent != null)
         {
            await onReplyAsyncDone();
         }
      }

      async private void MenuItemEditNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         HtmlPanel htmlPanel = (HtmlPanel)(menuItem.Tag);
         if (htmlPanel?.Parent?.Parent == null)
         {
            return;
         }

         await onEditDiscussionNoteAsync(htmlPanel);
      }

      async private void MenuItemDeleteNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         HtmlPanel htmlPanel = (HtmlPanel)(menuItem.Tag);
         if (htmlPanel?.Parent?.Parent == null)
         {
            return;
         }

         if (MessageBox.Show("This discussion note will be deleted. Are you sure?", "Confirm deletion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
         {
            return;
         }

         await onDeleteNoteAsync(getNoteFromHtmlPanel(htmlPanel));
      }

      async private void MenuItemToggleResolveNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         HtmlPanel htmlPanel = (HtmlPanel)(menuItem.Tag);
         if (htmlPanel?.Parent?.Parent == null)
         {
            return;
         }

         DiscussionNote note = getNoteFromHtmlPanel(htmlPanel);
         Debug.Assert(note == null || note.Resolvable);

         await onToggleResolveNoteAsync(note);
      }

      async private void MenuItemToggleResolveDiscussion_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         Control control = (Control)(menuItem.Tag);
         if (control?.Parent?.Parent == null)
         {
            return;
         }

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
         Debug.Assert(Discussion.Notes.Any());

         DiscussionNote firstNote = Discussion.Notes.First();

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
         DiffPosition position = PositionConverter.Convert(note.Position);
         Debug.Assert(note.Type == "DiffNote");

         double fontSizePx = WinFormsHelpers.GetFontSizeInPixels(htmlPanel);

         string html = getContext(_panelContextMaker, position,
            _diffContextDepth, fontSizePx, out string css);
         htmlPanel.BaseStylesheet = css;
         htmlPanel.Text = html;

         string tooltipHtml = getContext(_tooltipContextMaker, position,
            _tooltipContextDepth, fontSizePx, out string tooltipCSS);
         _htmlDiffContextToolTip.BaseStylesheet =
            String.Format("{0} .htmltooltip {{ padding: 1px; }}", tooltipCSS);
         _htmlDiffContextToolTip.SetToolTip(htmlPanel, tooltipHtml);
      }

      private string getContext(IContextMaker contextMaker, DiffPosition position,
         ContextDepth depth, double fontSizePx, out string stylesheet)
      {
         stylesheet = String.Empty;
         if (contextMaker == null)
         {
            return "<html><body>Cannot access git repository and render diff context</body></html>";
         }

         try
         {
            DiffContext context = contextMaker.GetContext(position, depth);
            DiffContextFormatter formatter = new DiffContextFormatter(fontSizePx, 2);
            stylesheet = formatter.GetStylesheet();
            return formatter.GetBody(context);
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
         foreach (DiscussionNote note in notes)
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
         if (note == null)
         {
            return false;
         }
         return note.Author.Id == _currentUser.Id && (!note.Resolvable || !note.Resolved);
      }

      // TODO
      private string getNoteText(DiscussionNote note, User firstNoteAuthor)
      {
         bool appendNoteAuthor = note.Author.Id != _currentUser.Id && note.Author.Id != firstNoteAuthor.Id;
         Debug.Assert(!appendNoteAuthor || !canBeModified(note));

         string prefix = appendNoteAuthor ? String.Format("({0}) ", note.Author.Name) : String.Empty;
         string body = note.Body.Replace("\n", "\r\n");
         return prefix + body;
      }

      private Control createTextBox(DiscussionNote note, bool discussionResolved, User firstNoteAuthor)
      {
         if (!isServiceDiscussionNote(note))
         {
            //TextBox textBox = new TextBoxNoWheel()
            //{
            //   ReadOnly = true,
            //   Text = getNoteText(note, firstNoteAuthor),
            //   Multiline = true,
            //   BackColor = getNoteColor(note),
            //   AutoSize = false,
            //};
            //textBox.GotFocus += Control_GotFocus;
            //textBox.LostFocus += TextBox_LostFocus;
            //textBox.KeyDown += DiscussionNoteTextBox_KeyDown;
            //textBox.KeyUp += DiscussionNoteTextBox_KeyUp;
            //textBox.ContextMenu = createContextMenuForDiscussionNote(note, discussionResolved, textBox);
            //textBox.FontChanged += (sender, e) => updateDiscussionNoteInTextBox(textBox, note);

            //updateDiscussionNoteInTextBox(textBox, note);

            HtmlPanel htmlPanel = new HtmlPanelWithGoodImages
            {
               BackColor = getNoteColor(note),
               BorderStyle = BorderStyle.FixedSingle,
               Tag = note,
               Parent = this,
               IsContextMenuEnabled = false
            };
            htmlPanel.GotFocus += Control_GotFocus;
            htmlPanel.KeyDown += DiscussionNoteHtmlPanel_KeyDown;
            htmlPanel.ContextMenu = createContextMenuForDiscussionNote(note, discussionResolved, htmlPanel);
            htmlPanel.FontChanged += (sender, e) => setDiscussionNoteHtmlText(htmlPanel);

            setDiscussionNoteHtmlText(htmlPanel);

            return htmlPanel;
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
            htmlPanel.FontChanged += (sender, e) => setServiceDiscussionNoteHtmlText(htmlPanel);

            setServiceDiscussionNoteHtmlText(htmlPanel);

            return htmlPanel;
         }
      }

      private void updateDiscussionNote(HtmlPanel htmlPanel, DiscussionNote note)
      {
         htmlPanel.Tag = note;
         if (note != null)
         {
            setDiscussionNoteHtmlText(htmlPanel);
         }
      }

      private void setDiscussionNoteHtmlText(HtmlPanel htmlPanel)
      {
         DiscussionNote note = (DiscussionNote)htmlPanel.Tag;
         if (note == null)
         {
            return;
         }

         htmlPanel.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
         Properties.Resources.Common_CSS,
         WinFormsHelpers.GetFontSizeInPixels(htmlPanel));

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath, _specialDiscussionNoteMarkdownPipeline);
         htmlPanel.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         htmlPanel.PerformLayout();

         _htmlDiscussionNoteToolTip.BaseStylesheet =
            String.Format("{0} body div {{ font-size: {1}px; }}",
               Properties.Resources.Common_CSS,
               WinFormsHelpers.GetFontSizeInPixels(htmlPanel));
         _htmlDiscussionNoteToolTip.SetToolTip(htmlPanel, getNoteTooltipHtml(note));
      }

      private void setServiceDiscussionNoteHtmlText(HtmlPanel htmlPanel)
      {
         DiscussionNote note = (DiscussionNote)htmlPanel.Tag;

         // We need to zero the control size before SetText call to allow HtmlPanel to compute the size
         htmlPanel.Width = 0;
         htmlPanel.Height = 0;

         htmlPanel.BaseStylesheet = String.Format("{0} body div {{ font-size: {1}px; }}",
            Properties.Resources.Common_CSS,
            WinFormsHelpers.GetFontSizeInPixels(htmlPanel));

         string body = MarkDownUtils.ConvertToHtml(note.Body, _imagePath, _specialDiscussionNoteMarkdownPipeline);
         htmlPanel.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         htmlPanel.PerformLayout();

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
         bool discussionResolved, HtmlPanel htmlPanel)
      {
         ContextMenu contextMenu = new ContextMenu();

         MenuItem menuItemToggleDiscussionResolve = new MenuItem
         {
            Tag = htmlPanel,
            Text = (discussionResolved ? "Unresolve" : "Resolve") + " Thread",
            Enabled = note.Resolvable
         };
         menuItemToggleDiscussionResolve.Click += MenuItemToggleResolveDiscussion_Click;
         contextMenu.MenuItems.Add(menuItemToggleDiscussionResolve);

         MenuItem menuItemToggleResolve = new MenuItem
         {
            Tag = htmlPanel,
            Text = (note.Resolvable && note.Resolved ? "Unresolve" : "Resolve") + " Note",
            Enabled = note.Resolvable
         };
         menuItemToggleResolve.Click += MenuItemToggleResolveNote_Click;
         contextMenu.MenuItems.Add(menuItemToggleResolve);

         MenuItem menuItemDeleteNote = new MenuItem
         {
            Tag = htmlPanel,
            Enabled = canBeModified(note),
            Text = "Delete Note"
         };
         menuItemDeleteNote.Click += MenuItemDeleteNote_Click;
         contextMenu.MenuItems.Add(menuItemDeleteNote);

         MenuItem menuItemEditNote = new MenuItem
         {
            Tag = htmlPanel,
            Enabled = canBeModified(note),
            Text = "Edit Note\t(F2)"
         };
         menuItemEditNote.Click += MenuItemEditNote_Click;
         contextMenu.MenuItems.Add(menuItemEditNote);

         MenuItem menuItemReply = new MenuItem
         {
            Tag = htmlPanel,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply\t(F4)"
         };
         menuItemReply.Click += MenuItemReply_Click;
         contextMenu.MenuItems.Add(menuItemReply);

         MenuItem menuItemReplyDone = new MenuItem
         {
            Tag = htmlPanel,
            Enabled = !Discussion.Individual_Note,
            Text = "Reply \"Done\" and " + (discussionResolved ? "Unresolve" : "Resolve") + " Thread" + "\t(Shift-F4)"
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
            Text = "Resolve/Unresolve Thread",
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
            Text = "Reply \"Done\" and Resolve/Unresolve Thread" + "\t(Shift-F4)"
         };
         menuItemReplyDone.Click += MenuItemReplyDone_Click;
         contextMenu.MenuItems.Add(menuItemReplyDone);

         return contextMenu;
      }

      private string getNoteTooltipHtml(DiscussionNote note)
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();
         if (note.Resolvable)
         {
            string text = note.Resolved ? "Resolved." : "Not resolved.";
            string color = note.Resolved ? "green" : "red";
            result.AppendFormat("<i style=\"color: {0}\">{1}&nbsp;&nbsp;&nbsp;</i>", color, text);
         }
         result.AppendFormat("Created by <b> {0} </b> at <span style=\"color: blue\">{1}</span>",
            note.Author.Name, note.Created_At.ToLocalTime().ToString(Constants.TimeStampFormat));
         return result.ToString();
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
               if (textbox.Tag is DiscussionNote note)
               {
                  if (!isServiceDiscussionNote(note))
                  {
                     textbox.Width = width * NotesWidth / 100;
                     textbox.Height = (textbox as HtmlPanel).AutoScrollMinSize.Height + 2;
                  }
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
            _htmlDiffContextToolTip.MaximumSize = new Size(_panelContext.Width, 0 /* auto-height */);
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
         await onReplyAsync("Done", true);
      }

      async private Task onReplyAsync(string body, bool toggleResolve = false)
      {
         bool wasResolved = isDiscussionResolved();
         disableAllTextBoxes();

         try
         {
            if (toggleResolve)
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
            return;
         }

         await refreshDiscussion();
      }

      async private Task submitNewBodyAsync(HtmlPanel htmlPanel, string newText)
      {
         DiscussionNote cachedNote = getNoteFromHtmlPanel(htmlPanel);
         if (cachedNote == null || newText == cachedNote.Body)
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

         Color oldColor = htmlPanel.BackColor;
         ContextMenu oldMenu = htmlPanel.ContextMenu;
         disableHtmlPanel(htmlPanel); // let's make a visual effect similar to other modifications

         DiscussionNote note;
         try
         {
            note = await _editor.ModifyNoteBodyAsync(cachedNote.Id, newText);
         }
         catch (DiscussionEditorException ex)
         {
            string message = "Cannot update discussion text";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         _onContentChanged(this, true);

         if (!htmlPanel.IsDisposed)
         {
            htmlPanel.BackColor = oldColor;
            htmlPanel.ContextMenu = oldMenu;
            updateDiscussionNote(htmlPanel, note);
         }
      }

      async private Task onDeleteNoteAsync(DiscussionNote note)
      {
         if (note == null)
         {
            return;
         }

         disableAllTextBoxes();

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
         if (note == null)
         {
            return;
         }

         disableAllTextBoxes();

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
            return;
         }

         await refreshDiscussion();
      }

      async private Task onToggleResolveDiscussionAsync()
      {
         bool wasResolved = isDiscussionResolved();
         disableAllTextBoxes();

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
            return;
         }

         await refreshDiscussion(discussion);
      }

      private void disableTextBox(TextBox textBox)
      {
         if (textBox != null)
         {
            textBox.BackColor = Color.LightGray;
            textBox.ContextMenu = new ContextMenu();
         }
      }

      private void disableHtmlPanel(HtmlPanel htmlPanel)
      {
         if (htmlPanel != null)
         {
            htmlPanel.BackColor = Color.LightGray;
            htmlPanel.ContextMenu = new ContextMenu();
            updateDiscussionNote(htmlPanel, null);
         }
      }

      private void disableAllTextBoxes()
      {
         foreach (Control textBox in _textboxesNotes)
         {
            disableHtmlPanel(textBox as HtmlPanel);
         }
         disableTextBox(_textboxFilename as TextBox);
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
            // So if we hide text boxes in _preContentChanged() and process WM_MOUSEUP in `await` below we're in a trouble.
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
            _preContentChange(this);
         }

         // Load updated discussion
         try
         {
            Discussion = discussion ?? await _editor.GetDiscussion();
         }
         catch (DiscussionEditorException ex)
         {
            ExceptionHandlers.Handle("Not an error - last discussion item has been deleted", ex);

            // it is not an error here, we treat it as 'last discussion item has been deleted'
            // Seems it was the only note in the discussion, remove ourselves from parents controls
            prepareToRefresh();
            Parent?.Controls.Remove(this);
            _onContentChanged(this, false);
            return;
         }

         if (Discussion.Notes.Count() == 0 || Discussion.Notes.First().System)
         {
            // It happens when Discussion has System notes like 'a line changed ...'
            // along with a user note that has been just deleted
            prepareToRefresh();
            Parent?.Controls.Remove(this);
            _onContentChanged(this, false);
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
         _onContentChanged(this, false);
      }

      private bool isDiscussionResolved()
      {
         bool result = true;
         if (_textboxesNotes != null)
         {
            foreach (Control htmlPanel in _textboxesNotes)
            {
               if (htmlPanel != null)
               {
                  DiscussionNote note = getNoteFromHtmlPanel(htmlPanel as HtmlPanel);
                  if (note != null && note.Resolvable && !note.Resolved)
                  {
                     result = false;
                  }
               }
            }
         }
         return result;
      }

      // TODO Check if needed for HtmlPanel textboxes
      /// <summary>
      /// The only purpose of this class is to disable async image loading.
      /// Given feature prevents showing full-size images because their size are unknown
      /// at the moment of tooltip rendering.
      /// </summary>
      internal class HtmlToolTipWithGoodImages : HtmlToolTip
      {
         internal HtmlToolTipWithGoodImages()
            : base()
         {
            this._htmlContainer.AvoidAsyncImagesLoading = true;
         }
      }

      // TODO Check if needed for HtmlPanel textboxes
      /// <summary>
      /// The only purpose of this class is to disable async image loading.
      /// Given feature prevents showing full-size images because their size are unknown
      /// at the moment of tooltip rendering.
      /// </summary>
      internal class HtmlPanelWithGoodImages : HtmlPanel
      {
         internal HtmlPanelWithGoodImages()
            : base()
         {
            this._htmlContainer.AvoidAsyncImagesLoading = true;
         }
      }

      private DiscussionNote getNoteFromHtmlPanel(HtmlPanel htmlPanel)
      {
         return (htmlPanel == null || htmlPanel.Tag == null) ? null : (DiscussionNote)(htmlPanel.Tag);
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

      private readonly User _mergeRequestAuthor;
      private readonly User _currentUser;
      private readonly string _imagePath;

      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly IDiscussionEditor _editor;

      private readonly ColorScheme _colorScheme;

      private readonly Action<DiscussionBox> _preContentChange;
      private readonly Action<DiscussionBox, bool> _onContentChanged;
      private readonly Action<Control> _onControlGotFocus;

      private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip _htmlDiffContextToolTip;
      private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip _htmlDiscussionNoteToolTip;
      private readonly Markdig.MarkdownPipeline _specialDiscussionNoteMarkdownPipeline;
   }
}

