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
         DiscussionManager manager, User currentUser, Func<MergeRequestDescriptor, Task> updateGitRepository)
      {
         _mergeRequestDescriptor = mrd;
         _mergeRequestTitle = mrTitle;
         _mergeRequestAuthor = mergeRequestAuthor;

         _gitRepository = gitRepository;
         _diffContextDepth = diffContextDepth;

         _colorScheme = colorScheme;

         _manager = manager;
         _updateGitRepository = updateGitRepository;

         _currentUser = currentUser;
         if (_currentUser.Id == 0)
         {
            throw new ArgumentException("Bad user Id");
         }

         InitializeComponent();

         DiscussionFilterState state = new DiscussionFilterState
            {
               ByCurrentUserOnly = false,
               ByAnswers = FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               ByResolution = FilterByResolution.Resolved | FilterByResolution.NotResolved
            };

         DisplayFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, state);
         SystemFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, state);

         FilterPanel = new DiscussionFilterPanel(DisplayFilter.Filter,
            () =>
            {
               DisplayFilter.Filter = FilterPanel.Filter;
               updateLayout(null);
            });
         ActionsPanel = new DiscussionActionsPanel(async () => await onRefresh());

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);

         if (!renderDiscussions(discussions))
         {
            throw new NoDiscussionsToShow();
         }
      }

      async private void DiscussionsForm_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.F5)
         {
            await onRefresh();
         }
         else if (e.KeyCode == Keys.Home)
         {
            if (!(ActiveControl is TextBox))
            {
               AutoScrollPosition = new Point(AutoScrollPosition.X, VerticalScroll.Minimum);
               PerformLayout();
               e.Handled = true;
            }
         }
         else if (e.KeyCode == Keys.End)
         {
            if (!(ActiveControl is TextBox))
            {
               AutoScrollPosition = new Point(AutoScrollPosition.X, VerticalScroll.Maximum);
               PerformLayout();
               e.Handled = true;
            }
         }
         else if (e.KeyCode == Keys.PageUp)
         {
            AutoScrollPosition = new Point(AutoScrollPosition.X,
               Math.Max(VerticalScroll.Minimum, VerticalScroll.Value - VerticalScroll.LargeChange));
            PerformLayout();
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.PageDown)
         {
            AutoScrollPosition = new Point(AutoScrollPosition.X,
               Math.Min(VerticalScroll.Maximum, VerticalScroll.Value + VerticalScroll.LargeChange));
            PerformLayout();
            e.Handled = true;
         }
      }

      private async Task onRefresh()
      {
         Trace.TraceInformation("[DiscussionsForm] Refreshing by user request");

         if (!renderDiscussions(await loadDiscussionsAsync()))
         {
            MessageBox.Show("No discussions to show. Press OK to close form.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
         }
      }

      private void DiscussionsForm_Layout(object sender, LayoutEventArgs e)
      {
         repositionControls();
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
         Trace.TraceInformation(String.Format(
            "[DiscussionsForm] Loading discussions. Hostname: {0}, Project: {1}, MR IId: {2}",
               _mergeRequestDescriptor.HostName, _mergeRequestDescriptor.ProjectName, _mergeRequestDescriptor.IId));

         this.Text = DefaultCaption + "   (Checking for new commits)";
         await _updateGitRepository(_mergeRequestDescriptor);

         this.Text = DefaultCaption + "   (Loading discussions)";

         List<Discussion> discussions = null;
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

      private bool renderDiscussions(List<Discussion> discussions)
      {
         updateLayout(discussions);
         Focus(); // Set focus to the Form
         return discussions != null && Controls.Cast<Control>().Any((x) => x is DiscussionBox);
      }

      private void updateLayout(List<Discussion> discussions)
      {
         this.Text = DefaultCaption + "   (Rendering)";

         SuspendLayout();

         if (discussions != null)
         {
            for (int iBox = Controls.Count - 1; iBox >= 0; --iBox)
            {
               if (Controls[iBox] is DiscussionBox)
               {
                  Controls.RemoveAt(iBox);
               }
            }

            createDiscussionBoxes(discussions);
         }

         // Reposition controls before updating their visiblity to avoid flickering
         repositionControls();

         // Un-hide controls that should be visible now
         updateVisibilityOfBoxes();

         AdjustFormScrollbars(true);

         ResumeLayout(false /* don't need immediate re-layout, everything is already ok */);

         this.Text = DefaultCaption;
      }

      private void updateVisibilityOfBoxes()
      {
         foreach (Control control in Controls)
         {
            if (control is DiscussionBox box)
            {
               box.Visible = DisplayFilter.DoesMatchFilter(box.Discussion);
            }
         }
      }

      private void createDiscussionBoxes(List<Discussion> discussions)
      {
         foreach (var discussion in discussions)
         {
            if (!SystemFilter.DoesMatchFilter(discussion))
            {
               continue;
            }

            DiscussionEditor editor = _manager.GetDiscussionEditor(_mergeRequestDescriptor, discussion.Id);
            DiscussionBox box = new DiscussionBox(discussion, editor, _mergeRequestAuthor, _currentUser,
               _diffContextDepth, _gitRepository, _colorScheme,
               (sender) =>
               {
                  SuspendLayout();
                  sender.Visible = false; // to avoid flickering on repositioning
               }, (sender) => updateLayout(null))
            {

               // Let new boxes be hidden to avoid flickering on repositioning
               Visible = false
            };
            Controls.Add(box);
         }
      }

      private void repositionControls()
      {
         int groupBoxMarginLeft = 5;
         int groupBoxMarginTop = 5;

         // If Vertical Scroll is visible, its width is already excluded from ClientSize.Width
         int vscrollDelta = VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth;

         // Stack panels horizontally
         FilterPanel.Location = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         ActionsPanel.Location = new Point(
            FilterPanel.Location.X + FilterPanel.Size.Width + groupBoxMarginLeft, groupBoxMarginTop);

         // Stack boxes vertically
         Point previousBoxLocation = new Point(0,
            Math.Max(FilterPanel.Location.Y + FilterPanel.Size.Height,
                     ActionsPanel.Location.Y + ActionsPanel.Size.Height));
         Size previousBoxSize = new Size();

         foreach (Control control in Controls)
         {
            if (!(control is DiscussionBox box))
            {
               continue;
            }

            // Check if this box will be visible or not. The same condition as in updateVisibilityOfBoxes().
            // Cannot check Visible property because it is not set so far, we're trying to avoid flickering.
            if (!DisplayFilter.DoesMatchFilter(box.Discussion))
            {
               continue;
            }

            box.Location = new Point
            {
               X = groupBoxMarginLeft,
               Y = previousBoxLocation.Y + previousBoxSize.Height + groupBoxMarginTop
            };

            previousBoxLocation = box.Location;

            // Discussion box can take all the width except scroll bars and the left margin
            previousBoxSize = box.AdjustToWidth(ClientSize.Width - vscrollDelta - groupBoxMarginLeft);
         }

         // Apply scroll bar offset
         foreach (Control control in Controls)
         {
            control.Location = new Point
            {
               X = control.Location.X - HorizontalScroll.Value,
               Y = control.Location.Y - VerticalScroll.Value
            };
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
      private readonly Func<MergeRequestDescriptor, Task> _updateGitRepository;

      private readonly DiscussionFilterPanel FilterPanel;
      private readonly DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private readonly DiscussionFilter SystemFilter; // filters out discussions with System notes

      private readonly DiscussionActionsPanel ActionsPanel;
   }

   internal class NoDiscussionsToShow : ArgumentException { }; 
}

