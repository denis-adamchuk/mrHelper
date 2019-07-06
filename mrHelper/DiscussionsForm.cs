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
         int groupBoxMarginLeft = 10;
         int groupBoxMarginTop = 10;

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

         // @{ Margins for each control within Discussion Box
         int discussionLabelMarginLeft = 7;
         int discussionLabelMarginTop = 5;
         int filenameTextBoxMarginLeft = 7;
         int filenameTextBoxMarginTop = 5;
         int noteTextBoxMarginLeft = 7;
         int noteTextBoxMarginTop = 5;
         int noteTextBoxMarginBottom = 5;
         int noteTextBoxMarginRight = 7;
         // @}

         // @{ Sizes of controls
         var filenameTextBoxSize = new Size(400, 20);
         var noteTextBoxSize = new Size(500, 20);
         // @}

         // Create a discussion box, which is a container of other controls
         GroupBox groupBox = new GroupBox();

         // Create a label that shows discussion creation data and author
         Label discussionLabel = new Label();
         groupBox.Controls.Add(discussionLabel);
         discussionLabel.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;
         discussionLabel.Location = new Point(discussionLabelMarginLeft, discussionLabelMarginTop);
         discussionLabel.AutoSize = true;

         // Create a text box with filename
         // TODO This can be replaced with a rendered diff snippet later
         TextBox filenameTextBox = new TextBox();
         groupBox.Controls.Add(filenameTextBox);
         filenameTextBox.Location = new Point(filenameTextBoxMarginLeft,
            discussionLabel.Location.Y + discussionLabel.Size.Height + filenameTextBoxMarginTop);
         filenameTextBox.Size = filenameTextBoxSize;
         if (firstNote.Type == DiscussionNoteType.DiffNote)
         {
            Debug.Assert(firstNote.Position.HasValue);
            filenameTextBox.Text = convertPositionToText(firstNote.Position.Value);
         }

         // Create a series of boxes which represent notes
         Point previousBoxLocation = new Point(0,
            filenameTextBox.Location.Y + filenameTextBox.Size.Height + noteTextBoxMarginTop);
         Size previousBoxSize = new Size(0, 0);
         int shownCount = 0;
         foreach (var note in discussion.Notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            TextBox textBox = new TextBox();
            groupBox.Controls.Add(textBox);
            toolTip.SetToolTip(textBox, "Created at " + note.CreatedAt.ToString() + " by " + note.Author.Name);
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            textBox.Size = noteTextBoxSize;
            textBox.Text = note.Body;
            textBox.Multiline = true;
            if (note.Resolvable && note.Resolved.HasValue && note.Resolved.Value)
            {
               textBox.BackColor = Color.LightGreen;
            }

            Point textBoxLocation = new Point();
            textBoxLocation.X = previousBoxLocation.X + previousBoxSize.Width + noteTextBoxMarginLeft;
            textBoxLocation.Y = previousBoxLocation.Y;
            textBox.Location = textBoxLocation;
            previousBoxLocation = textBoxLocation;
            previousBoxSize = textBox.Size;

            ++shownCount;
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
            noteTextBoxMarginLeft * shownCount
          + noteTextBoxSize.Width * shownCount
          + noteTextBoxMarginRight;

         groupBox.Location = location;
         groupBox.Size = new Size(groupBoxWidth, groupBoxHeight);
         return groupBox;
      }

      private static string convertPositionToText(Position position)
      {
         string result = "Before: ";

         if (position.OldLine != null)
         {
            result += position.OldPath + " (line " + position.OldLine + ")";
         }
         else
         {
            result += "-";
         }

         result += "   After: ";

         if (position.NewLine != null)
         {
            result += position.NewPath + " (line " + position.NewLine + ")";
         }
         else
         {
            result += "-";
         }

         return result;
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly int _mergeRequestId;
      private readonly User _currentUser;
   }
}

