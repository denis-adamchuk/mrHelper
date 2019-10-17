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
      internal DiscussionsForm(MergeRequestKey mrk, string mrTitle, User mergeRequestAuthor,
         IGitRepository gitRepository, int diffContextDepth, ColorScheme colorScheme, List<Discussion> discussions,
         DiscussionManager manager, User currentUser, Func<MergeRequestKey, Task> updateGitRepository)
      {
         _mergeRequestKey = mrk;
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
               updateLayout(null, true);
            });
         ActionsPanel = new DiscussionActionsPanel(() => BeginInvoke(new Action(async () => await onRefresh())));
         TextSearch = new TextSearch(this,
            (control) =>
            {
               return control is TextBox &&
                  (control.Parent is DiscussionBox box && DisplayFilter.DoesMatchFilter(box.Discussion));
            });
         SearchPanel = new DiscussionSearchPanel(
            (text, forward) =>
            {
               unhighlightSearchResult();
               if (text != _searchText)
               {
                  _searchText = text;
                  _searchResults = TextSearch.Search(text, forward);
               }
               else
               {
                  _searchResults.MoveNext(forward);
               }
               highlightSearchResult();
               return _searchResults.Count;
            },
            () =>
            {
               resetSearch();
            });

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);
         Controls.Add(SearchPanel);

         if (!renderDiscussions(discussions, false))
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
         else if (e.KeyCode == Keys.F3)
         {
            unhighlightSearchResult();
            _searchResults.MoveNext(!e.Modifiers.HasFlag(Keys.Shift));
            highlightSearchResult();
         }
         else if (e.KeyCode == Keys.Escape)
         {
            resetSearch();
         }
         else if (e.KeyCode == Keys.Home)
         {
            if (!(ActiveControl is TextBox) && !(ActiveControl is DiscussionSearchPanel))
            {
               AutoScrollPosition = new Point(AutoScrollPosition.X, VerticalScroll.Minimum);
               PerformLayout();
               e.Handled = true;
            }
         }
         else if (e.KeyCode == Keys.End)
         {
            if (!(ActiveControl is TextBox) && !(ActiveControl is DiscussionSearchPanel))
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

         if (!renderDiscussions(await loadDiscussionsAsync(), true))
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
               _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId));

         this.Text = DefaultCaption + "   (Checking for new commits)";
         await _updateGitRepository(_mergeRequestKey);

         this.Text = DefaultCaption + "   (Loading discussions)";

         List<Discussion> discussions = null;
         try
         {
            discussions = await _manager.GetDiscussionsAsync(_mergeRequestKey);
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

      private bool renderDiscussions(List<Discussion> discussions, bool needReposition)
      {
         updateLayout(discussions, needReposition);
         Focus(); // Set focus to the Form
         return discussions != null && Controls.Cast<Control>().Any((x) => x is DiscussionBox);
      }

      private void updateLayout(List<Discussion> discussions, bool needReposition)
      {
         resetSearch();

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

         if (needReposition)
         {
            // Reposition controls before updating their visibility to avoid flickering
            repositionControls();
         }

         // Un-hide controls that should be visible now
         updateVisibilityOfBoxes();

         // Updates Scroll Bars and also updates Location property of controls in accordance with new AutoScrollPosition
         AdjustFormScrollbars(true);

         ResumeLayout(false /* don't need immediate re-layout */);

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

      private void highlightSearchResult()
      {
         if (_searchResults.Current.Control is TextBox textbox)
         {
            textbox.Select(_searchResults.Current.InsideControlPosition, _searchText.Length);
            textbox.Focus();

            Point controlLocationAtScreen = textbox.PointToScreen(new Point(0, -5));
            Point controlLocationAtForm = this.PointToClient(controlLocationAtScreen);

            if (!ClientRectangle.Contains(controlLocationAtForm))
            {
               Point newPosition = new Point(AutoScrollPosition.X, VerticalScroll.Value + controlLocationAtForm.Y);
               AutoScrollPosition = newPosition;
               PerformLayout();
            }

         }
      }

      private void unhighlightSearchResult()
      {
         if (_searchResults.Current.Control is TextBox textbox)
         {
            textbox.SelectionLength = 0;
         }
      }

      private void resetSearch()
      {
         unhighlightSearchResult();
         _searchResults = default(SearchResults<TextSearchResult>);
         _searchText = String.Empty;
         SearchPanel.Reset();
      }

      private void createDiscussionBoxes(List<Discussion> discussions)
      {
         foreach (var discussion in discussions)
         {
            if (!SystemFilter.DoesMatchFilter(discussion))
            {
               continue;
            }

            DiscussionEditor editor = _manager.GetDiscussionEditor(_mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(discussion, editor, _mergeRequestAuthor, _currentUser,
               _diffContextDepth, _gitRepository, _colorScheme,
               (sender) =>
               {
                  SuspendLayout();
                  sender.Visible = false; // to avoid flickering on repositioning
               }, (sender) => updateLayout(null, true))
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

         // Temporary variables to avoid changing control Location more than once
         Point filterPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point actionsPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point searchPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         actionsPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width, 0);
         searchPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width,
                                    actionsPanelLocation.Y + ActionsPanel.Size.Height);

         // Stack panels horizontally
         FilterPanel.Location = filterPanelLocation + (Size)AutoScrollPosition;
         ActionsPanel.Location = actionsPanelLocation + (Size)AutoScrollPosition;
         SearchPanel.Location = searchPanelLocation + (Size)AutoScrollPosition;

         // Prepare to stack boxes vertically
         int topOffset = Math.Max(filterPanelLocation.Y + FilterPanel.Size.Height,
                         Math.Max(actionsPanelLocation.Y + ActionsPanel.Size.Height,
                                  searchPanelLocation.Y + SearchPanel.Size.Height));
         Size previousBoxSize = new Size();
         Point previousBoxLocation = new Point();
         previousBoxLocation.Offset(0, topOffset);

         // Stack boxes vertically
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

            // Temporary variable to void changing box Location more than once
            Point location = new Point(groupBoxMarginLeft, groupBoxMarginTop);
            location.Offset(0, previousBoxLocation.Y + previousBoxSize.Height);

            // Discussion box can take all the width except scroll bars and the left margin
            box.AdjustToWidth(ClientSize.Width - vscrollDelta - groupBoxMarginLeft);

            box.Location = location + (Size)AutoScrollPosition;
            previousBoxLocation = location;
            previousBoxSize = box.Size;
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

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private readonly IGitRepository _gitRepository;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private User _currentUser;
      private readonly DiscussionManager _manager;
      private readonly Func<MergeRequestKey, Task> _updateGitRepository;

      private readonly DiscussionFilterPanel FilterPanel;
      private readonly DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private readonly DiscussionFilter SystemFilter; // filters out discussions with System notes

      private readonly DiscussionActionsPanel ActionsPanel;

      private readonly DiscussionSearchPanel SearchPanel;
      private readonly TextSearch TextSearch;
      private SearchResults<TextSearchResult> _searchResults;
      private string _searchText;
   }

   internal class NoDiscussionsToShow : ArgumentException { }; 
}

