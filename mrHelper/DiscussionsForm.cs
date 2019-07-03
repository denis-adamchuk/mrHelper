using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            previousBoxSize = createDiscussionBox(discussion, location);
         }
      }

      private Size createDiscussionBox(Discussion discussion, Point location)
      {
         int discussionLabelMarginLeft = 10;
         int discussionLabelMarginTop = 10;

         int filenameTextBoxMarginLeft = 10;
         int filenameTextBoxMarginTop = 10;
         int noteTextBoxMarginLeft = 10;
         int noteTextBoxMarginTop = 10;
         int noteTextBoxOffsetTop = 20;

         var filenameTextBoxSize = new Size(300, 20);
         var noteTextBoxSize = new Size(300, 100);
         GroupBox groupBox = new GroupBox();
         groupBox.Location = location;

         var firstNote = discussion.Notes[0];
         Label label = new Label();
         label.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;

         TextBox filenameTextBox = new TextBox();
         filenameTextBox.Size = filenameTextBoxSize;
         // TODO Add file name if note type is DiffNote

         foreach (var note in discussion.Notes)
         {
            TextBox textBox = new TextBox();
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            //textBox.Site = noteTextBoxSize;
            //textBox.Location = 
            //groupBox.Size increment
         }

         return groupBox.Size;
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly int _mergeRequestId;
      private readonly User _currentUser;
   }
}
