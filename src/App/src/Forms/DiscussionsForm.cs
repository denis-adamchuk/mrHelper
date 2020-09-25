﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.App.Controls;
using mrHelper.Common.Exceptions;
using mrHelper.StorageSupport;
using mrHelper.App.Helpers.GitLab;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Forms
{
   internal partial class DiscussionsForm : CustomFontForm
   {
      /// <summary>
      /// Throws:
      /// </summary>
      internal DiscussionsForm(
         DataCache dataCache, GitLabInstance gitLabInstance, IModificationListener modificationListener,
         IGitCommandService git, User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         string mergeRequestTitle, User mergeRequestAuthor,
         int diffContextDepth, ColorScheme colorScheme,
         Func<MergeRequestKey, IEnumerable<Discussion>, Task> updateGit, Action onDiscussionModified)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mergeRequestTitle;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;

         _git = git;
         _diffContextDepth = diffContextDepth;

         _colorScheme = colorScheme;

         _dataCache = dataCache;
         _gitLabInstance = gitLabInstance;
         _modificationListener = modificationListener;
         _updateGit = updateGit;
         _onDiscussionModified = onDiscussionModified;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         createPanels();

         applyFont(Program.Settings.MainWindowFontSizeName);
         applyTheme(Program.Settings.VisualThemeName);

         if (!renderDiscussions(Array.Empty<Discussion>(), discussions))
         {
            throw new NoDiscussionsToShow();
         }
         _discussions = discussions;

         // Make some boxes visible. This does not paint them because their parent (Form) is hidden so far.
         updateVisibilityOfBoxes();
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         // Form is visible now and controls will be drawn inside base.OnVisibleChanged() call.
         // Before drawing anything, let's put controls at their places.
         // Note that we have to postpone onLayoutUpdate() till this moment because before this moment ClientSize
         // Width obtains some intermediate values.
         onLayoutUpdate();

         base.OnVisibleChanged(e);
      }

      private void DiscussionsForm_Shown(object sender, EventArgs e)
      {
         // Subscribe to layout changes during Form lifetime
         this.Layout += this.DiscussionsForm_Layout;
      }

      private void DiscussionsForm_Layout(object sender, LayoutEventArgs e)
      {
         // TODO WTF Calculate number of this calls
         onLayoutUpdate();
      }

      private void DiscussionsForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         // No longer need to process Layout changes
         this.Layout -= this.DiscussionsForm_Layout;
      }

      private void createPanels()
      {
         SystemFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, DiscussionFilterState.AllExceptSystem);
         DisplayFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor,
            DiscussionFilterState.Default);
         FilterPanel = new DiscussionFilterPanel(DisplayFilter.Filter, onFilterChanged);

         DisplaySort = new DiscussionSort(DiscussionSortState.Default);
         SortPanel = new DiscussionSortPanel(DisplaySort.SortState, onSortChanged);

         ActionsPanel = new DiscussionActionsPanel(onRefreshAction, onAddCommentAction, onAddThreadAction,
            _mergeRequestKey, Program.Settings);
         BottomActionsPanel = new DiscussionActionsPanel(onRefreshAction, onAddCommentAction, onAddThreadAction,
            _mergeRequestKey, Program.Settings);
         SearchPanel = new DiscussionSearchPanel(onFind);
         FontSelectionPanel = new DiscussionFontSelectionPanel(font => applyFont(font));

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);
         Controls.Add(BottomActionsPanel);
         Controls.Add(SortPanel);
         Controls.Add(SearchPanel);
         Controls.Add(FontSelectionPanel);
      }

      private void onSortChanged()
      {
         DisplaySort.SortState = SortPanel.SortState;
         PerformLayout(); // Recalculate locations of child controls
         updateSearch();
      }

      private void onFind(SearchQuery query, bool forward)
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
      }

      private void onRefreshAction()
      {
         BeginInvoke(new Action(async () => await onRefresh()));
      }

      private void onAddThreadAction()
      {
         BeginInvoke(new Action(async () =>
         {
            await DiscussionHelper.AddThreadAsync(
               _mergeRequestKey, _mergeRequestTitle, _modificationListener, _currentUser, _dataCache);
            onRefreshAction();
         }));
      }

      private void onAddCommentAction()
      {
         BeginInvoke(new Action(async () =>
         {
            await DiscussionHelper.AddCommentAsync(
               _mergeRequestKey, _mergeRequestTitle, _modificationListener, _currentUser);
            onRefreshAction();
         }));
      }

      private void onFilterChanged()
      {
         DisplayFilter.Filter = FilterPanel.Filter;
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places
         updateSearch();
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

         // Avoid repositioning child controls on box removing, creation and visibility change
         SuspendLayout(); 

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync();

         IEnumerable<Discussion> updatedDiscussions = discussions
            .Where(discussion =>
            {
               Discussion cachedDiscussion = _discussions.SingleOrDefault(d => d.Id == discussion.Id);
               if (cachedDiscussion == null)
               {
                  return true;
               }

               bool isResolved(Discussion d) => d.Notes.Any(note => note.Resolvable && !note.Resolved);
               DateTime getTimestamp(Discussion d) => discussion.Notes.Select(note => note.Updated_At).Max();

               return isResolved(cachedDiscussion) != isResolved(discussion)
                   || getTimestamp(cachedDiscussion) < getTimestamp(discussion);
            })
            .ToArray(); // force immediate execution

         IEnumerable<Discussion> deletedDiscussions = _discussions
            .Where(cachedDiscussion =>
            {
               var discussion = discussions.SingleOrDefault(d => d.Id == cachedDiscussion.Id);
               return discussion == null;
            })
            .ToArray(); // force immediate execution

         // Some controls are deleted here and some new are created. New controls are invisible.
         if (!renderDiscussions(deletedDiscussions, updatedDiscussions))
         {
            MessageBox.Show("No discussions to show. Press OK to close form.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
         }

         _discussions = discussions;

         // Make boxes that match user filter visible
         updateVisibilityOfBoxes();

         // Put all child controls at their places
         ResumeLayout(true);

         updateSearch();
      }

      protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
      {
         // https://nickstips.wordpress.com/2010/03/03/c-panel-resets-scroll-position-after-focus-is-lost-and-regained/
         // Returning the current location prevents the Form from
         // scrolling to the active control when the Form loses and regains focus
         return DisplayRectangle.Location;
      }

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync()
      {
         if (_dataCache?.DiscussionCache == null)
         {
            return null;
         }

         Trace.TraceInformation(String.Format(
            "[DiscussionsForm] Loading discussions. Hostname: {0}, Project: {1}, MR IId: {2}",
               _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId));

         IEnumerable<Discussion> discussions;
         try
         {
            this.Text = DefaultCaption + "   (Loading discussions)";
            discussions = await _dataCache.DiscussionCache.LoadDiscussions(_mergeRequestKey);
         }
         catch (DiscussionCacheException ex)
         {
            string message = "Cannot load discussions from GitLab";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
         }
         finally
         {
            this.Text = DefaultCaption;
         }

         if (discussions == null)
         {
            return null;
         }

         this.Text = DefaultCaption + "   (Checking for new commits)";
         await _updateGit(_mergeRequestKey, discussions);
         this.Text = DefaultCaption;

         return discussions;
      }

      private bool renderDiscussions(
         IEnumerable<Discussion> deletedDiscussions,
         IEnumerable<Discussion> updatedDiscussions)
      {
         IEnumerable<Control> affectedControls =
            getControlsAffectedByDiscussionChanges(deletedDiscussions, updatedDiscussions);
         foreach (Control control in affectedControls)
         {
            Controls.Remove(control);
         }

         this.Text = DefaultCaption + "   (Rendering)";
         createDiscussionBoxes(updatedDiscussions);
         this.Text = DefaultCaption;

         Focus(); // Set focus to the Form
         return Controls.Cast<Control>().Any((x) => x is DiscussionBox);
      }

      private IEnumerable<Control> getControlsAffectedByDiscussionChanges(
         IEnumerable<Discussion> deletedDiscussions,
         IEnumerable<Discussion> updatedDiscussions)
      {
         return Controls
            .Cast<Control>()
            .Where(control => control is DiscussionBox)
            .Where(control =>
            {
               bool doesControlHoldAnyOfDiscussions(IEnumerable<Discussion> discussions) =>
                  discussions.Any(discussion => (control as DiscussionBox).Discussion.Id == discussion.Id);
               return doesControlHoldAnyOfDiscussions(deletedDiscussions)
                   || doesControlHoldAnyOfDiscussions(updatedDiscussions);
            })
            .ToArray(); // force immediate execution
      }

      private void onLayoutUpdate()
      {
         // Reposition child controls
         repositionControls();

         // Updates Scroll Bars and also updates Location property of controls in accordance with new AutoScrollPosition
         AdjustFormScrollbars(true);
      }

      private void updateVisibilityOfBoxes()
      {
         foreach (DiscussionBox box in getAllBoxes())
         {
            bool isAllowedToDisplay = DisplayFilter.DoesMatchFilter(box.Discussion);
            // Note that the following does not change Visible property value until Form gets Visible itself
            box.Visible = isAllowedToDisplay;
         }
      }

      private IEnumerable<DiscussionBox> getAllBoxes()
      {
         return Controls
            .Cast<Control>()
            .Where(x => x is DiscussionBox)
            .Cast<DiscussionBox>()
            .ToArray(); // force immediate execution
      }

      private IEnumerable<DiscussionBox> getVisibleBoxes()
      {
         // Check if this box will be visible or not. The same condition as in updateVisibilityOfBoxes().
         // Cannot check Visible property because it is not set so far, we're trying to avoid flickering.
         return getAllBoxes()
            .Where(box => DisplayFilter.DoesMatchFilter(box.Discussion))
            .ToArray(); // force immediate execution
      }

      private IEnumerable<DiscussionBox> getVisibleAndSortedBoxes()
      {
         return DisplaySort.Sort(getVisibleBoxes(), x => x.Discussion.Notes);
      }

      private IEnumerable<ITextControl> getAllVisibleAndSortedTextControls()
      {
         return getVisibleAndSortedBoxes()
            .SelectMany(box => box.Controls.Cast<Control>())
            .Where(control => control is ITextControl)
            .Cast<ITextControl>()
            .ToArray(); // force immediate execution
      }

      private void highlightSearchResult(TextSearchResult? result)
      {
         TextSearchResult = null;
         if (result.HasValue && TextSearch != null)
         {
            result.Value.Control.HighlightFragment(result.Value.InsideControlPosition, TextSearch.Query.Text.Length);

            Control control = (result.Value.Control as Control);
            control.Focus();

            Point controlLocationAtScreen = control.PointToScreen(new Point(0, -5));
            Point controlLocationAtForm = this.PointToClient(controlLocationAtScreen);

            if (!ClientRectangle.Contains(controlLocationAtForm))
            {
               int x = AutoScrollPosition.X;
               int y = VerticalScroll.Value + controlLocationAtForm.Y;
               Point newPosition = new Point(x, y);
               AutoScrollPosition = newPosition;
               PerformLayout();
            }

            TextSearchResult = result;
         }
      }

      private void createDiscussionBoxes(IEnumerable<Discussion> discussions)
      {
         foreach (Discussion discussion in discussions)
         {
            if (!SystemFilter.DoesMatchFilter(discussion))
            {
               continue;
            }

            SingleDiscussionAccessor accessor = Shortcuts.GetSingleDiscussionAccessor(
               _gitLabInstance, _modificationListener, _mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(this, accessor, _git, _currentUser,
               _mergeRequestKey.ProjectKey, discussion, _mergeRequestAuthor,
               _diffContextDepth, _colorScheme, onDiscussionBoxContentChanged, onDiscussionBoxContentChanging,
               sender => MostRecentFocusedDiscussionControl = sender,
               _htmlTooltip)
            {
               // Let new boxes be hidden to avoid flickering on repositioning
               Visible = false
            };
            Controls.Add(box);
         }
      }

      private void onDiscussionBoxContentChanging(DiscussionBox sender)
      {
         SuspendLayout(); // Avoid repositioning child controls on changing sender visibility
         sender.Visible = true;
         ResumeLayout(true); // Put child controls at their places
         updateSearch();
         _onDiscussionModified?.Invoke();
      }

      private void onDiscussionBoxContentChanged(DiscussionBox sender)
      {
         SuspendLayout(); // Avoid repositioning child controls on changing sender visibility
         sender.Visible = false; // hide sender to avoid flickering on repositioning
         ResumeLayout(false); // Don't perform layout immediately, will be done in next callback
      }

      private void repositionControls()
      {
         int groupBoxMarginLeft = 5;
         int groupBoxMarginTop = 5;
         int bottomActionsPanelMarginTop = 200;

         // Temporary variables to avoid changing control Location more than once
         Point filterPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point sortPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point fontSelectionPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point actionsPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point searchPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);

         filterPanelLocation.Offset(actionsPanelLocation.X + ActionsPanel.Size.Width, 0);
         sortPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width, 0);
         fontSelectionPanelLocation.Offset(sortPanelLocation.X + SortPanel.Size.Width, 0);
         searchPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width,
                                    Math.Max(sortPanelLocation.Y + SortPanel.Size.Height,
                                             fontSelectionPanelLocation.Y + FontSelectionPanel.Size.Height));

         // Stack panels horizontally
         FilterPanel.Location = filterPanelLocation + (Size)AutoScrollPosition;
         SortPanel.Location = sortPanelLocation + (Size)AutoScrollPosition;
         FontSelectionPanel.Location = fontSelectionPanelLocation + (Size)AutoScrollPosition;
         ActionsPanel.Location = actionsPanelLocation + (Size)AutoScrollPosition;
         SearchPanel.Location = searchPanelLocation + (Size)AutoScrollPosition;

         // A hack to show right border of FontSelectionPanel at the same X coordinate as the right border of SearchPanel
         FontSelectionPanel.Width = SearchPanel.Location.X + SearchPanel.Width - FontSelectionPanel.Location.X;

         // A hack to show bottom border of SearchPanel at the same Y coordinate as the bottom border of FilterPanel
         SearchPanel.Height = FilterPanel.Location.Y + FilterPanel.Height - SearchPanel.Location.Y;

         // Prepare to stack boxes vertically
         int topOffset = Math.Max(filterPanelLocation.Y + FilterPanel.Size.Height,
                                  searchPanelLocation.Y + SearchPanel.Size.Height);
         Size previousBoxSize = new Size();
         Point previousBoxLocation = new Point();
         previousBoxLocation.Offset(0, topOffset);

         // Stack boxes vertically
         foreach (DiscussionBox box in getVisibleAndSortedBoxes())
         {
            // Temporary variable to void changing box Location more than once
            Point location = new Point(groupBoxMarginLeft, groupBoxMarginTop);
            location.Offset(0, previousBoxLocation.Y + previousBoxSize.Height);

            // If Vertical Scroll is visible, its width is already excluded from ClientSize.Width
            int vscrollDelta = VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth;

            // Discussion box can take all the width except scroll bars and the left margin
            box.AdjustToWidth(ClientSize.Width - vscrollDelta - groupBoxMarginLeft);

            box.Location = location + (Size)AutoScrollPosition;
            previousBoxLocation = location;
            previousBoxSize = box.Size;
         }

         Point bottomActionsPanelLocation = new Point(groupBoxMarginLeft, bottomActionsPanelMarginTop);
         bottomActionsPanelLocation.Offset(0, previousBoxLocation.Y + previousBoxSize.Height);
         BottomActionsPanel.Location = bottomActionsPanelLocation + (Size)AutoScrollPosition;
      }

      private void startSearch(SearchQuery query, bool highlight)
      {
         resetSearch();

         TextSearch = new TextSearch(getAllVisibleAndSortedTextControls(), query);
         TextSearchResult? result = TextSearch.FindFirst(out int count);
         SearchPanel.DisplayFoundCount(count);

         if (highlight)
         {
            highlightSearchResult(result);
         }
      }

      private void continueSearch(bool forward)
      {
         if (TextSearch == null)
         {
            return;
         }

         int startPosition = 0;
         Control control = MostRecentFocusedDiscussionControl ?? ActiveControl;
         if (control is ITextControl textControl && textControl.HighlightState != null)
         {
            startPosition = forward
               ? textControl.HighlightState.HighlightStart + textControl.HighlightState.HighlightLength
               : textControl.HighlightState.HighlightStart ;
            textControl.ClearHighlight();
         }

         TextSearchResult? result = forward
            ? TextSearch.FindNext(control, startPosition)
            : TextSearch.FindPrev(control, startPosition);

         if (result != null)
         {
            highlightSearchResult(result);
         }
      }

      private void resetSearch()
      {
         TextSearch = null;
         SearchPanel.DisplayFoundCount(null);
         MostRecentFocusedDiscussionControl = null;
         TextSearchResult?.Control.ClearHighlight();
         TextSearchResult = null;
      }

      private void updateSearch()
      {
         if (TextSearch != null)
         {
            startSearch(TextSearch.Query, false);
         }
      }

      private string DefaultCaption
      {
         get
         {
            return String.Format("Discussions for merge request \"{0}\"", _mergeRequestTitle);
         }
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private readonly IGitCommandService _git;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private readonly User _currentUser;
      private readonly DataCache _dataCache;
      private readonly GitLabInstance _gitLabInstance;
      private readonly IModificationListener _modificationListener;
      private readonly Func<MergeRequestKey, IEnumerable<Discussion>, Task> _updateGit;
      private readonly Action _onDiscussionModified;

      private DiscussionFilterPanel FilterPanel;
      private DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private DiscussionFilter SystemFilter; // filters out discussions with System notes

      private DiscussionActionsPanel ActionsPanel;
      private DiscussionActionsPanel BottomActionsPanel;

      private DiscussionSearchPanel SearchPanel;
      private TextSearch TextSearch;
      private TextSearchResult? TextSearchResult;

      private DiscussionSortPanel SortPanel;
      private DiscussionSort DisplaySort;

      private DiscussionFontSelectionPanel FontSelectionPanel;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control MostRecentFocusedDiscussionControl;
      private IEnumerable<Discussion> _discussions;

      private readonly HtmlToolTip _htmlTooltip = new HtmlToolTip
      {
         AutoPopDelay = 20000, // 20s
         InitialDelay = 300,
         // BaseStylesheet = Don't specify anything here because users' HTML <style> override it
      };

   }

   internal class NoDiscussionsToShow : Exception { };
}

