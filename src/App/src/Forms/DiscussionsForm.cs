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
using mrHelper.Client.Discussions;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Session;
using mrHelper.Common.Interfaces;
using mrHelper.StorageSupport;

namespace mrHelper.App.Forms
{
   internal partial class DiscussionsForm : CustomFontForm
   {
      /// <summary>
      /// Throws:
      /// ArgumentException
      /// </summary>
      internal DiscussionsForm(
         ISession session, IGitCommandService git,
         User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         string mergeRequestTitle, User mergeRequestAuthor,
         int diffContextDepth, ColorScheme colorScheme,
         Func<MergeRequestKey, IEnumerable<Discussion>, Task> updateGit, Action onDiscussionModified)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mergeRequestTitle;
         _mergeRequestAuthor = mergeRequestAuthor;

         _git = git;
         _diffContextDepth = diffContextDepth;

         _colorScheme = colorScheme;

         _session = session;
         _updateGit = updateGit;
         _onDiscussionModified = onDiscussionModified;

         _currentUser = currentUser;
         if (_currentUser.Id == 0)
         {
            throw new ArgumentException("Bad user Id");
         }

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         DisplayFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor,
            DiscussionFilterState.Default);
         SystemFilter = new DiscussionFilter(_currentUser, _mergeRequestAuthor,
            DiscussionFilterState.AllExceptSystem);

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

         FontSelectionPanel = new DiscussionFontSelectionPanel(font => applyFont(font));

         Controls.Add(FilterPanel);
         Controls.Add(ActionsPanel);
         Controls.Add(SearchPanel);
         Controls.Add(SortPanel);
         Controls.Add(FontSelectionPanel);

         applyFont(Program.Settings.MainWindowFontSizeName);
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

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync()
      {
         if (_session?.DiscussionCache == null)
         {
            return null;
         }

         Trace.TraceInformation(String.Format(
            "[DiscussionsForm] Loading discussions. Hostname: {0}, Project: {1}, MR IId: {2}",
               _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId));

         this.Text = DefaultCaption + "   (Loading discussions)";

         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await _session.DiscussionCache.LoadDiscussions(_mergeRequestKey);
         }
         catch (DiscussionCacheException ex)
         {
            this.Text = DefaultCaption;
            string message = "Cannot load discussions from GitLab";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
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

      private bool renderDiscussions(IEnumerable<Discussion> discussions, bool needReposition)
      {
         updateLayout(discussions, needReposition, true);
         Focus(); // Set focus to the Form
         return discussions != null && Controls.Cast<Control>().Any((x) => x is DiscussionBox);
      }

      private void updateLayout(IEnumerable<Discussion> discussions, bool needReposition, bool suspendLayout)
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

         foreach (DiscussionBox box in boxes)
         {
            box.Visible = DisplayFilter.DoesMatchFilter(box.Discussion);
         }
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
         foreach (var discussion in discussions)
         {
            if (!SystemFilter.DoesMatchFilter(discussion))
            {
               continue;
            }

            IDiscussionEditor editor = _session.GetDiscussionEditor(_mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(this, editor, _git, _currentUser,
               _mergeRequestKey.ProjectKey, discussion, _mergeRequestAuthor,
               _diffContextDepth, _colorScheme,
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
                  _onDiscussionModified?.Invoke();
               }, sender => MostRecentFocusedDiscussionControl = sender)
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
         Point fontSelectionPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point actionsPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);
         Point searchPanelLocation = new Point(groupBoxMarginLeft, groupBoxMarginTop);

         sortPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width, 0);
         fontSelectionPanelLocation.Offset(sortPanelLocation.X + SortPanel.Size.Width, 0);
         actionsPanelLocation.Offset(fontSelectionPanelLocation.X + FontSelectionPanel.Size.Width, 0);
         searchPanelLocation.Offset(filterPanelLocation.X + FilterPanel.Size.Width,
                                    Math.Max(sortPanelLocation.Y + SortPanel.Size.Height,
                                    Math.Max(fontSelectionPanelLocation.Y + FontSelectionPanel.Size.Height,
                                             actionsPanelLocation.Y + ActionsPanel.Size.Height)));

         // Stack panels horizontally
         FilterPanel.Location = filterPanelLocation + (Size)AutoScrollPosition;
         SortPanel.Location = sortPanelLocation + (Size)AutoScrollPosition;
         FontSelectionPanel.Location = fontSelectionPanelLocation + (Size)AutoScrollPosition;
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

      private bool isSearchableControl(Control control)
      {
         return control.Parent is DiscussionBox box && DisplayFilter.DoesMatchFilter(box.Discussion);
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly User _mergeRequestAuthor;
      private readonly IGitCommandService _git;
      private readonly int _diffContextDepth;
      private readonly ColorScheme _colorScheme;

      private readonly User _currentUser;
      private readonly ISession _session;
      private readonly Func<MergeRequestKey, IEnumerable<Discussion>, Task> _updateGit;
      private readonly Action _onDiscussionModified;

      private readonly DiscussionFilterPanel FilterPanel;
      private readonly DiscussionFilter DisplayFilter; // filters out discussions by user preferences
      private readonly DiscussionFilter SystemFilter; // filters out discussions with System notes

      private readonly DiscussionActionsPanel ActionsPanel;

      private readonly DiscussionSearchPanel SearchPanel;
      private TextSearch TextSearch;
      private TextSearchResult? TextSearchResult;

      private readonly DiscussionSortPanel SortPanel;
      private readonly DiscussionSort DisplaySort;

      private readonly DiscussionFontSelectionPanel FontSelectionPanel;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control MostRecentFocusedDiscussionControl;
   }

   internal class NoDiscussionsToShow : ArgumentException { };
}

