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
using GitLabSharp;

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      static private string DefaultCaption = "Discussions";

      public DiscussionsForm(MergeRequestDetails mergeRequestDetails, GitRepository gitRepository,
         int diffContextDepth, ColorScheme colorScheme, List<Discussion> discussions, User currentUser)
      {
         _mergeRequestDetails = mergeRequestDetails;

         _gitRepository = gitRepository;
         _diffContextDepth = diffContextDepth;

         _colorScheme = colorScheme;

         InitializeComponent();

         _currentUser = currentUser;
         if (_currentUser.Id != 0)
         {
            onRefresh(discussions);
         }
      }

      async private void DiscussionsForm_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.F5)
         {
            onRefresh(await loadDiscussionsAsync());
         }
      }

      private void DiscussionsForm_Layout(object sender, LayoutEventArgs e)
      {
         repositionDiscussionBoxes();
      }

      protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
      {
         // https://nickstips.wordpress.com/2010/03/03/c-panel-resets-scroll-position-after-focus-is-lost-and-regained/
         // Returning the current location prevents the Form from
         // scrolling to the active control when the Form loses and regains focus
         return DisplayRectangle.Location;
      }

      async private Task<List<Discussion>> loadDiscussionsAsync()
      {
         List<Discussion> discussions = null;
         GitLab gl = new GitLab(_mergeRequestDetails.Host, _mergeRequestDetails.AccessToken);
         this.Text = DefaultCaption + "   (Loading discussions)";
         try
         {
            discussions = await gl.Projects.Get(_mergeRequestDetails.ProjectId).MergeRequests.
               Get(_mergeRequestDetails.MergeRequestIId).Discussions.LoadAllTaskAsync();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load discussions from GitLab");
         }
         this.Text = DefaultCaption;

         return discussions;
      }

      private void onRefresh(List<Discussion> discussions)
      {
         if (discussions == null || discussions.Count<Discussion>(x => x.Notes.Count > 0 && !x.Notes[0].System) == 0)
         {
            MessageBox.Show("No discussions to show. Press OK to close form.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close(); // a simple way to handle fatal errors in this Form
            return;
         }

         // Avoid scroll bar redraw on each added control
         SuspendLayout();

         // Clean up the form
         Controls.Clear();

         // Load updated data and create controls for it
         createDiscussionBoxes(discussions);

         // Put controls at their places
         ResumeLayout();

         // Set focus to the Form
         Focus();
      }

      private void createDiscussionBoxes(List<Discussion> discussions)
      {
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0 || discussion.Notes[0].System)
            {
               continue;
            }

            var control = new DiscussionBox(discussion, _mergeRequestDetails, _currentUser,
               _diffContextDepth, _gitRepository, _colorScheme,
               () => {
                  repositionDiscussionBoxes();
               });
            Controls.Add(control);
         }
      }

      private void repositionDiscussionBoxes()
      {
         int groupBoxMarginLeft = 5;
         int groupBoxMarginTop = 5;

         Point previousBoxLocation = new Point();
         Size previousBoxSize = new Size();
         foreach (Control control in Controls)
         {
            Debug.Assert(control is DiscussionBox);

            Point location = new Point
            {
               X = groupBoxMarginLeft,
               Y = previousBoxLocation.Y + previousBoxSize.Height + groupBoxMarginTop
            };

            // Discussion box can take all the width except scroll bars and the left margin
            (control as DiscussionBox).AdjustToWidth(ClientSize.Width - groupBoxMarginLeft);

            control.Location = new Point(
               location.X - HorizontalScroll.Value,
               location.Y - VerticalScroll.Value);
            previousBoxLocation = location;
            previousBoxSize = control.Size;
         }
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly GitRepository _gitRepository;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private User _currentUser;
   }
}

