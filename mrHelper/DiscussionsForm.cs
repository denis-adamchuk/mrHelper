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

      private void renderDiscussions(List<Discussion> discussions)
      {
         int listViewMarginX = 20;
         int listViewMarginY = 20;
         int listViewHeight = 50; // Width is determined by Form width minus left and right margins
         Size listViewSize = new Size(this.Width - listViewMarginX * 2, listViewHeight);

         int offsetY = listViewMarginY;
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0)
            {
               continue;
            }

            var firstNote = discussion.Notes[0];
            Label label = new Label();
            label.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;
            
            ListView listView = new ListView();
            listView.HeaderStyle = ColumnHeaderStyle.None;
            listView.View = View.Details;
            listView.Location = new Point(listViewMarginX, offsetY);
            listView.Size = listViewSize;

            ListViewItem listViewItem = new ListViewItem();
            foreach (var note in discussion.Notes)
            {
               listViewItem.SubItems.Add(note.Body);
            }
            listView.Items.Add(listViewItem);

            foreach (ColumnHeader column in listView.Columns)
            {
               column.AutoResize(ColumnHeaderAutoResizeStyle.None);
               column.Width = 100;
            }

            offsetY += listViewMarginY;
         }
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly int _mergeRequestId;
   }
}
