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
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Controls
{
   public interface ITextControlHost
   {
      ITextControl[] Controls { get; }
      ITextControl ActiveControl { get; }
      event Action ContentChanged;
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
         Action<string> onSelectNoteByUrl)
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
         _onSelectNoteByUrl = onSelectNoteByUrl;

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

      public void OnHighlighted(Control control)
      {
         control.Focus();
         scrollToControl(control);
      }

      internal bool SelectNoteById(int noteId)
      {
         DiscussionBox boxWithNote = getVisibleAndSortedBoxes().FirstOrDefault(box => box.SelectNote(noteId));
         if (boxWithNote != null)
         {
            scrollToControl(boxWithNote);
            return true;
         }
         return false;
      }

      internal int DiscussionCount => getAllBoxes().Count();

      ITextControl[] ITextControlHost.Controls => getAllVisibleAndSortedTextControls().ToArray();

      ITextControl ITextControlHost.ActiveControl => ((_mostRecentFocusedDiscussionControl ?? ActiveControl) as ITextControl);

      public event Action ContentChanged;
      public event Action ContentMismatchesFilter;
      public event Action ContentMatchesFilter;

      protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
      {
         return this.AutoScrollPosition; // https://stackoverflow.com/a/9428480
      }

      private void scrollToControl(Control control)
      {
         Point controlLocationAtScreen = control.PointToScreen(new Point(0, -5));
         Point controlLocationAtForm = PointToClient(controlLocationAtScreen);

         if (!ClientRectangle.Contains(controlLocationAtForm))
         {
            int x = AutoScrollPosition.X;
            int y = VerticalScroll.Value + controlLocationAtForm.Y;
            Point newPosition = new Point(x, y);
            AutoScrollPosition = newPosition;
         }
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

      internal void ProcessKeyDown(KeyEventArgs e)
      {
         void selectFirstBoxOnScreen()
         {
            IEnumerable<DiscussionBox> notes = getVisibleAndSortedBoxes();
            foreach (DiscussionBox box in notes)
            {
               if (box.SelectTopVisibleNote())
               {
                  break;
               }
            }
         }

         if (e.KeyCode == Keys.Home)
         {
            onSelectNoteByPosition(ENoteSelectionRequest.First, null /* does not matter */);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.End)
         {
            onSelectNoteByPosition(ENoteSelectionRequest.Last, null /* does not matter */);
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
            selectFirstBoxOnScreen();
            e.Handled = true;
         }
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
         int? selectedNoteId = new int?();
         if (_currentSelectedNote != null)
         {
            selectedNoteId = ((DiscussionNote)(_currentSelectedNote.Tag)).Id;
         }

         PerformLayout(); // Recalculate locations of child controls
         ContentChanged?.Invoke();

         if (selectedNoteId.HasValue)
         {
            SelectNoteById(selectedNoteId.Value);
         }
      }

      private void onFilterChanged()
      {
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places
         ContentChanged?.Invoke();
      }

      private void onControlGotFocus(Control sender)
      {
         _mostRecentFocusedDiscussionControl = sender;

         if (sender is HtmlPanelEx htmlPanelEx && htmlPanelEx.NeedShowBorder && sender != _currentSelectedNote)
         {
            if (_currentSelectedNote != null)
            {
               _currentSelectedNote.ShowBorder = false;
            }
            _currentSelectedNote = htmlPanelEx;
            _currentSelectedNote.ShowBorder = true;
         }
      }

      private void onRestoreFocus()
      {
         _mostRecentFocusedDiscussionControl?.Focus();
      }

      private void onSelectNoteByUrl(string url)
      {
         UrlParser.ParsedNoteUrl parsed = UrlParser.ParseNoteUrl(url);
         if (StringUtils.GetHostWithPrefix(parsed.Host) == _mergeRequestKey.ProjectKey.HostName
          && parsed.Project == _mergeRequestKey.ProjectKey.ProjectName
          && parsed.IId == _mergeRequestKey.IId)
         {
            SelectNoteById(parsed.NoteId);
            return;
         }

         _onSelectNoteByUrl(url);
      }

      private void onSelectNoteByPosition(ENoteSelectionRequest request, DiscussionBox current)
      {
         IEnumerable<DiscussionBox> boxes = getVisibleAndSortedBoxes();
         List<DiscussionBox> boxList = boxes.ToList();

         int iNewIndex = -1;
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
            DiscussionBox box = boxList[iNewIndex];
            box.SelectNote(newRequest);
            scrollToControl(box);
         }
      }

      private void createDiscussionBoxes(IEnumerable<Discussion> discussions)
      {
         foreach (Discussion discussion in discussions)
         {
            SingleDiscussionAccessor accessor = _shortcuts.GetSingleDiscussionAccessor(
               _mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(this, accessor, _git, _currentUser,
               _mergeRequestKey, discussion, _mergeRequestAuthor,
               _colorScheme, onDiscussionBoxContentChanging, onDiscussionBoxContentChanged,
               onControlGotFocus, onRestoreFocus, _htmlTooltip, _popupWindow,
               _discussionLayout.DiffContextPosition,
               _discussionLayout.DiscussionColumnWidth,
               _discussionLayout.NeedShiftReplies,
               _discussionLayout.DiffContextDepth,
               _avatarImageCache,
               _pathCache,
               _webUrl,
               onSelectNoteByUrl,
               onSelectNoteByPosition)
            {
               // Let new boxes be hidden to avoid flickering on repositioning
               Visible = false
            };
            if (box.HasNotes)
            {
               Controls.Add(box);
            }
            else
            {
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
         int discussionBoxTopMargin = isContextAtTop ? 40 : 20;
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
            .ToArray(); // force immediate execution
         ContentMatchesFilter?.Invoke();
      }

      private void updateVisibilityOfBox(DiscussionBox box)
      {
         if (box?.Discussion == null)
         {
            return;
         }

         bool isAllowedToDisplay = _displayFilter.DoesMatchFilter(box.Discussion);
         // Note that the following does not change Visible property value until Form gets Visible itself
         box.Visible = isAllowedToDisplay;
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         getAllBoxes()
            .ToList()
            .ForEach(box => box.RefreshTimeStamps());
      }

      private User _currentUser;
      private AvatarImageCache _avatarImageCache;
      private MergeRequestKey _mergeRequestKey;
      private User _mergeRequestAuthor;
      private IGitCommandService _git;
      private ColorScheme _colorScheme;
      private Shortcuts _shortcuts;
      private string _webUrl;
      private Action<string> _onSelectNoteByUrl;
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

      private RoundedPathCache _pathCache = new RoundedPathCache(10);
      private HtmlPanelEx _currentSelectedNote;
   }
}

