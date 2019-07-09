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
// 1. Move 'labels' to the left of 'discussion boxes'
// 2. Decrease a margin between discussion boxes
// 3. Consider removing frames of discussion boxes
// 4. Introduce a four-color palette for notes: author/non-author + resolved/non-resolved
// 5. Show configurable number of lines within diff contexts (default: 0 above, 3 below) and mark a selected line with bold
// 6. Add info about resolved/non-resolved state of note to tooltips of notes
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
         int groupBoxMarginTop = 15;

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

      private GroupBox createDiscussionBox(Discussion discussion, Point location)
      {
         DiscussionBoxControls? controls = createDiscussionBoxControls(discussion, 4 /* context size */);
         return repositionAndBoxControls(controls, location);
      }

      private GroupBox repositionAndBoxControls(DiscussionBoxControls? controls, Point location)
      {
         if (!controls.HasValue)
         {
            return null;
         }
         var c = controls.Value;

         int leftMargin = 7;
         int interControlVertMargin = 10;
         int interControlHorzMargin = 20;

         // the Label is a top-left control
         Point labelPos = new Point(leftMargin, 10);
         c.Label.Location = labelPos;

         // the Context is an optional control below the Label
         Point ctxPos = new Point(leftMargin, labelPos.Y + c.Label.Height + interControlVertMargin);
         if (c.Context != null)
         {
            c.Context.Location = ctxPos;
         }

         // a list of Notes is either below the Label or to the right of the Context
         int nextNoteX = leftMargin + (c.Context == null ? 0 : c.Context.Width + interControlHorzMargin);
         Point nextNotePos = new Point(nextNoteX, ctxPos.Y);
         foreach (var note in c.Notes)
         {
            note.Location = nextNotePos;
            nextNotePos.Offset(0, note.Height + interControlVertMargin);
         }

         GroupBox box = new GroupBox();
         box.Controls.Add(c.Label);
         box.Controls.Add(c.Context);
         foreach (var note in c.Notes)
         {
            box.Controls.Add(note);
         }
         box.Location = location;
         box.Size = new Size(nextNoteX + c.Notes[0].Width + interControlHorzMargin,
            Math.Max((c.Context == null ? 0 : ctxPos.Y + c.Context.Height),
               c.Notes[c.Notes.Count - 1].Location.Y + c.Notes[c.Notes.Count - 1].Height) + interControlVertMargin);
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
            toolTip.SetToolTip(textBox, "Created at " + note.CreatedAt.ToString() + " by " + note.Author.Name);
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            textBox.Size = singleTextBoxSize;
            textBox.Text = note.Body;
            textBox.Multiline = true;
            var numberOfLines = SendMessage(textBox.Handle.ToInt32(), EM_GETLINECOUNT, 0, 0);
            textBox.Height = (textBox.Font.Height + 4) * numberOfLines;
            if (note.Resolvable)
            {
               if (note.Resolved.HasValue && note.Resolved.Value)
               {
                  textBox.BackColor = Color.FromArgb(174, 240, 216);
               }
               else
               {
                  textBox.BackColor = Color.FromArgb(239, 228, 176);
               }
            }
            else
            {
               textBox.BackColor = Color.FromArgb(196, 219, 201);
            }

            boxes.Add(textBox);
         }
         return boxes;
      }

      private HtmlPanel createDiffContext(DiscussionNote firstNote, int contextSize)
      {
         if (firstNote.Type != DiscussionNoteType.DiffNote)
         {
            return null;
         }

         int fontSizePx = 12;
         int rowsVPaddingPx = 2;
         int height = contextSize * (fontSizePx + rowsVPaddingPx * 2);

         HtmlPanel htmlPanel = new HtmlPanel();
         htmlPanel.Size = new Size(1000 /* big enough for wide lines */, height);

         try
         {
            Debug.Assert(firstNote.Position.HasValue);
            DiffContext context = _contextMaker.GetContext(firstNote.Position.Value, contextSize);
            string text = _formatter.FormatAsHTML(context, fontSizePx, rowsVPaddingPx);
            htmlPanel.Text = text;
         }
         catch (Exception)
         {
            htmlPanel.Text = "<html><body>Cannot show diff context</body></html>";
         }

         return htmlPanel;
      }

      // Create a label that shows discussion creation date and author
      private static Label createDiscussionLabel(DiscussionNote firstNote)
      {
         Label discussionLabel = new Label();
         discussionLabel.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;
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

