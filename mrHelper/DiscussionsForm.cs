using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrCore;

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      public DiscussionsForm(string host, string accessToken, string projectId, int mergeRequestId)
      {
         _host = host;
         _accessToken = accessToken;
         _projectId = projectId;
         _mergeRequestId = mergeRequestId;
         _currentUser = getUser();

         InitializeComponent();
      }

      private void Discussions_Load(object sender, EventArgs e)
      {
         try
         {
            renderDiscussions(loadDiscussions());
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
         int groupBoxMarginLeft = 20;
         int groupBoxMarginTop = 20;

         Point previousBoxLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Size previousBoxSize = new Size();
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0)
            {
               continue;
            }

            Point location = new Point();
            location.X = groupBoxMarginLeft;
            location.Y = previousBoxLocation.Y + previousBoxSize.Height;
            var discussionBoxSize = createDiscussionBox(discussion, location);
            if (discussionBoxSize.HasValue)
            {
               previousBoxSize = discussionBoxSize.Value;
            }
         }
      }

      private Size? createDiscussionBox(Discussion discussion, Point location)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         if (firstNote.System)
         {
            return null;
         }

         // @{ Margins for each control within Discussion Box
         int discussionLabelMarginLeft = 10;
         int discussionLabelMarginTop = 10;
         int filenameTextBoxMarginLeft = 10;
         int filenameTextBoxMarginTop = 10;
         int noteTextBoxMarginLeft = 10;
         int noteTextBoxMarginTop = 10;
         int noteTextBoxMarginBottom = 10;
         int noteTextBoxMarginRight = 10;
         // @}

         // @{ Sizes of controls
         var filenameTextBoxSize = new Size(300, 20);
         var noteTextBoxSize = new Size(300, 100);
         // @}

         // Create a discussion box, which is a container of other controls
         GroupBox groupBox = new GroupBox();

         // Create a label that shows discussion creation data and author
         Label discussionLabel = new Label();
         groupBox.Controls.Add(discussionLabel);
         discussionLabel.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;
         discussionLabel.Location = new Point(discussionLabelMarginLeft, discussionLabelMarginTop);

         // Create a text box with filename
         // TODO This can be replaced with a rendered diff snippet later
         TextBox filenameTextBox = new TextBox();
         groupBox.Controls.Add(filenameTextBox);
         filenameTextBox.Location = new Point(filenameTextBoxMarginLeft,
            discussionLabel.Location.Y + discussionLabel.Size.Height + filenameTextBoxMarginTop);
         filenameTextBox.Size = filenameTextBoxSize;
         if (firstNote.Type == DiscussionNoteType.DiffNote)
         {
            filenameTextBox.Text = firstNote.Position.NewPath + " (" + firstNote.Position.NewLine + ")";
         }

         // Create a series of boxes which represent notes
         Point previousBoxLocation = new Point(noteTextBoxMarginLeft,
            filenameTextBox.Location.Y + filenameTextBox.Size.Height + noteTextBoxMarginTop);
         foreach (var note in discussion.Notes)
         {
            TextBox textBox = new TextBox();
            groupBox.Controls.Add(textBox);
            toolTip.SetToolTip(textBox, "Created at " + note.CreatedAt.ToString() + " by " + note.Author.Name);
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            textBox.Size = noteTextBoxSize;

            Point textBoxLocation = new Point();
            textBoxLocation.X = previousBoxLocation.X + noteTextBoxMarginLeft;
            textBoxLocation.Y = previousBoxLocation.Y;
            textBox.Location = textBoxLocation;
            previousBoxLocation = textBoxLocation;
         }

         // Calculate discussion box location and size
         int groupBoxHeight =
            discussionLabelMarginTop
          + discussionLabel.Height
          + filenameTextBoxMarginTop
          + filenameTextBox.Height
          + noteTextBoxMarginTop
          + noteTextBoxSize.Height
          + noteTextBoxMarginBottom;

         int groupBoxWidth =
            noteTextBoxMarginLeft * discussion.Notes.Count
          + noteTextBoxSize.Width * discussion.Notes.Count
          + noteTextBoxMarginRight;

         groupBox.Location = location;
         groupBox.Size = new Size(groupBoxHeight, groupBoxWidth);
         return groupBox.Size;
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly int _mergeRequestId;
      private readonly User _currentUser;
   }
}

