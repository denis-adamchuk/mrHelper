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

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      private const int EM_GETLINECOUNT = 0xba;
      [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      private static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam); 

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

         InitializeComponent();
      }

      private void Discussions_Load(object sender, EventArgs e)
      {
         try
         {
            this.Hide();
            renderDiscussions(loadDiscussions());
            this.Show();
            this.Invalidate();
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
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         if (firstNote.System)
         {
            return null;
         }

         // Create a discussion box, which is a container of other controls
         GroupBox groupBox = createDiscussionBox(discussion, 4 /* context size */);
         updateDiscussionBoxLayout(groupBox);

         return groupBox;
      }

      private void updateDiscussionBoxLayout(GroupBox groupBox)
      {
         //// @{ Margins for each control within Discussion Box
         //int discussionLabelMarginLeft = 7;
         //int discussionLabelMarginTop = 10;
         //int htmlPanelMarginLeft = 7;
         //int htmlPanelMarginTop = 5;
         //int noteTextBoxMarginLeft = 7;
         //int noteTextBoxMarginTop = 5;
         //int noteTextBoxMarginBottom = 5;
         //int noteTextBoxMarginRight = 7;
         //// @}

         //// Calculate discussion box location and size
         //int groupBoxHeight =
         //   discussionLabelMarginTop
         // + discussionLabel.Height
         // + htmlPanelMarginTop
         // + htmlPanelSize.Height
         // + noteTextBoxMarginTop
         // + biggestTextBoxHeight
         // + noteTextBoxMarginBottom;

         //int groupBoxWidth =
         // +Math.Max(htmlPanelMarginLeft + htmlPanelSize.Width,
         //            (noteTextBoxMarginLeft + noteTextBoxSize.Width) * shownCount + noteTextBoxMarginRight);

         //groupBox.Location = location;
         //groupBox.Size = new Size(groupBoxWidth, groupBoxHeight);
      }

      private GroupBox createDiscussionBox(Discussion discussion, int contextSize)
      {
         var firstNote = discussion.Notes[0];

         GroupBox groupBox = new GroupBox();
         groupBox.Controls.Add(createDiscussionLabel(firstNote));

         // Create a box that shows diff context
         var html = createDiffContext(firstNote, contextSize);
         if (html != null)
         {
            groupBox.Controls.Add(html);
         }

         // Create a series of boxes which represent notes
         List<TextBox> textBoxes = createTextBoxes(discussion.Notes);
         foreach (var tb in textBoxes)
         {
            groupBox.Controls.Add(tb);
         }

         return groupBox;
      }

      private List<TextBox> createTextBoxes(List<DiscussionNote> notes)
      {
         Size singleTextBoxSize = new Size(500, 0 /* height is adjusted to line count */);

         List<TextBox> boxes = new List<TextBox>();
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
            if (note.Resolvable && note.Resolved.HasValue && note.Resolved.Value)
            {
               textBox.BackColor = Color.LightGreen;
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
            DiffContextFormatter diffContextFormatter = new DiffContextFormatter();
            Debug.Assert(firstNote.Position.HasValue);
            DiffContext context = _contextMaker.GetContext(firstNote.Position.Value, contextSize);
            string text = diffContextFormatter.FormatAsHTML(context, fontSizePx, rowsVPaddingPx);
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
   }
}

