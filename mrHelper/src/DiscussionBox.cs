using GitLabSharp;
using mrCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelperUI
{
   public struct MergeRequestDetails
   {
      public string Host;
      public string AccessToken;
      public string ProjectId;
      public int MergeRequestIId;
      public User Author;
      public User CurrentUser;
   }

   internal class DiscussionBox : Panel
   {
      private const int EM_GETLINECOUNT = 0xba;
      [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      private static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam);

      public delegate void OnBoxEvent();

      public DiscussionBox(Discussion discussion, MergeRequestDetails mergeRequestDetails, User currentUser,
         int diffContextDepth, GitRepository gitRepository, ColorScheme colorScheme,  OnBoxEvent onSizeChanged)
      {
         _mergeRequestDetails = mergeRequestDetails;
         _currentUser = currentUser;

         GitLab gl = new GitLab(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
         _mergeRequestAccessor = gl.Projects.Get(_mergeRequestDetails.ProjectId).MergeRequests.
            Get(_mergeRequestDetails.MergeRequestIId);

         _diffContextDepth = new ContextDepth(0, diffContextDepth);
         _tooltipContextDepth = new ContextDepth(5, 5);
         _formatter = new DiffContextFormatter();
         if (gitRepository != null)
         {
            _panelContextMaker = new EnhancedContextMaker(gitRepository);
            _tooltipContextMaker = new CombinedContextMaker(gitRepository);
         }
         _colorScheme = colorScheme;

         _onSizeChanged = onSizeChanged;

         _toolTip = new ToolTip();
         _toolTip.AutoPopDelay = 5000;
         _toolTip.InitialDelay = 500;
         _toolTip.ReshowDelay = 100;

         _toolTipNotifier = new ToolTip();

         _htmlToolTip = new HtmlToolTip();
         _htmlToolTip.AutoPopDelay = 10000; // 10s
         _htmlToolTip.BaseStylesheet = ".htmltooltip { padding: 1px; }";

         onCreate(discussion);
      }

      private void TextBox_KeyDown(object sender, KeyEventArgs e)
      {
         TextBox textBox = (TextBox)(sender);

         if (textBox.ReadOnly && e.KeyData == Keys.F2)
         {
            DiscussionNote note = (DiscussionNote)(textBox.Tag);
            if (canBeModified(note))
            {
               onStartEditNote(textBox);
            }
         }
         else if (!textBox.ReadOnly && e.KeyData == Keys.Escape)
         {
            onCancelEditNote(textBox);
         }
      }

      private void TextBox_KeyUp(object sender, KeyEventArgs e)
      {
         TextBox textBox = (TextBox)(sender);

         if (!textBox.ReadOnly && e.KeyData == Keys.Enter)
         {
            onNewLineAddedToNote(textBox);
         }
      }

      private void onNewLineAddedToNote(TextBox textBox)
      {
         int newHeight = getTextBoxPreferredHeight(textBox);
         if (newHeight > textBox.Height)
         {
            textBox.Height = newHeight;
            _onSizeChanged();
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

      async private void MenuItemReply_Click(object sender, EventArgs e)
      {
         NewDiscussionItemForm form = new NewDiscussionItemForm();
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

      private void MenuItemEditNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         onStartEditNote(textBox);
      }

      async private void MenuItemDeleteNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         textBox.ReadOnly = true; // prevent submitting body modifications in the current handler

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
         textBox.ReadOnly = true; // prevent submitting body modifications in the current handler

         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         Debug.Assert(note.Resolvable);

         await onToggleResolveNoteAsync(textBox);
      }

      async private void MenuItemToggleResolveDiscussion_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         textBox.ReadOnly = true; // prevent submitting body modifications in the current handler

         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         Debug.Assert(note.Resolvable);

         await onToggleResolveDiscussionAsync();
      }

      public void AdjustToWidth(int width)
      {
         resizeBoxContent(width);
         repositionBoxContent(width);
      }

      private void onCreate(Discussion discussion)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         Debug.Assert(!firstNote.System);

         _discussionId = discussion.Id;
         _individual = discussion.Individual_Note;

         _labelAuthor = createLabelAuthor(firstNote);
         _labelFileName = createLabelFilename(firstNote);
         _panelContext = createDiffContext(firstNote);
         _textboxesNotes = createTextBoxes(discussion.Notes);

         Controls.Add(_labelAuthor);
         Controls.Add(_labelFileName);
         Controls.Add(_panelContext);
         foreach (var note in _textboxesNotes)
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

         int fontSizePx = 12;
         int rowsVPaddingPx = 2;
         int rowHeight = (fontSizePx + rowsVPaddingPx * 2 + 1 /* border of control */ + 2);
         // we're adding 2 extra pixels for each row because HtmlRenderer does not support CSS line-height property
         // this value was found experimentally

         int panelHeight = (_diffContextDepth.Size + 1) * rowHeight;

         HtmlPanel htmlPanel = new HtmlPanel
         {
            BorderStyle = BorderStyle.FixedSingle,
            Height = panelHeight,
            MinimumSize = new Size(600, 0),
            TabStop = false
         };

         DiffPosition position = convertToDiffPosition(firstNote.Position);
         htmlPanel.Text = getContext(_panelContextMaker, position,
            _diffContextDepth, fontSizePx, rowsVPaddingPx);
         _htmlToolTip.SetToolTip(htmlPanel, getContext(_tooltipContextMaker, position,
            _tooltipContextDepth, fontSizePx, rowsVPaddingPx));

         return htmlPanel;
      }

      private string getContext(IContextMaker contextMaker, DiffPosition position,
         ContextDepth depth, int fontSizePx, int rowsVPaddingPx)
      {
         if (contextMaker == null || _formatter == null)
         {
            return "<html><body>Cannot access git repository and render diff context</body></html>";
         }

         string contextHtml = "<html><body>N/A</body></html>";
         try
         {
            DiffContext context = contextMaker.GetContext(position, depth);
            contextHtml = _formatter.FormatAsHTML(context, fontSizePx, rowsVPaddingPx);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot render HTML context");
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot render HTML context");
         }
         return contextHtml;
      }

      // Create a label that shows filename
      private Label createLabelFilename(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         string oldPath = firstNote.Position.Old_Path ?? String.Empty;
         string newPath = firstNote.Position.New_Path ?? String.Empty;
         string result = oldPath == newPath ? oldPath : (newPath + "\n(was " + oldPath + ")");

         Label labelFilename = new Label
         {
            Text = result,
            AutoSize = true
         };
         return labelFilename;
      }
      // Create a label that shows discussion author
      private Label createLabelAuthor(DiscussionNote firstNote)
      {
         Label labelAuthor = new Label
         {
            Text = firstNote.Author.Name,
            AutoEllipsis = true,
            MinimumSize = new Size(100, 0)
         };
         return labelAuthor;
      }

      private List<Control> createTextBoxes(List<DiscussionNote> notes)
      {
         var discussionResolved = notes.Cast<DiscussionNote>().All(x => (!x.Resolvable || x.Resolved));

         List<Control> boxes = new List<Control>();
         foreach (var note in notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            TextBox textBox = createTextBox(note, discussionResolved);
            boxes.Add(textBox);
         }
         return boxes;
      }

      private bool canBeModified(DiscussionNote note)
      {
         return note.Author.Id == _currentUser.Id && (!note.Resolvable || !note.Resolved);
      }

      private TextBox createTextBox(DiscussionNote note, bool discussionResolved)
      {
         TextBox textBox = new TextBox();
         _toolTip.SetToolTip(textBox, getNoteTooltipText(note));
         textBox.ReadOnly = true;
         textBox.Text = note.Body.Replace("\n", "\r\n");
         textBox.Multiline = true;
         textBox.Height = getTextBoxPreferredHeight(textBox);
         textBox.BackColor = getNoteColor(note);
         textBox.LostFocus += TextBox_LostFocus;
         textBox.KeyDown += TextBox_KeyDown;
         textBox.KeyUp += TextBox_KeyUp;
         textBox.MinimumSize = new Size(300, 0);
         textBox.Tag = note;
         textBox.ContextMenu = createContextMenuForDiscussionNote(note, discussionResolved, textBox);

         return textBox;
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
            Text = "Edit Note"
         };
         menuItemEditNote.Click += MenuItemEditNote_Click;
         contextMenu.MenuItems.Add(menuItemEditNote);

         MenuItem menuItemReply = new MenuItem
         {
            Enabled = !_individual,
            Text = "Reply"
         };
         menuItemReply.Click += MenuItemReply_Click;
         contextMenu.MenuItems.Add(menuItemReply);

         return contextMenu;
      }

      private static int getTextBoxPreferredHeight(TextBox textBox)
      {
         var numberOfLines = SendMessage(textBox.Handle.ToInt32(), EM_GETLINECOUNT, 0, 0);
         return textBox.Font.Height * (numberOfLines + 1);
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
         if (note.Resolvable)
         {
            if (note.Author.Id == _mergeRequestDetails.Author.Id)
            {
               return note.Resolved ? _colorScheme.GetColor("Discussions_Author_Notes_Resolved")
                                    : _colorScheme.GetColor("Discussions_Author_Notes_Unresolved");
            }
            else
            {
               return note.Resolved ? _colorScheme.GetColor("Discussions_NonAuthor_Notes_Resolved")
                                    : _colorScheme.GetColor("Discussions_NonAuthor_Notes_Unresolved");
            }
         }
         else
         {
            return _colorScheme.GetColor("Discussions_Comments");
         }
      }

      private void resizeBoxContent(int width)
      {
         foreach (var textbox in _textboxesNotes)
         {
            textbox.Width = width * NotesWidth / 100;
            textbox.Height = getTextBoxPreferredHeight(textbox as TextBox);
         }

         if (_panelContext != null)
         {
            _panelContext.Width = width * ContextWidth / 100;
            _htmlToolTip.MaximumSize = new Size(_panelContext.Width, 0 /* auto-height */);
         }
         _labelAuthor.Width = width * LabelAuthorWidth / 100;
         if (_labelFileName != null)
         {
            _labelFileName.Width = width * LabelFilenameWidth / 100;
         }
      }

      private void repositionBoxContent(int width)
      {
         int interControlVertMargin = 5;
         int interControlHorzMargin = width * HorzMarginWidth / 100;

         // the LabelAuthor is placed at the left side
         Point labelPos = new Point(interControlHorzMargin, interControlVertMargin);
         _labelAuthor.Location = labelPos;

         // the Context is an optional control to the right of the Label
         Point ctxPos = new Point(_labelAuthor.Location.X + _labelAuthor.Width + interControlHorzMargin,
            interControlVertMargin);
         if (_panelContext != null)
         {
            _panelContext.Location = ctxPos;
         }

         // prepare initial position for controls that places to the right of the Context
         int nextNoteX = ctxPos.X + (_panelContext == null ? 0 : _panelContext.Width + interControlHorzMargin);
         Point nextNotePos = new Point(nextNoteX, ctxPos.Y);

         // the LabelFilename is placed to the right of the Context and vertically aligned with Notes
         if (_labelFileName != null)
         {
            _labelFileName.Location = nextNotePos;
            nextNotePos.Offset(0, _labelFileName.Height + interControlVertMargin);
         }

         // a list of Notes is to the right of the Context
         foreach (var note in _textboxesNotes)
         {
            note.Location = nextNotePos;
            nextNotePos.Offset(0, note.Height + interControlVertMargin);
         }

         int lblAuthorHeight = _labelAuthor.Location.Y + _labelAuthor.PreferredSize.Height;
         int lblFNameHeight = (_labelFileName == null ? 0 : _labelFileName.Location.Y + _labelFileName.Height);
         int ctxHeight = (_panelContext == null ? 0 : _panelContext.Location.Y + _panelContext.Height);
         int notesHeight = _textboxesNotes[_textboxesNotes.Count - 1].Location.Y + _textboxesNotes[_textboxesNotes.Count - 1].Height;

         int boxContentWidth = nextNoteX + _textboxesNotes[0].Width;
         int boxContentHeight = new[] { lblAuthorHeight, lblFNameHeight, ctxHeight, notesHeight }.Max();
         Size = new Size(boxContentWidth + interControlHorzMargin, boxContentHeight + interControlVertMargin);
      }

      async private Task onReplyAsync(string body)
      {
         try
         {
            await _mergeRequestAccessor.Discussions.Get(_discussionId).CreateNewNoteTaskAsync(
               new CreateNewNoteParameters
               {
                  Body = body
               });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create a reply to discussion");
            return;
         }

         await refreshDiscussion();
      }

      private void onStartEditNote(TextBox textBox)
      {
         textBox.ReadOnly = false;
         textBox.Focus();
      }

      private void onCancelEditNote(TextBox textBox)
      {
         textBox.ReadOnly = true;

         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         textBox.Text = note.Body.Replace("\n", "\r\n");

         int newHeight = getTextBoxPreferredHeight(textBox);
         if (newHeight < textBox.Height)
         {
            textBox.Height = newHeight;
            _onSizeChanged();
         }
      }

      async private Task onSubmitNewBodyAsync(TextBox textBox)
      {
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

         try
         {
            note = await _mergeRequestAccessor.Discussions.Get(_discussionId).ModifyNoteTaskAsync(note.Id,
               new ModifyDiscussionNoteParameters
               {
                  Type = ModifyDiscussionNoteParameters.ModificationType.Body,
                  Body = textBox.Text
               });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot update discussion text");
            return;
         }

         _toolTipNotifier.Show("Discussion note was edited", textBox, textBox.Width + 20, 0, 2000 /* ms */);

         // Create a new text box
         TextBox newTextBox = createTextBox(note, isDiscussionResolved()); 

         // By default place a new textbox at the same place as the old one. It may be changed if height changed.
         // It is better to change Location right now to avoid flickering during _onSizeChanged(). 
         newTextBox.Location = textBox.Location;

         // By default, let the new textbox to have the same size as the old one.
         newTextBox.Width = textBox.Width;

         // Measure heights
         int oldHeight = textBox.Height;
         int newHeight = getTextBoxPreferredHeight(newTextBox);

         // Update Height, because initial one was measured for a wrong width
         newTextBox.Height = newHeight;

         // Replace text box in Discussion Box
         replaceControlInParent(textBox, newTextBox);

         if (oldHeight != newHeight)
         {
            // Notify parent that our size has changed
            _onSizeChanged();
         }
      }

      async private Task onDeleteNoteAsync(TextBox textBox)
      {
         DiscussionNote note = (DiscussionNote)(textBox.Tag);

         try
         {
            await _mergeRequestAccessor.Notes.Get(note.Id).DeleteTaskAsync();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot delete a discussion note");
            return;
         }

         if (!await refreshDiscussion())
         {
            // Seems it was the only note in the discussion, remove ourselves from parents controls
            Parent.Controls.Remove(this);
         }
      }

      async private Task onToggleResolveNoteAsync(TextBox textBox)
      {
         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         bool wasResolved = note.Resolved;

         try
         {
            await _mergeRequestAccessor.Discussions.Get(_discussionId).ModifyNoteTaskAsync(note.Id,
               new ModifyDiscussionNoteParameters
               {
                  Type = ModifyDiscussionNoteParameters.ModificationType.Resolved,
                  Resolved = !wasResolved
               });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a discussion note");
            return;
         }

         await refreshDiscussion();
      }

      async private Task onToggleResolveDiscussionAsync()
      {
         bool wasResolved = isDiscussionResolved();

         // Change discussion state at Server
         try
         {
            await _mergeRequestAccessor.Discussions.Get(_discussionId).ResolveTaskAsync(
               new ResolveThreadParameters
               {
                  Resolve = !wasResolved
               });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a discussion");
            return;
         }

         await refreshDiscussion();
      }

      async private Task<bool> refreshDiscussion()
      {
         // Get rid of old text boxes
         for (int iControl = Controls.Count - 1; iControl >= 0; --iControl)
         {
            if (Controls[iControl] is TextBox)
            {
               Controls.Remove(Controls[iControl]);
            }
         }
         _textboxesNotes.Clear();

         // Load updated discussion
         Discussion discussion;
         try
         {
            discussion = await _mergeRequestAccessor.Discussions.Get(_discussionId).LoadTaskAsync();
            if (discussion.Notes.Count == 0 || discussion.Notes[0].System)
            {
               return false;
            }
         }
         catch (GitLabRequestException ex)
         {
            // it is not an error here, we treat it as 'last discussion item has been deleted'
            var response = (System.Net.HttpWebResponse)(ex.WebException.Response);
            Debug.Assert(response.StatusCode == System.Net.HttpStatusCode.NotFound);
            return false;
         }

         // Create controls
         _textboxesNotes = createTextBoxes(discussion.Notes);
         foreach (var note in _textboxesNotes)
         {
            Controls.Add(note);
         }

         // To reposition new controls
         _onSizeChanged();
         return true;
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

      private void replaceControlInParent(Control oldControl, Control newControl)
      {
         var index = oldControl.Parent.Controls.IndexOf(oldControl);
         var parent = oldControl.Parent;
         oldControl.Parent.Controls.Remove(oldControl);
         if (newControl != null)
         {
            parent.Controls.Add(newControl);
            parent.Controls.SetChildIndex(newControl, index);
         }

         if (parent is DiscussionBox)
         {
            replaceControl(oldControl, newControl);
         }
      }

      private void replaceControl(Control oldControl, Control newControl)
      {
         if (_labelAuthor == oldControl)
         {
            _labelAuthor = newControl;
         }
         else if (_labelFileName == oldControl)
         {
            _labelFileName = newControl;
         }
         else if (_panelContext == oldControl)
         {
            _panelContext = newControl;
         }
         else
         {
            for (int iNote = 0; iNote < _textboxesNotes.Count; ++iNote)
            {
               if (_textboxesNotes[iNote] == oldControl)
               {
                  _textboxesNotes[iNote] = newControl;
                  break;
               }
            }
         }
      }

      private DiffPosition convertToDiffPosition(Position position)
      {
         return new DiffPosition
         {
            LeftLine = position.Old_Line,
            LeftPath = position.Old_Path,
            RightLine = position.New_Line,
            RightPath = position.New_Path,
            Refs = new mrCore.DiffRefs
            {
               LeftSHA = position.Base_SHA,
               RightSHA = position.Head_SHA
            }
         };
      }

      // Widths in %
      private readonly int HorzMarginWidth = 1;
      private readonly int LabelAuthorWidth = 5;
      private readonly int ContextWidth = 55;
      private readonly int NotesWidth = 34;
      private readonly int LabelFilenameWidth = 34;

      private Control _labelAuthor;
      private Control _labelFileName;
      private Control _panelContext;
      private List<Control> _textboxesNotes;

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly SingleMergeRequestAccessor _mergeRequestAccessor;
      private readonly User _currentUser;
      private string _discussionId;
      private bool _individual;

      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly DiffContextFormatter _formatter;

      private readonly ColorScheme _colorScheme;

      private readonly OnBoxEvent _onSizeChanged;

      private System.Windows.Forms.ToolTip _toolTip;
      private System.Windows.Forms.ToolTip _toolTipNotifier;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlToolTip _htmlToolTip;
   }
}
