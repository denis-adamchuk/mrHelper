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
using mrHelper.Client.Git;

namespace mrHelper.App.Forms
{
   internal partial class DiscussionsForm : Form
   {
      /// <summary>
      /// Throws:
      /// ArgumentException
      /// </summary>
      internal DiscussionsForm(MergeRequestKey mrk, string mrTitle, User mergeRequestAuthor,
         GitClient gitClient, int diffContextDepth, ColorScheme colorScheme, List<Discussion> discussions,
         DiscussionManager manager, User currentUser, Func<MergeRequestKey, Task<IGitRepository>> updateGitRepository)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mrTitle;
         _mergeRequestAuthor = mergeRequestAuthor;

         if (gitClient != null)
         {
            gitClient.Disposed += client => onGitClientDisposed(client);
         }
         _gitRepository = gitClient;
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
               ServiceMessages = true,
               ByAnswers = FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               ByResolution = FilterByResolution.Resolved | FilterByResolution.NotResolved
            };

         DisplayFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, state);
         SystemFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, state);

         FilterPanel = new DiscussionFilterPanel(DisplayFilter.Filter,
            () =>
            {
               DisplayFilter.Filter = FilterPanel.Filter;
               updateLayout(null, true, true);
               updateSearch();
            });

         ActionsPanel = new DiscussionActionsPanel(() => BeginInvoke(new Action(async () => await onRefresh())));

         SearchPanel = new DiscussionSearchPanel(
            (query, forward) =>
            {
               if (query.Text == String.Empty)
               {
                  resetSearch();
               }
               else if (TextSearch == null || !query.Equals(TextSearch.Query))
               {
                  startSearch(query, true);
               }
               else
               {
                  MostRecentFocusedDiscussionControl?.Focus();
                  continueSearch(forward);
               }
            });

         DiscussionSortState sortState = DiscussionSortState.Default;
         DisplaySort = new DiscussionSort(sortState);
         SortPanel = new DiscussionSortPanel(DisplaySort.SortState,
            () =>
            {
               DisplaySort.SortState = SortPanel.SortState;
               updateLayout(null, true, true);
               updateSearch();
            });

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);
         Controls.Add(SearchPanel);
         Controls.Add(SortPanel);

         applyTheme(Program.Settings.VisualThemeName);

         if (!renderDiscussions(discussions, false))
         {
            throw new NoDiscussionsToShow();
         }
      }

      private void applyTheme(string theme)
      {
         if (theme == "New Year 2020")
         {
            pictureBox1.BackgroundImage = mrHelper.App.Properties.Resources.HappyNY2020;
            pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox1.Visible = true;
            pictureBox1.SendToBack();
         }
         else
         {
            pictureBox1.Visible = false;
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
            continueSearch(!e.Modifiers.HasFlag(Keys.Shift));
         }
         else if (e.KeyCode == Keys.Escape)
         {
            // to not pass Escape keystroke to a textbox being edited
            if (!isEditing())
            {
               resetSearch();
            }
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

      private void onGitClientDisposed(GitClient client)
      {
         client.Disposed -= onGitClientDisposed;
         if (IsHandleCreated)
         {
            BeginInvoke(new Action(async () => await onRefresh()));
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

         updateSearch();
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
         _gitRepository = await _updateGitRepository(_mergeRequestKey);

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
         updateLayout(discussions, needReposition, true);
         Focus(); // Set focus to the Form
         return discussions != null && Controls.Cast<Control>().Any((x) => x is DiscussionBox);
      }

      private void updateLayout(List<Discussion> discussions, bool needReposition, bool suspendLayout)
      {
         this.Text = DefaultCaption + "   (Rendering)";

         if (suspendLayout)
         {
            SuspendLayout();
         }

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
         IEnumerable<DiscussionBox> boxes = Controls
            .Cast<Control>()
            .Where(x => x is DiscussionBox)
            .Cast<DiscussionBox>();

         boxes.ToList().ForEach(x => x.Visible = DisplayFilter.DoesMatchFilter(x.Discussion));
      }

      private void highlightSearchResult(TextSearchResult? result)
      {
         if (result.HasValue && result.Value.Control is TextBox textbox && TextSearch != null)
         {
            textbox.Select(result.Value.InsideControlPosition, TextSearch.Query.Text.Length);
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
               // pre-content-change
               (sender) =>
               {
                  SuspendLayout();
                  sender.Visible = false; // to avoid flickering on repositioning
               },
               // post-content-change
               (sender, lite) =>
               {
                  // 'lite' means that there were no a preceding PreContentChange event, so we did not suspend layout
                  updateLayout(null, true, lite);
                  updateSearch();
               },
               sender => MostRecentFocusedDiscussionControl = sender)
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
         Point sortPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point actionsPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point searchPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);

         sortPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width, 0);
         actionsPanelLocation.Offset(sortPanelLocation.X + SortPanel.Size.Width, 0);
         searchPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width,
                                    Math.Max(actionsPanelLocation.Y + ActionsPanel.Size.Height,
                                            (sortPanelLocation.Y + SortPanel.Size.Height)));

         // Stack panels horizontally
         FilterPanel.Location = filterPanelLocation + (Size)AutoScrollPosition;
         SortPanel.Location = sortPanelLocation + (Size)AutoScrollPosition;
         ActionsPanel.Location = actionsPanelLocation + (Size)AutoScrollPosition;
         SearchPanel.Location = searchPanelLocation + (Size)AutoScrollPosition;

         // Prepare to stack boxes vertically
         int topOffset = Math.Max(filterPanelLocation.Y + FilterPanel.Size.Height,
                                  searchPanelLocation.Y + SearchPanel.Size.Height);
         Size previousBoxSize = new Size();
         Point previousBoxLocation = new Point();
         previousBoxLocation.Offset(0, topOffset);

         // Filter out boxes
         IEnumerable<DiscussionBox> boxes = Controls
            .Cast<Control>()
            .Where(x => x is DiscussionBox)
            .Cast<DiscussionBox>();

         // Sort boxes
         IEnumerable<DiscussionBox> sortedBoxes = DisplaySort.Sort(boxes, x => x.Discussion.Notes);

         // Stack boxes vertically
         foreach (DiscussionBox box in sortedBoxes)
         {
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

      private void startSearch(SearchQuery query, bool highlight)
      {
         resetSearch();

         TextSearch = new TextSearch(this, query, control => isSearchableControl(control));

         TextSearchResult? result = TextSearch.FindFirst(out int count);
         SearchPanel.DisplayFoundCount(count);

         if (highlight)
         {
            highlightSearchResult(result);
         }
      }

      private void continueSearch(bool forward)
      {
         // To not jump inside the current control when it is being edited
         if (isEditing())
         {
            return;
         }

         if (TextSearch != null)
         {
            Control startControl = MostRecentFocusedDiscussionControl ?? ActiveControl;

            TextSearchResult current = new TextSearchResult
            {
               Control = startControl,
               InsideControlPosition = ((startControl as TextBox)?.SelectionStart ?? 0)
                          + (forward ? ((startControl as TextBox)?.SelectionLength ?? 0) : 0)
            };

            highlightSearchResult(forward ? TextSearch.FindNext(current) : TextSearch.FindPrev(current));
         }
      }

      private void resetSearch()
      {
         TextSearch = null;
         SearchPanel.DisplayFoundCount(null);
         MostRecentFocusedDiscussionControl = null;
      }

      private void updateSearch()
      {
         // to not change search state in the middle of edit
         if (isEditing())
         {
            return;
         }

         if (TextSearch != null)
         {
            startSearch(TextSearch.Query, false);
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

      private bool isEditing()
      {
         return (ActiveControl is TextBox) && !(ActiveControl as TextBox).ReadOnly;
      }

      private bool isSearchableControl(Control control)
      {
         return control is TextBox &&
               (control.Parent is DiscussionBox box && DisplayFilter.DoesMatchFilter(box.Discussion));
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private IGitRepository _gitRepository;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private User _currentUser;
      private readonly DiscussionManager _manager;
      private readonly Func<MergeRequestKey, Task<IGitRepository>> _updateGitRepository;

      private readonly DiscussionFilterPanel FilterPanel;
      private readonly DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private readonly DiscussionFilter SystemFilter; // filters out discussions with System notes

      private readonly DiscussionActionsPanel ActionsPanel;

      private readonly DiscussionSearchPanel SearchPanel;
      private TextSearch TextSearch;

      private readonly DiscussionSortPanel SortPanel;
      private readonly DiscussionSort DisplaySort;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control MostRecentFocusedDiscussionControl;
   }

   internal class NoDiscussionsToShow : ArgumentException { }; 
}

