using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
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
   }

   public partial class DiscussionPanel : UserControl, ITextControlHost, IHighlightListener
   {
      public DiscussionPanel()
      {
         InitializeComponent();

         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();

         _pathCache = new RoundedPathCache(DiffContextBorderRadius);
      }

      internal void Initialize(
         DiscussionSort discussionSort,
         DiscussionFilter displayFilter,
         DiscussionFilter pageFilter,
         DiscussionFilter searchFilter,
         AsyncDiscussionLoader discussionLoader,
         IEnumerable<Discussion> discussions,
         Shortcuts shortcuts,
         IGitCommandService git,
         MergeRequestKey mergeRequestKey,
         User mergeRequestAuthor,
         User currentUser,
         DiscussionLayout discussionLayout,
         AvatarImageCache avatarImageCache,
         string webUrl,
         Action<string> selectExternalNoteByUrl,
         IEnumerable<User> fullUserList,
         IEnumerable<Project> fullProjectList,
         Action<bool> contentChanged)
      {
         _shortcuts = shortcuts;
         _git = git;
         _mergeRequestKey = mergeRequestKey;
         _mergeRequestAuthor = mergeRequestAuthor;
         _currentUser = currentUser;
         _avatarImageCache = avatarImageCache;
         _popupWindow.Renderer = new CommonControls.Tools.WinFormsHelpers.BorderlessRenderer();
         _popupWindow.DropShadowEnabled = false;
         _webUrl = webUrl;
         _selectExternalNoteByUrl = selectExternalNoteByUrl;
         _fullUserList = fullUserList;
         _fullProjectList = fullProjectList;
         _contentChanged = contentChanged;

         _discussionSort = discussionSort;
         _discussionSort.SortStateChanged += onSortStateChanged;

         _displayFilter = displayFilter;
         _displayFilter.FilterStateChanged += onDisplayFilterChanged;

         _pageFilter = pageFilter;
         _pageFilter.FilterStateChanged += onPageFilterChanged;

         _searchFilter = searchFilter;
         _searchFilter.FilterStateChanged += onSearchFilterChanged;

         _discussionLoader = discussionLoader;
         _discussionLoader.Loaded += onDiscussionsLoaded;

         _discussionLayout = discussionLayout;
         _discussionLayout.DiffContextPositionChanged += onDiffContextPositionChanged;
         _discussionLayout.DiscussionColumnWidthChanged += onDiscussionColumnWidthChanged;
         _discussionLayout.NeedShiftRepliesChanged += onNeedShiftRepliesChanged;
         _discussionLayout.DiffContextDepthChanged += onDiffContextDepthChanged;

         apply(discussions);
      }

      private void onSearchFilterChanged()
      {
         IEnumerable<DiscussionBox> oldHiddenBoxes = _boxesHiddenBySearch;
         IEnumerable<DiscussionBox> newHiddenBoxes = null;

         bool checkIfRepositionNeeded()
         {
            if (oldHiddenBoxes != null && newHiddenBoxes == null)
            {
               return true;
            }
            else if (oldHiddenBoxes == null && newHiddenBoxes != null)
            {
               return true;
            }
            else if (oldHiddenBoxes != null && newHiddenBoxes != null)
            {
               IEnumerable<string> oldIds = oldHiddenBoxes.Select(box => box.Discussion.Id);
               IEnumerable<string> newIds = newHiddenBoxes.Select(box => box.Discussion.Id);
               return !oldIds.SequenceEqual(newIds);
            }
            return false;
         }

         IEnumerable<Discussion> enabledDiscussions = _searchFilter.FilterState.EnabledDiscussions;
         if (enabledDiscussions != null)
         {
            IEnumerable<DiscussionBox> foundBoxes = getBoxesForSearch()
               .Where(box => enabledDiscussions.Any(x => x.Id == box.Discussion.Id));
            newHiddenBoxes = getBoxesForSearch().Except(foundBoxes).ToArray();
         }
         _boxesHiddenBySearch = newHiddenBoxes;

         if (checkIfRepositionNeeded())
         {
            SuspendLayout(); // Avoid repositioning child controls on each box visibility change
            updateVisibilityOfBoxes();
            ResumeLayout(true); // Place controls at their places

            PageChangeRequest?.Invoke(0);
         }
      }

      internal IEnumerable<Discussion> CollectDiscussionsForControls(IEnumerable<ITextControl> controls)
      {
         return getBoxesForSearch()
            .Where(box => controls.Any(control => box.HasTextControl(control)))
            .Select(box => box.Discussion);
      }

      public void OnHighlighted(Control control)
      {
         scrollToControl(control, ExpectedControlPosition.TopEdge);
      }

      public event Action ContentMismatchesFilter;
      public event Action ContentMatchesFilter;
      public event Action PageCountChanged;
      public event Action PageSizeChanged;
      public event Action<int> PageChangeRequest;
      internal int PageCount => _boxesToPages.Values.Distinct().Count();
      internal int PageSize
      {
         get => _pageSize == null ? Program.Settings.DiscussionPageSize : _pageSize.Value;
         private set
         {
            _pageSize = value;
            PageSizeChanged?.Invoke();
         }
      }

      internal enum ESelectStyle
      {
         Normal,
         Flickering
      };

      internal bool SelectNoteById(int noteId, int? prevNoteId, ESelectStyle selectStyle)
      {
         if (selectNoteById(noteId, prevNoteId, selectStyle))
         {
            return true;
         }

         if (getAllBoxes().Any(box => box.HasNote(noteId)))
         {
            if (MessageBox.Show("Requested note is hidden by filters. Do you want to clear filters and highlight the note?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
               _displayFilter.FilterState = DiscussionFilterState.Default;
               return selectNoteById(noteId, prevNoteId, selectStyle);
            }
         }

         return false;
      }

      internal int DiscussionCount => getAllBoxes().Count();

      internal void ProcessKeyDown(KeyEventArgs e)
      {
         void selectFirstBoxOnScreen()
         {
            foreach (DiscussionBox box in getVisibleAndSortedBoxesOnCurrentPage())
            {
               if (box.SelectTopVisibleNote())
               {
                  break;
               }
            }
         }

         void selectLastBoxOnScreen()
         {
            foreach (DiscussionBox box in getVisibleAndSortedBoxesOnCurrentPage().Reverse())
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

      ITextControl[] ITextControlHost.Controls => getControlsSuitableForSearch().ToArray();

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

         // _focusedNote has been updated inside onControlGotFocus() called from ProcessTabKey()
         scrollToControl(_focusedNote, ExpectedControlPosition.Auto);
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

      private void onSortStateChanged() //-V3013
      {
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places

         _contentChanged?.Invoke(true);

         scrollToControl(_focusedNote, ExpectedControlPosition.TopEdge);
      }

      private void onDisplayFilterChanged()
      {
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places

         _contentChanged?.Invoke(true);

         if (_focusedNote == null)
         {
            PageChangeRequest?.Invoke(0);
         }
         else
         {
            if (!_focusedNote.Visible)
            {
               DiscussionBox box = _focusedNote.Parent as DiscussionBox;
               if (box != null && !_boxesToPages.ContainsKey(box))
               {
                  // focused note is hidden by Display Filter
                  PageChangeRequest?.Invoke(0);
               }
            }
            else
            {
               scrollToControl(_focusedNote, ExpectedControlPosition.TopEdge);
            }
         }
      }

      private void onPageFilterChanged()
      {
         SuspendLayout(); // Avoid repositioning child controls on each box visibility change
         updateVisibilityOfBoxes();
         ResumeLayout(true); // Place controls at their places

         AutoScrollPosition = new Point(0, 0);
      }

      private void onDiscussionControlGotFocus(Control sender)
      {
         _mostRecentFocusedDiscussionControl = sender;

         if (sender is HtmlPanelEx htmlPanelEx && htmlPanelEx.IsBorderSupported && sender != _focusedNote)
         {
            if (_focusedNote != null)
            {
               _focusedNote.ShowBorderWhenNotFocused = false;
               _focusedNote.Invalidate();
            }
            _focusedNote = htmlPanelEx;
            _focusedNote.ShowBorderWhenNotFocused = true;
         }
      }

      private void setFocusToSavedDiscussionControl()
      {
         _mostRecentFocusedDiscussionControl?.Focus();
      }

      private void selectNoteByUrl(string url)
      {
         DiscussionNote getCurrentNote() =>
            _focusedNote != null ? ((DiscussionNote)(_focusedNote.Tag)) : null;

         UrlParser.ParsedNoteUrl parsed = UrlParser.ParseNoteUrl(url);
         if (StringUtils.GetHostWithPrefix(parsed.Host) == _mergeRequestKey.ProjectKey.HostName
          && parsed.Project == _mergeRequestKey.ProjectKey.ProjectName
          && parsed.IId == _mergeRequestKey.IId)
         {
            if (SelectNoteById(parsed.NoteId, getCurrentNote()?.Id, ESelectStyle.Flickering))
            {
               scrollToControl(_focusedNote, ExpectedControlPosition.ForceTopEdge);
            }
            return;
         }

         _selectExternalNoteByUrl(url); // note belongs to another MR, need to open its own Discussions view
      }

      private void selectNoteByPosition(ENoteSelectionRequest request, DiscussionBox current)
      {
         List<DiscussionBox> boxList = getVisibleAndSortedBoxesOnCurrentPage().ToList();

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

      private bool selectNoteById(int noteId, int? prevNoteId, ESelectStyle selectStyle)
      {
         DiscussionBox boxWithNote = getVisibleAndSortedBoxesOnAllPages()
            .FirstOrDefault(box => box.SelectNote(noteId, prevNoteId));
         if (boxWithNote == null)
         {
            return false;
         }

         if (selectStyle == ESelectStyle.Flickering)
         {
            _focusedNote?.FlickBorder();
         }
         return true;
      }

      private void createDiscussionBoxes(IEnumerable<Discussion> discussions)
      {
         void scrollToNote(Control control) => scrollToControl(control, ExpectedControlPosition.Auto);

         Project project = _fullProjectList
            .FirstOrDefault(p => p.Path_With_Namespace == _mergeRequestKey.ProjectKey.ProjectName);
         string imagePath = StringUtils.GetUploadsPrefix(_mergeRequestKey.ProjectKey.HostName, project?.Id ?? 0);
         foreach (Discussion discussion in discussions)
         {
            SingleDiscussionAccessor accessor = _shortcuts.GetSingleDiscussionAccessor(
               _mergeRequestKey, discussion.Id);
            DiscussionBox box = new DiscussionBox(accessor, _git, _currentUser,
               imagePath, discussion, _mergeRequestAuthor,
               onDiscussionBoxContentChanging,
               onDiscussionBoxContentChanged,
               onDiscussionControlGotFocus,
               scrollToNote,
               setFocusToSavedDiscussionControl,
               _htmlTooltip,
               _popupWindow,
               _discussionLayout.DiffContextPosition,
               _discussionLayout.DiscussionColumnWidth,
               _discussionLayout.NeedShiftReplies,
               _discussionLayout.DiffContextDepth,
               _avatarImageCache,
               _pathCache,
               _estimatedWidthCache,
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

      private void adjustPageSize() => PageSize /= 2;

      private int? _pageSize;

      private Dictionary<DiscussionBox, int> _boxesToPages = new Dictionary<DiscussionBox, int>();
      private void matchBoxesToPages(IEnumerable<DiscussionBox> boxes)
      {
         int oldCount = PageCount;

         _boxesToPages.Clear();
         foreach (DiscussionBox box in boxes)
         {
            int page = _boxesToPages.Count / PageSize;
            _boxesToPages[box] = page;
         }

         int newCount = PageCount;
         if (oldCount != newCount)
         {
            Trace.TraceInformation("[DiscussionPanel] Page count changed from {0} to {1}", oldCount, newCount);
            PageCountChanged?.Invoke();
         }
      }

      private int? getPage(DiscussionBox box) => _boxesToPages.TryGetValue(box, out int value) ? value : new int?();

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
         int discussionBoxTopMargin = isContextAtTop ? DiscussionBoxTopMarginVertLayout : DiscussionBoxTopMarginHorzLayout;

         IEnumerable<DiscussionBox> boxes = getVisibleAndSortedBoxesOnCurrentPage();
         foreach (DiscussionBox box in boxes)
         {
            box.AdjustToWidth(clientWidth);

            int boxLocationX = (clientWidth - box.Width) / 2;
            int topMargin = box == boxes.First() ? FirstDiscussionBoxTopMargin : discussionBoxTopMargin;
            int boxLocationY = topMargin + previousBoxLocation.Y + previousBoxSize.Height;
            Point location = new Point(boxLocationX, boxLocationY);
            box.Location = location + (Size)AutoScrollPosition;

            previousBoxLocation = location;
            previousBoxSize = box.Size;

            if (isVertScrollVisible != VerticalScroll.Visible)
            {
               break;
            }

            if (location.Y > MaxBoxYLocationPx)
            {
               adjustPageSize();
               updateVisibilityOfBoxes();
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
            _contentChanged?.Invoke(true);
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

      private IEnumerable<DiscussionBox> getVisibleBoxesOnCurrentPage()
      {
         return _visibleBoxesOnCurrentPage?.Where(box => box.Discussion != null) ?? Array.Empty<DiscussionBox>();
      }

      private IEnumerable<DiscussionBox> getVisibleBoxesOnAllPages()
      {
         return _visibleBoxes?.Where(box => box.Discussion != null) ?? Array.Empty<DiscussionBox>();
      }
     
      private IEnumerable<DiscussionBox> getVisibleAndSortedBoxesOnCurrentPage()
      {
         return sortBoxes(getVisibleBoxesOnCurrentPage());
      }

      private IEnumerable<DiscussionBox> getVisibleAndSortedBoxesOnAllPages()
      {
         return sortBoxes(getVisibleBoxesOnAllPages());
      }

      private IEnumerable<DiscussionBox> getBoxesHiddenBySearch()
      {
         return _boxesHiddenBySearch ?? Array.Empty<DiscussionBox>();
      }

      private IEnumerable<DiscussionBox> getBoxesForSearch()
      {
         return sortBoxes(getVisibleBoxesOnAllPages().Concat(getBoxesHiddenBySearch()));
      }

      private IEnumerable<ITextControl> getControlsSuitableForSearch()
      {
         return getBoxesForSearch()
            .SelectMany(box => box.Controls.Cast<Control>())
            .Where(control => control is ITextControl)
            .Cast<ITextControl>()
            .ToArray(); // force immediate execution
      }

      private IEnumerable<DiscussionBox> sortBoxes(IEnumerable<DiscussionBox> boxes)
      {
         return _discussionSort?.Sort(boxes, x => x.Discussion.Notes) ?? Array.Empty<DiscussionBox>();
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

         Debug.Assert(isBoxAmongSearchResults(sender));
         _contentChanged?.Invoke(false /* Panel does not want to receive update from _searchFilter */);

         bool doesContentMismatchFilter =
            getVisibleBoxesOnCurrentPage().FirstOrDefault(box => !_displayFilter.DoesMatchFilter(box.Discussion)) != null;
         if (doesContentMismatchFilter)
         {
            ContentMismatchesFilter?.Invoke();
         }
      }

      private bool isBoxAmongSearchResults(DiscussionBox box)
      {
         return !getBoxesHiddenBySearch().Contains(box);
      }

      private void updateVisibilityOfBoxes()
      {
         int currentPage = _pageFilter.FilterState.Page; 

         bool isAllowedToDisplay(DiscussionBox box) =>
            _displayFilter.DoesMatchFilter(box.Discussion) && isBoxAmongSearchResults(box);

         bool isAllowedToDisplayOnCurrentPage(DiscussionBox box)
         {
            int? pageOpt = getPage(box);
            return pageOpt.HasValue && pageOpt.Value == currentPage;
         }

         // 1. Assign page to each box enabled by Filter
         IEnumerable<DiscussionBox> allSortedBoxes = sortBoxes(getAllBoxes());
         matchBoxesToPages(allSortedBoxes.Where(box => isAllowedToDisplay(box)));

         List<DiscussionBox> visibleBoxes = new List<DiscussionBox>();
         List<DiscussionBox> visibleBoxesOnCurrentPage = new List<DiscussionBox>();

         // 2. Collect all visible boxes in two collections and update Visible property
         foreach (DiscussionBox box in allSortedBoxes.Where(b => b?.Discussion != null))
         {
            bool isVisible = isAllowedToDisplay(box);
            bool isVisibleOnCurrentPage = isVisible && isAllowedToDisplayOnCurrentPage(box);
            if (isVisible)
            {
               visibleBoxes.Add(box);
            }
            if (isVisibleOnCurrentPage)
            {
               visibleBoxesOnCurrentPage.Add(box);
            }

            // Note that the following does not change Visible property value until Form gets Visible itself
            box.Visible = isVisibleOnCurrentPage;
         }

         // 3. Save collected boxes in class members
         _visibleBoxes = visibleBoxes.ToArray(); // force immediate execution
         _visibleBoxesOnCurrentPage = visibleBoxesOnCurrentPage.ToArray(); // force immediate execution

         // 4. Notify listeners
         ContentMatchesFilter?.Invoke();
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

         if (!control.Visible)
         {
            DiscussionBox box = control.Parent as DiscussionBox;
            if (box != null && _boxesToPages.TryGetValue(box, out int page))
            {
               PageChangeRequest?.Invoke(page);
            }
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

         if (!control.Focused)
         {
            control.Focus();
         }
      }

      private User _currentUser;
      private AvatarImageCache _avatarImageCache;
      private MergeRequestKey _mergeRequestKey;
      private User _mergeRequestAuthor;
      private IGitCommandService _git;
      private Shortcuts _shortcuts;
      private string _webUrl;
      private Action<string> _selectExternalNoteByUrl;
      private IEnumerable<User> _fullUserList;
      private IEnumerable<Project> _fullProjectList;
      private DiscussionSort _discussionSort;
      private DiscussionFilter _displayFilter; // filters out discussions by user preferences
      private DiscussionFilter _pageFilter; // filters out discussions by user preferences
      private DiscussionFilter _searchFilter; // filters out discussions by search
      private DiscussionLayout _discussionLayout;
      private AsyncDiscussionLoader _discussionLoader;
      private Action<bool> _contentChanged;

      /// <summary>
      /// Holds a control that had focus before we clicked on Find Next/Find Prev in order to continue search
      /// </summary>
      private Control _mostRecentFocusedDiscussionControl;
      private IEnumerable<DiscussionBox> _visibleBoxesOnCurrentPage;
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

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private const int MaxBoxYLocationPx = 30000;

      private int DiffContextBorderRadius => scale(10);
      private int DiscussionBoxTopMarginVertLayout => scale(40);
      private int DiscussionBoxTopMarginHorzLayout => scale(20);
      private int FirstDiscussionBoxTopMargin => scale(20);

      private RoundedPathCache _pathCache;
      private HtmlPanelEx _focusedNote;
      private IEnumerable<DiscussionBox> _boxesHiddenBySearch;

      DiffContextHelpers.EstimateWidthCache _estimatedWidthCache = new DiffContextHelpers.EstimateWidthCache();
   }
}

