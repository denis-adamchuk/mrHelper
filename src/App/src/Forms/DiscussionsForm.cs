using System;
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
using mrHelper.Common.Constants;
using mrHelper.CustomActions;
using SearchQuery = mrHelper.App.Helpers.SearchQuery;

namespace mrHelper.App.Forms
{
   internal partial class DiscussionsForm : CustomFontForm, ICommandCallback
   {
      /// <summary>
      /// Throws:
      /// </summary>
      internal DiscussionsForm(
         DataCache dataCache,
         IGitCommandService git, User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         string mergeRequestTitle, User mergeRequestAuthor,
         ColorScheme colorScheme, Func<MergeRequestKey, IEnumerable<Discussion>, Task> updateGit,
         Action onDiscussionModified, string webUrl, Shortcuts shortcuts)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mergeRequestTitle;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;

         _git = git;

         _colorScheme = colorScheme;

         _dataCache = dataCache;
         _updateGit = updateGit;
         _onDiscussionModified = onDiscussionModified;
         _diffContextPosition = ConfigurationHelper.GetDiffContextPosition(Program.Settings);
         _discussionColumnWidth = ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings);
         _needShiftReplies = Program.Settings.NeedShiftReplies;
         _shortcuts = shortcuts;

         CustomCommandLoader loader = new CustomCommandLoader(this);
         try
         {
            _commands = loader.LoadCommands(Constants.CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            ExceptionHandlers.Handle("Cannot load custom actions", ex);
         }

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         linkLabelGitLabURL.Text = webUrl;
         toolTip.SetToolTip(linkLabelGitLabURL, webUrl);
         updateSaveDefaultLayoutState();

         createPanels();

         applyFont(Program.Settings.MainWindowFontSizeName);
         applyTheme(Program.Settings.VisualThemeName);

         Trace.TraceInformation(String.Format("[DiscussionsForm] Rendering discussion contexts for MR IId {0}...", mrk.IId));

         IEnumerable<Discussion> nonSystemDiscussions = discussions
            .Where(discussion => SystemFilter.DoesMatchFilter(discussion));
         if (!renderDiscussions(Array.Empty<Discussion>(), nonSystemDiscussions))
         {
            throw new NoDiscussionsToShow();
         }

         Trace.TraceInformation("[DiscussionsForm] Updating visibility of boxes...");

         // Make some boxes visible. This does not paint them because their parent (Form) is hidden so far.
         updateVisibilityOfBoxes();

         // Temporary benchmark
         Trace.TraceInformation("[DiscussionsForm] Visibility updated");

         subscribeToSettingsChange();
      }

      // Temporary benchmark
      protected override void OnHandleCreated(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnHandleCreated()...");
         base.OnHandleCreated(e);
      }

      // Temporary benchmark
      protected override void OnBindingContextChanged(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnBindingContextChanged()...");
         base.OnBindingContextChanged(e);
      }

      // Temporary benchmark
      protected override void OnLoad(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnLoad()...");
         base.OnLoad(e);
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         // Temporary benchmark
         Trace.TraceInformation(String.Format("[DiscussionsForm] Processing OnVisibleChanged({0})...", Visible.ToString()));

         if (Visible)
         {
            // Form is visible now and controls will be drawn inside base.OnVisibleChanged() call.
            // Before drawing anything, let's put controls at their places.
            // Note that we have to postpone onLayoutUpdate() till this moment because before this moment ClientSize
            // Width obtains some intermediate values.
            onLayoutUpdate();
         }

         base.OnVisibleChanged(e);
      }

      // Temporary benchmark
      protected override void OnActivated(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnActivated()...");
         base.OnActivated(e);
      }

      // Temporary benchmark
      protected override void OnShown(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnShown()...");
         base.OnShown(e);

         // By default, leave focus on a font selection control
         FontSelectionPanel.Focus();
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
         if (this.WindowState != FormWindowState.Minimized)
         {
            _previousState = this.WindowState;
         }
      }

      public void Restore()
      {
         this.WindowState = _previousState;
         Activate();
      }

      private void DiscussionsForm_Shown(object sender, EventArgs e)
      {
         // Subscribe to layout changes during Form lifetime
         this.Layout += this.DiscussionsForm_Layout;
      }

      private void DiscussionsForm_Layout(object sender, LayoutEventArgs e)
      {
         onLayoutUpdate();
      }

      private void DiscussionsForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         // No longer need to process Layout changes
         this.Layout -= this.DiscussionsForm_Layout;
      }

      private void linkLabelSaveAsDefaultLayout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         ConfigurationHelper.SetDiffContextPosition(Program.Settings, _diffContextPosition);
         ConfigurationHelper.SetDiscussionColumnWidth(Program.Settings, _discussionColumnWidth);
         Program.Settings.NeedShiftReplies = _needShiftReplies;
         updateSaveDefaultLayoutState();
      }

      private void createPanels()
      {
         SystemFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor, DiscussionFilterState.AllExceptSystem);
         DisplayFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor,
            DiscussionFilterState.Default);
         FilterPanel = new DiscussionFilterPanel(DisplayFilter.Filter, onFilterChanged);

         DisplaySort = new DiscussionSort(DiscussionSortState.Default);
         SortPanel = new DiscussionSortPanel(DisplaySort.SortState, onSortChanged);

         ActionsPanel = new DiscussionActionsPanel(onRefreshAction, onAddCommentAction, onAddThreadAction, _commands,
            onCommandAction);
         SearchPanel = new DiscussionSearchPanel(onFind, resetSearch);
         FontSelectionPanel = new DiscussionFontSelectionPanel(font => applyFont(font));

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);
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
            _mostRecentFocusedDiscussionControl?.Focus();
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
            Discussion discussion = await DiscussionHelper.AddThreadAsync(
               _mergeRequestKey, _mergeRequestTitle, _currentUser, _dataCache, _shortcuts);
            if (discussion != null)
            {
               renderDiscussionsWithSuspendedLayout(Array.Empty<Discussion>(), new Discussion[] { discussion });
            }
         }));
      }

      private void onAddCommentAction()
      {
         BeginInvoke(new Action(async () =>
         {
            await DiscussionHelper.AddCommentAsync(_mergeRequestKey, _mergeRequestTitle, _currentUser, _shortcuts);
            onRefreshAction();
         }));
      }

      private void onCommandAction(ICommand command)
      {
         BeginInvoke(new Action(async () =>
         {
            try
            {
               await command.Run();
            }
            catch (Exception ex) // Whatever happened in Run()
            {
               string errorMessage = "Custom action failed";
               ExceptionHandlers.Handle(errorMessage, ex);
               MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
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

      private void DiscussionsForm_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.F5)
         {
            onRefreshAction();
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
         else if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
         {
            setColumnWidth(ConfigurationHelper.GetNextColumnWidth(_discussionColumnWidth));
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
         {
            setColumnWidth(ConfigurationHelper.GetPrevColumnWidth(_discussionColumnWidth));
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Up && e.Modifiers.HasFlag(Keys.Control))
         {
            setDiffContextPosition(ConfigurationHelper.DiffContextPosition.Top);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Left && e.Modifiers.HasFlag(Keys.Control))
         {
            setDiffContextPosition(ConfigurationHelper.DiffContextPosition.Left);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Right && e.Modifiers.HasFlag(Keys.Control))
         {
            setDiffContextPosition(ConfigurationHelper.DiffContextPosition.Right);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Down && e.Modifiers.HasFlag(Keys.Control))
         {
            setNeedShiftReplies(!_needShiftReplies);
            e.Handled = true;
         }
      }

      private void linkLabelGitLabURL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         UrlHelper.OpenBrowser((sender as LinkLabel).Text);
      }

      private async Task onRefresh()
      {
         Trace.TraceInformation("[DiscussionsForm] Refreshing by user request");

         bool isResolved(Discussion d) => d.Notes.Any(note => note.Resolvable && !note.Resolved);
         DateTime getTimestamp(Discussion d) => d.Notes.Select(note => note.Updated_At).Max();
         int getNoteCount(Discussion d) => d.Notes.Count();

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync();
         if (discussions == null)
         {
            return;
         }

         IEnumerable<Discussion> nonSystemDiscussions = discussions
            .Where(discussion => SystemFilter.DoesMatchFilter(discussion));
         IEnumerable<Discussion> cachedDiscussions = getAllBoxes().Select(box => box.Discussion);

         IEnumerable<Discussion> updatedDiscussions = nonSystemDiscussions
            .Where(discussion =>
            {
               Discussion cachedDiscussion = cachedDiscussions.SingleOrDefault(d => d.Id == discussion.Id);
               return cachedDiscussion == null ||
                     (isResolved(cachedDiscussion) != isResolved(discussion)
                   || getTimestamp(cachedDiscussion) != getTimestamp(discussion)
                   || getNoteCount(cachedDiscussion) != getNoteCount(discussion));
            })
            .ToArray(); // force immediate execution

         IEnumerable<Discussion> deletedDiscussions = cachedDiscussions
            .Where(cachedDiscussion => nonSystemDiscussions.SingleOrDefault(d => d.Id == cachedDiscussion.Id) == null)
            .ToArray(); // force immediate execution

         if (deletedDiscussions.Any() || updatedDiscussions.Any())
         {
            renderDiscussionsWithSuspendedLayout(deletedDiscussions, updatedDiscussions);
            updateSearch();
         }
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
            "[DiscussionsForm] Loading discussions. Hostname: {0}, Project: {1}, MR IId: {2}...",
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

         Trace.TraceInformation("[DiscussionsForm] Checking for new commits...");

         this.Text = DefaultCaption + "   (Checking for new commits)";
         await _updateGit(_mergeRequestKey, discussions);
         this.Text = DefaultCaption;

         return discussions;
      }

      private void renderDiscussionsWithSuspendedLayout(
         IEnumerable<Discussion> deletedDiscussions,
         IEnumerable<Discussion> updatedDiscussions)
      {
         // Avoid repositioning child controls on box removing, creation and visibility change
         SuspendLayout();

         Trace.TraceInformation(String.Format(
            "[DiscussionsForm] Rendering discussion contexts for MR IId {0} (updated {1}, deleted {2})...",
            _mergeRequestKey.IId, updatedDiscussions.Count(), deletedDiscussions.Count()));

         // Some controls are deleted here and some new are created. New controls are invisible.
         if (!renderDiscussions(deletedDiscussions, updatedDiscussions))
         {
            MessageBox.Show("No discussions to show. Press OK to close form.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            Trace.TraceInformation("[DiscussionsForm] No discussions to show");
            Close();
            return;
         }

         Trace.TraceInformation("[DiscussionsForm] Updating visibility of boxes...");

         // Make boxes that match user filter visible
         updateVisibilityOfBoxes();

         // Temporary benchmark
         Trace.TraceInformation("[DiscussionsForm] Visibility updated");

         // Put all child controls at their places
         ResumeLayout(true);
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
            updateVisibilityOfBox(box);
         }
      }

      private void updateVisibilityOfBox(DiscussionBox box)
      {
         if (box?.Discussion == null)
         {
            return;
         }

         bool isAllowedToDisplay = DisplayFilter.DoesMatchFilter(box.Discussion);
         // Note that the following does not change Visible property value until Form gets Visible itself
         box.Visible = isAllowedToDisplay;
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
         // Cannot check Visible property because it might be temporarily unset to avoid flickering.
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
            SingleDiscussionAccessor accessor = _shortcuts.GetSingleDiscussionAccessor(
               _mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(this, accessor, _git, _currentUser,
               _mergeRequestKey.ProjectKey, discussion, _mergeRequestAuthor,
               _colorScheme, onDiscussionBoxContentChanging, onDiscussionBoxContentChanged,
               sender => _mostRecentFocusedDiscussionControl = sender,
               _htmlTooltip, onAddCommentAction, onAddThreadAction, _commands, onCommandAction,
               _diffContextPosition, _discussionColumnWidth, _needShiftReplies)
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
         sender.Visible = false; // hide sender to avoid flickering on repositioning
         ResumeLayout(false); // Don't perform layout immediately, will be done in next callback
      }

      private void onDiscussionBoxContentChanged(DiscussionBox sender)
      {
         SuspendLayout(); // Avoid repositioning child controls on changing sender visibility
         updateVisibilityOfBox(sender);
         ResumeLayout(true); // Put child controls at their places
         updateSearch();
         _onDiscussionModified?.Invoke();
      }

      private void repositionControls()
      {
         int groupBoxMarginLeft = 5;
         int groupBoxMarginTop =  5;

         // If Vertical Scroll is visible, its width is already excluded from ClientSize.Width
         int vscrollWidth = VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth;

         // Discussion box can take all the width except scroll bar
         int clientWidth = ClientSize.Width - vscrollWidth;

         // Temporary variables to avoid changing control Location more than once
         int labelHintX = clientWidth - labelHotKeyHint.Width - groupBoxMarginLeft;
         Point linkLabelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point labelHintLocation = new Point(labelHintX, groupBoxMarginTop);
         Point linkLabelSaveLayoutLocation = new Point(labelHintX, groupBoxMarginTop);
         Point filterPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point sortPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point fontSelectionPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point actionsPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point searchPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);

         int topmostPanelOffset = linkLabelLocation.Y + linkLabelGitLabURL.Height;
         actionsPanelLocation.Offset(0, topmostPanelOffset);
         filterPanelLocation.Offset(actionsPanelLocation.X + ActionsPanel.Size.Width, topmostPanelOffset);
         sortPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width, topmostPanelOffset);
         fontSelectionPanelLocation.Offset(sortPanelLocation.X + SortPanel.Size.Width, topmostPanelOffset);
         searchPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width,
                                    Math.Max(sortPanelLocation.Y + SortPanel.Size.Height,
                                             fontSelectionPanelLocation.Y + FontSelectionPanel.Size.Height));
         linkLabelSaveLayoutLocation.Offset(0, labelHotKeyHint.Height);

         // Stack panels horizontally
         linkLabelGitLabURL.Location = linkLabelLocation + (Size)AutoScrollPosition;
         labelHotKeyHint.Location = labelHintLocation + (Size)AutoScrollPosition;
         linkLabelSaveAsDefaultLayout.Location = linkLabelSaveLayoutLocation + (Size)AutoScrollPosition;
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
         int discussionBoxTopMargin = _diffContextPosition == ConfigurationHelper.DiffContextPosition.Top ? 40 : 20;
         int firstDiscussionBoxTopMargin = 20;

         IEnumerable<DiscussionBox> boxes = getVisibleAndSortedBoxes();
         foreach (DiscussionBox box in boxes)
         {
            box.AdjustToWidth(clientWidth);

            int boxLocationX = (clientWidth - box.Width) / 2;
            int topMargin = box == boxes.First() ? firstDiscussionBoxTopMargin : discussionBoxTopMargin;
            int boxLocationY = topMargin + previousBoxLocation.Y + previousBoxSize.Height;
            Point location = new Point(boxLocationX, boxLocationY);
            box.Location = location + (Size)AutoScrollPosition;

            previousBoxLocation = location;
            previousBoxSize = box.Size;
         }
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
         Control control = _mostRecentFocusedDiscussionControl ?? ActiveControl;
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
         _mostRecentFocusedDiscussionControl = null;
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

      private void setDiffContextPosition(ConfigurationHelper.DiffContextPosition position)
      {
         _diffContextPosition = position;
         foreach (var box in getAllBoxes())
         {
            box.SetDiffContextPosition(_diffContextPosition);
         }
         updateSaveDefaultLayoutState();
         PerformLayout();
      }

      private void setColumnWidth(ConfigurationHelper.DiscussionColumnWidth width)
      {
         _discussionColumnWidth = width;
         foreach (var box in getAllBoxes())
         {
            box.SetDiscussionColumnWidth(_discussionColumnWidth);
         }
         updateSaveDefaultLayoutState();
         PerformLayout();
      }

      private void setNeedShiftReplies(bool value)
      {
         _needShiftReplies = value;
         foreach (var box in getAllBoxes())
         {
            box.SetNeedShiftReplies(_needShiftReplies);
         }
         updateSaveDefaultLayoutState();
         PerformLayout();
      }

      private void updateSaveDefaultLayoutState()
      {
         var diffContextPositionDefault = ConfigurationHelper.GetDiffContextPosition(Program.Settings);
         var discussionColumnWidthDefault = ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings);
         var needShiftRepliesDefault = Program.Settings.NeedShiftReplies;
         bool needEnableControl = diffContextPositionDefault   != _diffContextPosition
                               || discussionColumnWidthDefault != _discussionColumnWidth
                               || needShiftRepliesDefault      != _needShiftReplies;
         linkLabelSaveAsDefaultLayout.Visible = needEnableControl;
      }

      private void subscribeToSettingsChange()
      {
         Program.Settings.PropertyChanged += Settings_PropertyChanged;
      }

      private void unsubscribeFromSettingsChange()
      {
         Program.Settings.PropertyChanged -= Settings_PropertyChanged;
      }

      private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         updateSaveDefaultLayoutState();
      }

      private string DefaultCaption
      {
         get
         {
            return String.Format("Discussions for merge request !{0} in {1} -- \"{2}\"",
               _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestTitle);
         }
      }

      public string GetCurrentHostName()
      {
         return _mergeRequestKey.ProjectKey.HostName;
      }

      public string GetCurrentAccessToken()
      {
         return Program.Settings.GetAccessToken(_mergeRequestKey.ProjectKey.HostName);
      }

      public string GetCurrentProjectName()
      {
         return _mergeRequestKey.ProjectKey.ProjectName;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _mergeRequestKey.IId;
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private readonly IGitCommandService _git;
      private readonly ColorScheme _colorScheme;

      private readonly User _currentUser;
      private readonly DataCache _dataCache;
      private readonly Shortcuts _shortcuts;
      private readonly Func<MergeRequestKey, IEnumerable<Discussion>, Task> _updateGit;
      private readonly Action _onDiscussionModified;

      private ConfigurationHelper.DiffContextPosition _diffContextPosition;
      private ConfigurationHelper.DiscussionColumnWidth _discussionColumnWidth;
      private bool _needShiftReplies;
      private DiscussionFilterPanel FilterPanel;
      private DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private DiscussionFilter SystemFilter; // filters out discussions with System notes

      private DiscussionActionsPanel ActionsPanel;
      private DiscussionSearchPanel SearchPanel;
      private TextSearch TextSearch;
      private TextSearchResult? TextSearchResult;

      private DiscussionSortPanel SortPanel;
      private DiscussionSort DisplaySort;

      private DiscussionFontSelectionPanel FontSelectionPanel;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control _mostRecentFocusedDiscussionControl;
      private FormWindowState _previousState;
      private readonly IEnumerable<ICommand> _commands;
      private readonly HtmlToolTipEx _htmlTooltip = new HtmlToolTipEx
      {
         AutoPopDelay = 20000, // 20s
         InitialDelay = 300,
         // BaseStylesheet = Don't specify anything here because users' HTML <style> override it
      };
   }

   internal class NoDiscussionsToShow : Exception { };
}

