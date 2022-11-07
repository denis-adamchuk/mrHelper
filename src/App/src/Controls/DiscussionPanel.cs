using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;
using TheArtOfDev.HtmlRenderer.WinForms;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Controls
{
   public interface ITextControlHost
   {
      ITextControl[] Controls { get; }
      ITextControl ActiveControl { get; }
      event Action ContentChanged;
      void OnSearchResults(IEnumerable<TextSearchResult> results, bool showFoundOnly);
   }

   public partial class DiscussionPanel : UserControl, ITextControlHost, IHighlightListener
   {
      public DiscussionPanel()
      {
         InitializeComponent();

         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();
      }

      internal void Initialize(
         DiscussionSort discussionSort,
         DiscussionFilter displayFilter,
         AsyncDiscussionLoader discussionLoader,
         IEnumerable<Discussion> discussions,
         Shortcuts shortcuts,
         IGitCommandService git,
         ColorScheme colorScheme,
         MergeRequestKey mergeRequestKey,
         User mergeRequestAuthor,
         User currentUser,
         DiscussionLayout discussionLayout,
         AvatarImageCache avatarImageCache,
         string webUrl,
         Action<string> selectExternalNoteByUrl,
         IEnumerable<User> fullUserList)
      {
         _shortcuts = shortcuts;
         _git = git;
         _colorScheme = colorScheme;
         _mergeRequestKey = mergeRequestKey;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _avatarImageCache = avatarImageCache;
         _popupWindow.Renderer = new CommonControls.Tools.WinFormsHelpers.BorderlessRenderer();
         _popupWindow.DropShadowEnabled = false;
         _webUrl = webUrl;
         _selectExternalNoteByUrl = selectExternalNoteByUrl;
         _fullUserList = fullUserList;

         _discussionSort = discussionSort;
         _discussionSort.SortStateChanged += onSortStateChanged;

         _displayFilter = displayFilter;
         _displayFilter.FilterStateChanged += onFilterChanged;

         _discussionLoader = discussionLoader;
         _discussionLoader.Loaded += onDiscussionsLoaded;

         _discussionLayout = discussionLayout;
         _discussionLayout.DiffContextPositionChanged += onDiffContextPositionChanged;
         _discussionLayout.DiscussionColumnWidthChanged += onDiscussionColumnWidthChanged;
         _discussionLayout.NeedShiftRepliesChanged += onNeedShiftRepliesChanged;
         _discussionLayout.DiffContextDepthChanged += onDiffContextDepthChanged;

         apply(discussions);
      }

      public void OnSearchResults(IEnumerable<TextSearchResult> results, bool showFoundOnly)
      {
         if (!showFoundOnly)
         {
            _foundBoxes = null;
         }
         else
         {
            IEnumerable<DiscussionNote> notes = results
               .Select(result => result.Control)
               .Distinct()
               .Where(control => control is SearchableHtmlPanel)
               .Cast<SearchableHtmlPanel>()
               .Select(control => (DiscussionNote)(control.Tag))
               .OrderBy(note => note.Id);

            IEnumerable<DiscussionBox> boxes = getAllBoxes()
               .Where(box => box.Discussion.Notes.Any(note => notes.Any(foundNote => foundNote.Id == note.Id)));

            _foundBoxes = boxes;
         }

         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places
      }

      public void OnHighlighted(Control control)
      {
         control.Focus();
         scrollToControl(control, ExpectedControlPosition.TopEdge);
      }

      public event Action ContentChanged;
      public event Action ContentMismatchesFilter;
      public event Action ContentMatchesFilter;

      internal enum ESelectStyle
      {
         Normal,
         Flickering
      };

      internal bool SelectNoteById(int noteId, int? prevNoteId, ESelectStyle selectStyle)
      {
         DiscussionBox boxWithNote = getVisibleAndSortedBoxes()
            .FirstOrDefault(box => box.SelectNote(noteId, prevNoteId));
         if (boxWithNote != null)
         {
            if (selectStyle == ESelectStyle.Flickering)
            {
               _currentSelectedNote?.FlickBorder();
            }
            return true;
         }

         if (getAllBoxes().Any(box => box.HasNote(noteId)))
         {
            if (MessageBox.Show("Requested note is hidden by filters. Do you want to clear filters and highlight the note?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
               _displayFilter.FilterState = DiscussionFilterState.Default;
               if (getVisibleAndSortedBoxes().Any(box => box.HasNote(noteId)))
               {
                  return SelectNoteById(noteId, prevNoteId, ESelectStyle.Flickering);
               }
               Debug.Assert(false);
            }
         }

         return false;
      }

      internal int DiscussionCount => getAllBoxes().Count();

      internal void ProcessKeyDown(KeyEventArgs e)
      {
         void selectFirstBoxOnScreen()
         {
            foreach (DiscussionBox box in getVisibleAndSortedBoxes())
            {
               if (box.SelectTopVisibleNote())
               {
                  break;
               }
            }
         }

         void selectLastBoxOnScreen()
         {
            foreach (DiscussionBox box in getVisibleAndSortedBoxes().Reverse())
            {
               if (box.SelectBottomVisibleNote(ClientRectangle.Height))
               {
                  break;
               }
            }
         }

         if (e.KeyCode == Keys.Home)
         {
            selectNoteByPosition(ENoteSelectionRequest.First, null /* does not matter */);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.End)
         {
            selectNoteByPosition(ENoteSelectionRequest.Last, null /* does not matter */);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.PageUp)
         {
            AutoScrollPosition = new Point(AutoScrollPosition.X,
               Math.Max(VerticalScroll.Minimum, VerticalScroll.Value - VerticalScroll.LargeChange));
            selectFirstBoxOnScreen();
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.PageDown)
         {
            AutoScrollPosition = new Point(AutoScrollPosition.X,
               Math.Min(VerticalScroll.Maximum, VerticalScroll.Value + VerticalScroll.LargeChange));
            selectLastBoxOnScreen();
            e.Handled = true;
         }
      }

      ITextControl[] ITextControlHost.Controls => getAllVisibleAndSortedTextControls().ToArray();

      ITextControl ITextControlHost.ActiveControl => ((_mostRecentFocusedDiscussionControl ?? ActiveControl) as ITextControl);

      protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
      {
         return this.AutoScrollPosition; // https://stackoverflow.com/a/9428480
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         Trace.TraceInformation(String.Format("[DiscussionPanel] Processing OnVisibleChanged({0})...", Visible.ToString()));

         if (Visible)
         {
            // Form is visible now and controls will be drawn inside base.OnVisibleChanged() call.
            // Before drawing anything, let's put controls at their places.
            // Note that we have to postpone onLayoutUpdate() till this moment because before this moment ClientSize
            // Width obtains some intermediate values.
            repositionControls();
            this.Layout += onLayout;
         }
         else
         {
            this.Layout -= onLayout;
         }

         base.OnVisibleChanged(e);
      }

      protected override bool ProcessTabKey(bool forward)
      {
         if (!base.ProcessTabKey(forward))
         {
            return false;
         }
         
         // _currentSelectedNote has been updated inside onControlGotFocus() called from ProcessTabKey()
         scrollToControl(_currentSelectedNote, ExpectedControlPosition.Auto);
         return true;
      }

      private void onDiscussionsLoaded(IEnumerable<Discussion> discussions)
      {
         apply(discussions);
      }

      private void onLayout(object sender, LayoutEventArgs e)
      {
         repositionControls();
      }

      private void onDiffContextDepthChanged()
      {
         foreach (var box in getAllBoxes())
         {
            box.SetDiffContextDepth(_discussionLayout.DiffContextDepth);
         }
         PerformLayout();
         AdjustFormScrollbars(true);
      }

      private void onNeedShiftRepliesChanged()
      {
         foreach (var box in getAllBoxes())
         {
            box.SetNeedShiftReplies(_discussionLayout.NeedShiftReplies);
         }
         PerformLayout();
      }

      private void onDiscussionColumnWidthChanged()
      {
         foreach (var box in getAllBoxes())
         {
            box.SetDiscussionColumnWidth(_discussionLayout.DiscussionColumnWidth);
         }
         PerformLayout();
         AdjustFormScrollbars(true);
      }

      private void onDiffContextPositionChanged()
      {
         foreach (var box in getAllBoxes())
         {
            box.SetDiffContextPosition(_discussionLayout.DiffContextPosition);
         }
         PerformLayout();
         AdjustFormScrollbars(true);
      }

      private void onSortStateChanged()
      {
         PerformLayout(); // Recalculate locations of child controls
         ContentChanged?.Invoke();
         scrollToControl(_currentSelectedNote, ExpectedControlPosition.TopEdge);
      }

      private void onFilterChanged()
      {
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places
         ContentChanged?.Invoke();
         scrollToControl(_currentSelectedNote, ExpectedControlPosition.TopEdge);
      }

      private void onDiscussionControlGotFocus(Control sender)
      {
         _mostRecentFocusedDiscussionControl = sender;

         if (sender is HtmlPanelEx htmlPanelEx && htmlPanelEx.IsBorderSupported && sender != _currentSelectedNote)
         {
            if (_currentSelectedNote != null)
            {
               _currentSelectedNote.ShowBorderWhenNotFocused = false;
               _currentSelectedNote.Invalidate();
            }
            _currentSelectedNote = htmlPanelEx;
            _currentSelectedNote.ShowBorderWhenNotFocused = true;
         }
      }

      private void setFocusToSavedDiscussionControl()
      {
         _mostRecentFocusedDiscussionControl?.Focus();
      }

      private void selectNoteByUrl(string url)
      {
         DiscussionNote getCurrentNote() =>
            _currentSelectedNote != null ? ((DiscussionNote)(_currentSelectedNote.Tag)) : null;

         UrlParser.ParsedNoteUrl parsed = UrlParser.ParseNoteUrl(url);
         if (StringUtils.GetHostWithPrefix(parsed.Host) == _mergeRequestKey.ProjectKey.HostName
          && parsed.Project == _mergeRequestKey.ProjectKey.ProjectName
          && parsed.IId == _mergeRequestKey.IId)
         {
            if (SelectNoteById(parsed.NoteId, getCurrentNote()?.Id, ESelectStyle.Flickering))
            {
               scrollToControl(_currentSelectedNote, ExpectedControlPosition.ForceTopEdge);
            }
            return;
         }

         _selectExternalNoteByUrl(url); // note belongs to another MR, need to open its own Discussions view
      }

      private void selectNoteByPosition(ENoteSelectionRequest request, DiscussionBox current)
      {
         List<DiscussionBox> boxList = getVisibleAndSortedBoxes().ToList();

         int iNewIndex;
         ENoteSelectionRequest newRequest;

         switch (request)
         {
            case ENoteSelectionRequest.First:
               iNewIndex = 0;
               newRequest = ENoteSelectionRequest.First;
               break;

            case ENoteSelectionRequest.Last:
               iNewIndex = boxList.Count - 1;
               newRequest = ENoteSelectionRequest.Last;
               break;

            case ENoteSelectionRequest.Prev:
               iNewIndex = boxList.IndexOf(current) - 1;
               newRequest = ENoteSelectionRequest.Last;
               break;

            case ENoteSelectionRequest.Next:
               iNewIndex = boxList.IndexOf(current) + 1;
               newRequest = ENoteSelectionRequest.First;
               break;

            default:
               return;
         }

         if (iNewIndex >= 0 && iNewIndex < boxList.Count)
         {
            boxList[iNewIndex].SelectNote(newRequest);
         }
      }

      private void createDiscussionBoxes(IEnumerable<Discussion> discussions)
      {
         void scrollToSelectedNote() => scrollToControl(_currentSelectedNote, ExpectedControlPosition.Auto);

         foreach (Discussion discussion in discussions)
         {
            SingleDiscussionAccessor accessor = _shortcuts.GetSingleDiscussionAccessor(
               _mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(accessor, _git, _currentUser,
               _mergeRequestKey, discussion, _mergeRequestAuthor, _colorScheme,
               onDiscussionBoxContentChanging,
               onDiscussionBoxContentChanged,
               onDiscussionControlGotFocus,
               scrollToSelectedNote,
               setFocusToSavedDiscussionControl,
               _htmlTooltip,
               _popupWindow,
               _discussionLayout.DiffContextPosition,
               _discussionLayout.DiscussionColumnWidth,
               _discussionLayout.NeedShiftReplies,
               _discussionLayout.DiffContextDepth,
               _avatarImageCache,
               _pathCache,
               _webUrl,
               _fullUserList,
               selectNoteByUrl,
               selectNoteByPosition)
            {
               // Let new boxes be hidden to avoid flickering on repositioning
               Visible = false
            };
            Controls.Add(box);
            box.Initialize(this);
            if (!box.HasNotes)
            {
               Controls.Remove(box);
               box.Dispose();
            }
         }
      }

      private void renderDiscussionsWithSuspendedLayout(
         IEnumerable<Discussion> deletedDiscussions,
         IEnumerable<Discussion> updatedDiscussions)
      {
         // Avoid repositioning child controls on box removing, creation and visibility change
         SuspendLayout();

         Trace.TraceInformation(String.Format(
            "[DiscussionPanel] Rendering discussion contexts for MR IId {0} (updated {1}, deleted {2})...",
            _mergeRequestKey.IId, updatedDiscussions.Count(), deletedDiscussions.Count()));

         // Some controls are deleted here and some new are created. New controls are invisible.
         renderDiscussions(deletedDiscussions, updatedDiscussions);

         Trace.TraceInformation("[DiscussionPanel] Updating visibility of boxes...");

         // Make boxes that match user filter visible
         updateVisibilityOfBoxes();

         Trace.TraceInformation("[DiscussionPanel] Visibility updated");

         // Put all child controls at their places
         ResumeLayout(true);
      }

      private void renderDiscussions(
         IEnumerable<Discussion> deletedDiscussions,
         IEnumerable<Discussion> updatedDiscussions)
      {
         IEnumerable<Control> affectedControls =
            getControlsAffectedByDiscussionChanges(deletedDiscussions, updatedDiscussions);
         foreach (Control control in affectedControls)
         {
            control.Dispose();
            Controls.Remove(control);
         }
         createDiscussionBoxes(updatedDiscussions);
      }

      private void repositionControls()
      {
         // Discussion box can take all the width except scroll bar
         bool isVertScrollVisible = VerticalScroll.Visible;
         int vscrollWidth = isVertScrollVisible ? SystemInformation.VerticalScrollBarWidth : 0;
         int clientWidth = Bounds.Width - vscrollWidth;

         int topOffset = 0;
         Size previousBoxSize = new Size();
         Point previousBoxLocation = new Point();
         previousBoxLocation.Offset(0, topOffset);

         // Stack boxes vertically
         DiffContextPosition diffContextPosition = _discussionLayout?.DiffContextPosition ?? DiffContextPosition.Top;
         bool isContextAtTop = diffContextPosition == DiffContextPosition.Top;
         int discussionBoxTopMargin = isContextAtTop ? 40 : 20; // TODO HighDPI
         int firstDiscussionBoxTopMargin = 20; // TODO HighDPI

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

            if (isVertScrollVisible != VerticalScroll.Visible)
            {
               break;
            }
         }
      }

      private void apply(IEnumerable<Discussion> discussions)
      {
         if (discussions == null)
         {
            return;
         }

         bool isResolved(Discussion d) => d.Notes.Any(note => note.Resolvable && !note.Resolved);
         DateTime getTimestamp(Discussion d) => d.Notes.Select(note => note.Updated_At).Max();
         int getNoteCount(Discussion d) => d.Notes.Count();

         IEnumerable<Discussion> cachedDiscussions = getAllBoxes().Select(box => box.Discussion);

         IEnumerable<Discussion> updatedDiscussions = discussions
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
            .Where(cachedDiscussion => discussions.SingleOrDefault(d => d.Id == cachedDiscussion.Id) == null)
            .ToArray(); // force immediate execution

         if (deletedDiscussions.Any() || updatedDiscussions.Any())
         {
            renderDiscussionsWithSuspendedLayout(deletedDiscussions, updatedDiscussions);
            ContentChanged?.Invoke();
         }
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
         return _visibleBoxes.Where(box => box.Discussion != null);
      }

      private IEnumerable<DiscussionBox> getVisibleAndSortedBoxes()
      {
         return _discussionSort?.Sort(getVisibleBoxes(), x => x.Discussion.Notes) ?? Array.Empty<DiscussionBox>();
      }

      private IEnumerable<ITextControl> getAllVisibleAndSortedTextControls()
      {
         return getVisibleAndSortedBoxes()
            .SelectMany(box => box.Controls.Cast<Control>())
            .Where(control => control is ITextControl)
            .Cast<ITextControl>()
            .ToArray(); // force immediate execution
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
         sender.Visible = true;
         ResumeLayout(true); // Put child controls at their places
         ContentChanged?.Invoke();

         bool doesContentMismatchFilter =
            getVisibleBoxes()
            .FirstOrDefault(box => !_displayFilter.DoesMatchFilter(box.Discussion)) != null;
         if (doesContentMismatchFilter)
         {
            ContentMismatchesFilter?.Invoke();
         }
      }

      private void updateVisibilityOfBoxes()
      {
         foreach (DiscussionBox box in getAllBoxes())
         {
            updateVisibilityOfBox(box);
         }

         // Check if this box will be visible or not. The same condition as in updateVisibilityOfBoxes().
         // Cannot check Visible property because it might be temporarily unset to avoid flickering.
         _visibleBoxes = getAllBoxes()
            .Where(box => _displayFilter.DoesMatchFilter(box.Discussion))
            .Where(box => isBoxAmongSearchResults(box))
            .ToArray(); // force immediate execution
         ContentMatchesFilter?.Invoke();
      }

      private void updateVisibilityOfBox(DiscussionBox box)
      {
         if (box?.Discussion == null)
         {
            return;
         }

         bool isAllowedToDisplay = _displayFilter.DoesMatchFilter(box.Discussion) && isBoxAmongSearchResults(box);
         // Note that the following does not change Visible property value until Form gets Visible itself
         box.Visible = isAllowedToDisplay;
      }

      private bool isBoxAmongSearchResults(DiscussionBox box)
      {
         return _foundBoxes == null || _foundBoxes.Contains(box);
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         getAllBoxes()
            .ToList()
            .ForEach(box => box.RefreshTimeStamps());
      }

      private enum ExpectedControlPosition
      {
         Auto,
         TopEdge,
         ForceTopEdge
      }

      private void scrollToControl(Control control, ExpectedControlPosition position)
      {
         if (control == null)
         {
            return;
         }

         void placeControlAtTop()
         {
            Point newControlTopLocationAtScreen = control.PointToScreen(new Point(0, -FontHeight));
            Point newControlTopLocationAtForm = PointToClient(newControlTopLocationAtScreen);

            int x = AutoScrollPosition.X;
            int y = VerticalScroll.Value + newControlTopLocationAtForm.Y;
            Point newPosition = new Point(x, y);
            AutoScrollPosition = newPosition;
         }

         Point controlTopLocationAtScreen = control.PointToScreen(new Point(0, 0));
         Point controlTopLocationAtForm = PointToClient(controlTopLocationAtScreen);

         void placeControlAtBottom()
         {
            int newControlTop = control.Height - (ClientRectangle.Y + ClientRectangle.Height - controlTopLocationAtForm.Y);

            int x = AutoScrollPosition.X;
            int y = VerticalScroll.Value + newControlTop;
            Point newPosition = new Point(x, y);
            AutoScrollPosition = newPosition;
         }

         Point controlBottomLocationAtScreen = control.PointToScreen(new Point(0, control.Height));
         Point controlBottomLocationAtForm = PointToClient(controlBottomLocationAtScreen);

         switch (position)
         {
            case ExpectedControlPosition.Auto:
               {
                  bool isTopVisible = ClientRectangle.Contains(controlTopLocationAtForm);
                  bool isBottomVisible = ClientRectangle.Contains(controlBottomLocationAtForm);
                  if (!isTopVisible && !isBottomVisible)
                  {
                     if (controlBottomLocationAtForm.Y > ClientRectangle.Y + ClientRectangle.Height)
                     {
                        placeControlAtBottom();
                     }
                     else if (controlTopLocationAtForm.Y < ClientRectangle.Y)
                     {
                        placeControlAtTop();
                     }
                  }
                  else if (!isTopVisible)
                  {
                     placeControlAtTop();
                  }
                  else if (!isBottomVisible)
                  {
                     placeControlAtBottom();
                  }
               }
               break;

            case ExpectedControlPosition.TopEdge:
               {
                  Point newControlTopLocationAtScreen = control.PointToScreen(new Point(0, 0));
                  Point newControlTopLocationAtForm = PointToClient(newControlTopLocationAtScreen);
                  if (!ClientRectangle.Contains(newControlTopLocationAtForm))
                  {
                     placeControlAtTop();
                  }
               }
               break;

            case ExpectedControlPosition.ForceTopEdge:
               placeControlAtTop();
               break;
         }
      }

      private User _currentUser;
      private AvatarImageCache _avatarImageCache;
      private MergeRequestKey _mergeRequestKey;
      private User _mergeRequestAuthor;
      private IGitCommandService _git;
      private ColorScheme _colorScheme;
      private Shortcuts _shortcuts;
      private string _webUrl;
      private Action<string> _selectExternalNoteByUrl;
      private IEnumerable<User> _fullUserList;
      private DiscussionSort _discussionSort;
      private DiscussionFilter _displayFilter; // filters out discussions by user preferences
      private DiscussionLayout _discussionLayout;
      private AsyncDiscussionLoader _discussionLoader;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control _mostRecentFocusedDiscussionControl;
      private IEnumerable<DiscussionBox> _visibleBoxes;
      private readonly HtmlToolTipEx _htmlTooltip = new HtmlToolTipEx
      {
         AutoPopDelay = 20000, // 20s
         InitialDelay = 300,
         // BaseStylesheet = Don't specify anything here because users' HTML <style> override it
      };

      private static readonly int PopupWindowBorderRadius = 10;
      private readonly PopupWindow _popupWindow = new PopupWindow(autoClose: true,
         borderRadius: PopupWindowBorderRadius);

      private static readonly int RedrawTimerInterval = 1000 * 60; // 1 minute

      // This timer is needed to update "ago" timestamps
      private readonly Timer _redrawTimer = new Timer
      {
         Interval = RedrawTimerInterval
      };

      public static int DiffContextBorderRadius = 10;
      private RoundedPathCache _pathCache = new RoundedPathCache(DiffContextBorderRadius);
      private HtmlPanelEx _currentSelectedNote;
      private IEnumerable<DiscussionBox> _foundBoxes;
   }
}

