using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrCore;
using TheArtOfDev.HtmlRenderer.WinForms;

// TODO:
// 5. Show configurable number of lines within diff contexts (default: 0 above, 3 below) and mark a selected line with bold
// 7. Add filter for 'comments'
// 9. Add ability to edit/delete notes and change their resolved state 

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      private const int EM_GETLINECOUNT = 0xba;
      [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      private static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam);

      private struct DiscussionBoxControls
      {
         public Control Label;
         public Control Context;
         public List<Control> Notes;
      }

      public DiscussionsForm(string host, string accessToken, string projectId, int mergeRequestId,
         User mrAuthor, GitRepository gitRepository, string diffContextAlgo, int diffContextDepth)
      {
         _host = host;
         _accessToken = accessToken;
         _projectId = projectId;
         _mergeRequestId = mergeRequestId;
         _mrAuthor = mrAuthor;
         _currentUser = getUser();
         if (gitRepository != null)
         {
            if (diffContextAlgo == "Plain")
            {
               _contextMaker = new PlainContextMaker(gitRepository);
            }
            else if (diffContextAlgo == "Enhanced")
            {
               _contextMaker = new EnhancedContextMaker(gitRepository);
            }
            else if (diffContextAlgo == "Combined")
            {
               _contextMaker = new CombinedContextMaker(gitRepository);
            }
            else
            {
               Debug.Assert(false);
               _contextMaker = new CombinedContextMaker(gitRepository);
            }
         }
         _diffContextDepth = diffContextDepth;
         _discussions = loadDiscussions();
         _formatter = new DiffContextFormatter();

         InitializeComponent();
      }

      private void Discussions_Load(object sender, EventArgs e)
      {
         try
         {
            renderDiscussions(_discussions);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private List<Discussion> loadDiscussions()
      {
         GitLabClient client = new GitLabClient(_host, _accessToken);
         return client.GetMergeRequestDiscussions(_projectId, _mergeRequestId);
      }

      private User getUser()
      {
         GitLabClient client = new GitLabClient(_host, _accessToken);
         return client.GetCurrentUser();
      }

      private void renderDiscussions(List<Discussion> discussions)
      {
         int groupBoxMarginLeft = 10;
         int groupBoxMarginTop = 5;

         Point previousBoxLocation = new Point();
         Size previousBoxSize = new Size();
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0)
            {
               continue;
            }

            Point location = new Point();
            location.X = groupBoxMarginLeft;
            location.Y = previousBoxLocation.Y + previousBoxSize.Height + groupBoxMarginTop;
            var discussionBox = createDiscussionBox(discussion, location);
            if (discussionBox != null)
            {
               previousBoxLocation = discussionBox.Location;
               previousBoxSize = discussionBox.Size;
            }
            Controls.Add(discussionBox);
         }
      }

      private Control createDiscussionBox(Discussion discussion, Point location)
      {
         DiscussionBoxControls? controls = createDiscussionBoxControls(discussion);
         return repositionAndBoxControls(controls, location);
      }

      private Control repositionAndBoxControls(DiscussionBoxControls? controls, Point location)
      {
         if (!controls.HasValue)
         {
            return null;
         }
         var c = controls.Value;

         int interControlVertMargin = 5;
         int interControlHorzMargin = 10;

         // separator
         Label horizontalLine = new Label();
         horizontalLine.AutoSize = false;
         horizontalLine.Height = 1;
         horizontalLine.BorderStyle = BorderStyle.FixedSingle;
         horizontalLine.Text = null;
         horizontalLine.Location = new Point();
         horizontalLine.BackColor = Color.LightGray;
         horizontalLine.Width = this.Width - interControlVertMargin;

         // the Label is placed at the left side
         Point labelPos = new Point(interControlHorzMargin, interControlVertMargin);
         c.Label.Location = labelPos;

         // the Context is an optional control to the right of the Label
         Point ctxPos = new Point(
            interControlHorzMargin + c.Label.PreferredSize.Width + interControlHorzMargin, interControlVertMargin);
         if (c.Context != null)
         {
            c.Context.Location = ctxPos;
         }

         // a list of Notes is to the right of Label and Context
         int nextNoteX = ctxPos.X + (c.Context == null ? 0 : c.Context.Width + interControlHorzMargin);
         Point nextNotePos = new Point(nextNoteX, ctxPos.Y);
         foreach (var note in c.Notes)
         {
            note.Location = nextNotePos;
            nextNotePos.Offset(0, note.Height + interControlVertMargin);
         }

         Panel box = new Panel();
         //box.Controls.Add(horizontalLine);
         box.Controls.Add(c.Label);
         box.Controls.Add(c.Context);
         foreach (var note in c.Notes)
         {
            box.Controls.Add(note);
         }
         box.Location = location;

         int labelHeight = labelPos.Y + c.Label.PreferredSize.Height;
         int ctxHeight = (c.Context == null ? 0 : ctxPos.Y + c.Context.Height);
         int notesHeight = c.Notes[c.Notes.Count - 1].Location.Y + c.Notes[c.Notes.Count - 1].Height;
         box.Size = new Size(nextNoteX + c.Notes[0].Width + interControlHorzMargin,
            Math.Max(labelHeight, Math.Max(ctxHeight, notesHeight)) + interControlVertMargin);
         return box;
      }

      private DiscussionBoxControls? createDiscussionBoxControls(Discussion discussion)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         if (firstNote.System)
         {
            return null;
         }

         DiscussionBoxControls controls = new DiscussionBoxControls();
         controls.Label = createDiscussionLabel(firstNote);
         controls.Context = createDiffContext(firstNote);
         controls.Notes = createTextBoxes(discussion.Notes);
         return controls;
      }

      private List<Control> createTextBoxes(List<DiscussionNote> notes)
      {
         Size singleTextBoxSize = new Size(500, 0 /* height is adjusted to line count */);

         List<Control> boxes = new List<Control>();
         foreach (var note in notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            TextBox textBox = new TextBox();
            toolTip.SetToolTip(textBox, getNoteTooltipText(note));
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            textBox.Size = singleTextBoxSize;
            textBox.Text = note.Body;
            textBox.Multiline = true;
            var numberOfLines = SendMessage(textBox.Handle.ToInt32(), EM_GETLINECOUNT, 0, 0);
            textBox.Height = (textBox.Font.Height + 4) * numberOfLines;
            textBox.BackColor = getNoteColor(note);
            textBox.LostFocus += DiscussionNoteTextBox_LostFocus;
            textBox.Tag = note;

            textBox.ContextMenu = new ContextMenu();
            MenuItem menuItemToggleResolve = new MenuItem();
            menuItemToggleResolve.Tag = note;
            menuItemToggleResolve.Text =
               note.Resolvable && note.Resolved.HasValue && note.Resolved.Value ? "Unresolve" : "Resolve";
            menuItemToggleResolve.Click += MenuItemToggleResolve_Click;
            textBox.ContextMenu.MenuItems.Add(menuItemToggleResolve);
            MenuItem menuItemDeleteNote = new MenuItem();
            menuItemDeleteNote.Tag = note;
            menuItemDeleteNote.Text = "Delete Note";
            menuItemDeleteNote.Click += MenuItemDeleteNote_Click;
            textBox.ContextMenu.MenuItems.Add(menuItemDeleteNote);

            boxes.Add(textBox);
         }
         return boxes;
      }

      private void DiscussionNoteTextBox_LostFocus(object sender, EventArgs e)
      {
         TextBox textBox = (TextBox)(sender);
         DiscussionNote note = (DiscussionNote)(textBox.Tag);
         // TODO Send modification request to GitLab
      }

      private void MenuItemDeleteNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         DiscussionNote note = (DiscussionNote)(menuItem.Tag);
         if (MessageBox.Show("This discussion note will be deleted. Are you sure?", "Confirm deletion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
         {
            // TODO Send deletion request to GitLab
            // TODO Re-render current discussion box only
         }
      }

      private void MenuItemToggleResolve_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         DiscussionNote note = (DiscussionNote)(menuItem.Tag);
         if (note.Resolvable && note.Resolved.HasValue)
         {
            // TODO Send modification request to GitLab

            if (note.Resolved.Value)
            {
               // this note is going to become unresolved, so change text to resolve it back
               menuItem.Text = "Resolved";
            }
            else
            {
               // this note is going to become resolved, so change text to unresolve it back
               menuItem.Text = "Unresolved";
            }
         }
      }

      private string getNoteTooltipText(DiscussionNote note)
      {
         string result = string.Empty;
         if (note.Resolvable)
         {
            result += note.Resolved.Value ? "Resolved." : "Not resolved.";
         }
         result += " Created by " + note.Author.Name + " at " + note.CreatedAt.ToLocalTime().ToString("g");
         return result;
      }

      private Color getNoteColor(DiscussionNote note)
      {
         if (note.Resolvable)
         {
            Debug.Assert(note.Resolved.HasValue);

            if (note.Author.Id == _mrAuthor.Id)
            {
               return note.Resolved.Value ? Color.FromArgb(225, 235, 242) : Color.FromArgb(136, 176, 204);
            }
            else
            {
               return note.Resolved.Value ? Color.FromArgb(247, 253, 204) : Color.FromArgb(231, 249, 100);
            }
         }
         else
         {
            return Color.FromArgb(255, 236, 250);
         }
      }

      private HtmlPanel createDiffContext(DiscussionNote firstNote)
      {
         if (firstNote.Type != DiscussionNoteType.DiffNote)
         {
            return null;
         }

         Debug.Assert(firstNote.Position.HasValue);

         int fontSizePx = 12;
         int rowsVPaddingPx = 2;
         int height = _diffContextDepth * (fontSizePx + rowsVPaddingPx * 2 + 2);
         // we're adding 2 extra pixels for each row because HtmlRenderer does not support CSS line-height property
         // this value was found experimentally

         HtmlPanel htmlPanel = new HtmlPanel();
         htmlPanel.Size = new Size(1000 /* big enough for long lines */, height);
         toolTip.SetToolTip(htmlPanel, firstNote.Position.Value.NewPath);

         if (_contextMaker != null)
         {
            DiffContext context = _contextMaker.GetContext(firstNote.Position.Value, _diffContextDepth);
            htmlPanel.Text = _formatter.FormatAsHTML(context, fontSizePx, rowsVPaddingPx);
         }
         else
         {
            htmlPanel.Text = "<html><body>Cannot access git repository and render diff context</body></html>";
         }

         return htmlPanel;
      }

      // Create a label that shows discussion creation date and author
      private static Label createDiscussionLabel(DiscussionNote firstNote)
      {
         Label discussionLabel = new Label();
         discussionLabel.Text =
            "Discussion started\n" +
            "by " + firstNote.Author.Name + "\n" +
            "at " + firstNote.CreatedAt.ToLocalTime().ToString("g");
         discussionLabel.AutoSize = true;
         return discussionLabel;
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly User _mrAuthor;
      private readonly int _mergeRequestId;
      private readonly int _diffContextDepth;
      private readonly User _currentUser;
      private readonly ContextMaker _contextMaker;
      private readonly List<Discussion> _discussions;
      private readonly DiffContextFormatter _formatter;
   }
}

