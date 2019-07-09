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
// 8. Add a parameter to choose context diff making algorithm
// 9. Add ability to edit/delete notes and change their resolved state 
// 10. Avoid scroll bars in context boxes when possible

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
         GitRepository gitRepository)
      {
         _host = host;
         _accessToken = accessToken;
         _projectId = projectId;
         _mergeRequestId = mergeRequestId;
         _currentUser = getUser();
         _gitRepository = gitRepository;
         _contextMaker = new CombinedContextMaker(_gitRepository);
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
         DiscussionBoxControls? controls = createDiscussionBoxControls(discussion, 4 /* context size */);
         return repositionAndBoxControls(controls, location);
      }

      private Control repositionAndBoxControls(DiscussionBoxControls? controls, Point location)
      {
         if (!controls.HasValue)
         {
            return null;
         }
         var c = controls.Value;

         int interControlVertMargin = 10;
         int interControlHorzMargin = 10;

         // separator
         Label label = new Label();
         label.AutoSize = false;
         label.Height = 1;
         label.BorderStyle = BorderStyle.FixedSingle;
         label.Text = null;
         label.Location = new Point();
         label.BackColor = Color.LightGray;
         label.Width = this.Width - interControlVertMargin;

         // the Label is placed to the left
         Point labelPos = new Point(interControlHorzMargin, interControlVertMargin);
         c.Label.Location = labelPos;

         // the Context is an optional control to the right of the Label
         Point ctxPos = new Point(interControlHorzMargin + c.Label.Width + interControlHorzMargin, interControlVertMargin);
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

         //GroupBox box = new GroupBox();
         Panel box = new Panel();
         box.Controls.Add(label);
         box.Controls.Add(c.Label);
         box.Controls.Add(c.Context);
         foreach (var note in c.Notes)
         {
            box.Controls.Add(note);
         }
         box.Location = location;

         int labelHeight = labelPos.Y + c.Label.Height;
         int ctxHeight = (c.Context == null ? 0 : ctxPos.Y + c.Context.Height);
         int notesHeight = c.Notes[c.Notes.Count - 1].Location.Y + c.Notes[c.Notes.Count - 1].Height;
         box.Size = new Size(nextNoteX + c.Notes[0].Width + interControlHorzMargin,
            Math.Max(labelHeight, Math.Max(ctxHeight, notesHeight)) + interControlVertMargin);
         return box;
      }

      private DiscussionBoxControls? createDiscussionBoxControls(Discussion discussion, int contextSize)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         if (firstNote.System)
         {
            return null;
         }

         DiscussionBoxControls controls = new DiscussionBoxControls();
         controls.Label = createDiscussionLabel(firstNote);
         controls.Context = createDiffContext(firstNote, contextSize);
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

            boxes.Add(textBox);
         }
         return boxes;
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

            if (note.Author.Id == _currentUser.Id)
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

      private HtmlPanel createDiffContext(DiscussionNote firstNote, int contextSize)
      {
         if (firstNote.Type != DiscussionNoteType.DiffNote)
         {
            return null;
         }

         Debug.Assert(firstNote.Position.HasValue);

         int fontSizePx = 12;
         int rowsVPaddingPx = 2;
         int height = contextSize * (fontSizePx + rowsVPaddingPx * 2);

         HtmlPanel htmlPanel = new HtmlPanel();
         htmlPanel.Size = new Size(1000 /* big enough for long lines */, height);
         htmlPanel.AutoScroll = false;

         DiffContext context = _contextMaker.GetContext(firstNote.Position.Value, contextSize);
         htmlPanel.Text = _formatter.FormatAsHTML(context, fontSizePx, rowsVPaddingPx);

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
      private readonly int _mergeRequestId;
      private readonly User _currentUser;
      private readonly GitRepository _gitRepository;
      private readonly ContextMaker _contextMaker;
      private readonly List<Discussion> _discussions;
      private readonly DiffContextFormatter _formatter;
   }
}

