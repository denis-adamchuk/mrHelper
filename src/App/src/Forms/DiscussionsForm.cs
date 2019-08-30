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
using TheArtOfDev.HtmlRenderer.WinForms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.App.Controls;
using mrHelper.Core.Context;
using mrHelper.Client.Tools;
using mrHelper.Client.Discussions;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Forms
{
   internal partial class DiscussionsForm : Form
   {
      /// <summary>
      /// Throws:
      /// ArgumentException
      /// </summary>
      internal DiscussionsForm(MergeRequestDescriptor mrd, string mrTitle, User mergeRequestAuthor,
         IGitRepository gitRepository, int diffContextDepth, ColorScheme colorScheme, List<Discussion> discussions,
         DiscussionManager manager, User currentUser)
      {
         _mergeRequestDescriptor = mrd;
         _mergeRequestTitle = mrTitle;
         _mergeRequestAuthor = mergeRequestAuthor;

         _gitRepository = gitRepository;
         _diffContextDepth = diffContextDepth;

         _colorScheme = colorScheme;

         InitializeComponent();

         _manager = manager;

         _currentUser = currentUser;
         if (_currentUser.Id == 0)
         {
            throw new ArgumentException("Bad user Id");
         }

         if (!onRefresh(discussions))
         {
            throw new NoDiscussionsToShow();
         }
      }

      async private void DiscussionsForm_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.F5)
         {
            if (!onRefresh(await loadDiscussionsAsync()))
            {
               MessageBox.Show("No discussions to show. Press OK to close form.", "Information",
                  MessageBoxButtons.OK, MessageBoxIcon.Information);
               Close();
            }
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
         this.Text = DefaultCaption + "   (Loading discussions)";
         try
         {
            discussions = await _manager.GetDiscussionsAsync(_mergeRequestDescriptor);
         }
         catch (DiscussionManagerException)
         {
            MessageBox.Show("Cannot load discussions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            this.Text = DefaultCaption;
         }

         return discussions;
      }

      private bool onRefresh(List<Discussion> discussions)
      {
         if (discussions == null || discussions.Count<Discussion>(x => x.Notes.Count > 0 && !x.Notes[0].System) == 0)
         {
            return false;
         }

         // Avoid scroll bar redraw on each added control
         SuspendLayout();

         // Clean up the form
         Controls.Clear();

         // Load updated data and create controls for it
         this.Text = DefaultCaption + "   (Rendering Discussions Form)";
         createDiscussionBoxes(discussions);

         // Put controls at their places
         ResumeLayout();
         this.Text = DefaultCaption;

         // Set focus to the Form
         Focus();

         return true;
      }

      private void createDiscussionBoxes(List<Discussion> discussions)
      {
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0 || discussion.Notes[0].System)
            {
               continue;
            }

            DiscussionEditor editor = _manager.GetDiscussionEditor(_mergeRequestDescriptor, discussion.Id);
            Control control = new DiscussionBox(discussion, editor, _mergeRequestAuthor, _currentUser,
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

      private string DefaultCaption
      {
         get
         {
            return String.Format("Discussions for merge request \"{0}\" with code repository in \"{1}\"",
               _mergeRequestTitle, _gitRepository?.Path ?? "no repository");
         }
      }

      private readonly MergeRequestDescriptor _mergeRequestDescriptor;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private readonly IGitRepository _gitRepository;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private User _currentUser;
      private readonly DiscussionManager _manager;
   }

   internal class NoDiscussionsToShow : ArgumentException { }; 
}

