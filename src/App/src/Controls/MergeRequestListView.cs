using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.CommonNative;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;
using ListViewSubItemInfo = mrHelper.App.Controls.MergeRequestListViewSubItemInfo;

namespace mrHelper.App.Controls
{
   class ListViewItemComparer : IComparer
   {
      public ListViewItemComparer(Func<ListViewItem, ListViewItem, int> comparisonFunction)
      {
         this._comparisonFunction = comparisonFunction;
      }

      public int Compare(object x, object y)
      {
         ListViewItem item1 = x as ListViewItem;
         ListViewItem item2 = y as ListViewItem;
         return _comparisonFunction(item1, item2);
      }

      private readonly Func<ListViewItem, ListViewItem, int> _comparisonFunction;
   }

   internal partial class MergeRequestListView : ListViewEx
   {
      internal event Action<ListView> ContentChanged;

      public MergeRequestListView()
      {
         OwnerDraw = true;
         _toolTip = new ListViewToolTip(this,
            getText, getToolTipText, getSubItemStringFormatFlags, getBounds, getForceShowToolTip);
         Tag = "DesignTimeName";
         _unmuteTimer.Tick += onUnmuteTimerTick;
         cleanUpMutedMergeRequests();
      }

      internal void Initialize(string hostname, bool doesSupportPin)
      {
         _hostname = hostname;
         _doesSupportPin = doesSupportPin;

         if (needShowGroups())
         {
            createGroups();
         }

         applySortModeFromConfiguration();
      }

      internal void SetDiffStatisticProvider(IDiffStatisticProvider diffStatisticProvider)
      {
         _diffStatisticProvider = diffStatisticProvider;
      }

      internal void SetCurrentUserGetter(Func<User> funcGetter)
      {
         _getCurrentUser = funcGetter;
      }

      internal void SetCollapsedProjects(HashSetWrapper<ProjectKey> collapsedProjects)
      {
         _collapsedProjects = collapsedProjects;
      }

      internal void SetIdentity(string identity)
      {
         _identity = identity;
      }

      internal void SetMutedMergeRequests(DictionaryWrapper<MergeRequestKey, DateTime> mutedMergeRequets)
      {
         _mutedMergeRequests = mutedMergeRequets;

         if (_mutedMergeRequests != null && !_unmuteTimer.Enabled)
         {
            _unmuteTimer.Start();
         }
         else if (_mutedMergeRequests == null && _unmuteTimer.Enabled)
         {
            _unmuteTimer.Stop();
         }
      }

      internal void SetPinChecker(Func<MergeRequestKey, bool> fnIsPinned)
      {
         _fnIsPinned = fnIsPinned;
      }

      internal void SetPinText(string pinText, string unpinText)
      {
         _pinText = pinText;
         _unpinText = unpinText;
      }

      internal void SetDataCache(DataCache dataCache)
      {
         _dataCache = dataCache;
      }

      internal void SetFilter(MergeRequestFilter filter)
      {
         _mergeRequestFilter = filter;
      }

      internal void SetOpenMergeRequestUrlCallback(Action<MergeRequestKey, string> callback)
      {
         _openMergeRequestUrlCallback = callback;
      }

      internal void SetTimeTrackingCheckingCallback(Func<MergeRequestKey, bool> callback)
      {
         _timeTrackingCheckingCallback = callback;
      }

      internal void SetColorScheme(ColorScheme colorScheme)
      {
         _colorScheme = colorScheme;
      }

      internal void SetExpressionResolver(ExpressionResolver expressionResolver)
      {
         _expressionResolver = expressionResolver;
      }

      internal void SetAvatarImageCache(AvatarImageCache avatarImageCache)
      {
         _avatarImageCache = avatarImageCache;
      }

      internal void DisableListView()
      {
         Enabled = false;
         DeselectAllListViewItems();
         Items.Clear();
      }

      internal void AssignContextMenu(MergeRequestListViewContextMenu contextMenu)
      {
         if (ContextMenuStrip != null)
         {
            ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
            ContextMenuStrip.Dispose();
         }

         ContextMenuStrip = contextMenu;

         if (ContextMenuStrip != null)
         {
            contextMenu.Opening += ContextMenuStrip_Opening;
         }
      }

      internal void DeselectAllListViewItems()
      {
         foreach (ListViewItem item in Items)
         {
            item.Selected = false;
         }
      }

      internal bool SelectMergeRequest(MergeRequestKey? mrk, bool exact)
      {
         if (!mrk.HasValue)
         {
            if (Items.Count < 1)
            {
               return false;
            }

            Items[0].Selected = true;
            ensureSelectionIsVisible();
            return true;
         }

         if (isGroupCollapsed(mrk.Value.ProjectKey))
         {
            setGroupCollapsing(mrk.Value.ProjectKey, false);
         }

         foreach (ListViewItem item in Items)
         {
            if (isSummaryItem(item))
            {
               continue;
            }

            FullMergeRequestKey key = (FullMergeRequestKey)(item.Tag);
            if (mrk.Value.ProjectKey.Equals(key.ProjectKey)
             && mrk.Value.IId == key.MergeRequest.IId)
            {
               item.Selected = true;
               ensureSelectionIsVisible();
               return true;
            }
         }

         if (exact)
         {
            return false;
         }

         if (needShowGroups())
         {
            // selected an item from the proper group
            foreach (ListViewGroup group in Groups)
            {
               if (mrk.Value.ProjectKey.MatchProject(group.Name) && group.Items.Count > 0)
               {
                  group.Items[0].Selected = true;
                  ensureSelectionIsVisible();
                  return true;
               }
            }

            // select whatever
            foreach (ListViewGroup group in Groups)
            {
               if (group.Items.Count > 0)
               {
                  group.Items[0].Selected = true;
                  ensureSelectionIsVisible();
                  return true;
               }
            }
         }
         else if (Items.Count > 0)
         {
            Items[0].Selected = true;
            ensureSelectionIsVisible();
            return true;
         }

         return false;
      }

      internal void createGroupForProject(ProjectKey projectKey, bool isSortNeeded)
      {
         Debug.Assert(needShowGroups());

         ListViewGroup group = new ListViewGroup(projectKey.ProjectName, projectKey.ProjectName)
         {
            Tag = projectKey
         };
         updateGroupCaption(group);
         if (!isSortNeeded)
         {
            // user defines how to sort group here
            Groups.Add(group);
            return;
         }

         // sort groups alphabetically
         int indexToInsert = Groups.Count;
         for (int iGroup = 0; iGroup < Groups.Count; ++iGroup)
         {
            if (projectKey.ProjectName.CompareTo(Groups[iGroup].Header) < 0)
            {
               indexToInsert = iGroup;
               break;
            }
         }
         Groups.Insert(indexToInsert, group);
      }

      internal void updateGroups()
      {
         if (!needShowGroups())
         {
            return;
         }

         IMergeRequestCache mergeRequestCache = _dataCache.MergeRequestCache;
         Debug.Assert(mergeRequestCache != null);

         // Add missing project groups
         IEnumerable<ProjectKey> allProjects = mergeRequestCache.GetProjects();
         foreach (ProjectKey projectKey in allProjects)
         {
            if (!Groups.Cast<ListViewGroup>().Any(x => projectKey.Equals((ProjectKey)(x.Tag))))
            {
               createGroupForProject(projectKey, true);
            }
         }

         // Remove deleted project groups
         for (int index = Groups.Count - 1; index >= 0; --index)
         {
            ListViewGroup group = Groups[index];
            if (!allProjects.Any(x => x.Equals((ProjectKey)group.Tag)))
            {
               Groups.Remove(group);
            }
         }
      }

      internal bool IsGroupCollapsed(ProjectKey projectKey)
      {
         return isGroupCollapsed(projectKey);
      }

      internal void UpdateItems()
      {
         initColorOrderCache();

         if (needShowGroups())
         {
            updateItemsWithGroups();
         }
         else
         {
            updateItemsWithoutGroups();
         }
         Sort();

         clearColorOrderCache();
      }

      internal void updateItemsWithoutGroups()
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         BeginUpdate();

         // Add missing merge requests and update existing ones
         foreach (FullMergeRequestKey fmk in getAllMergeRequests())
         {
            ListViewItem item = Items.Cast<ListViewItem>().FirstOrDefault(
               x => ((FullMergeRequestKey)x.Tag).Equals(fmk)); // item=`null` if not found
            if (item == null)
            {
               item = createListViewMergeRequestItem(fmk);
               Items.Add(item);
            }
            else
            {
               item.Tag = fmk;
            }
            setListViewSubItemsTags(item, fmk);
         }

         // Remove deleted merge requests
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            if (!getAllProjectItemsFromCache(fmk.ProjectKey).Any(x => x.MergeRequest.IId == fmk.MergeRequest.IId))
            {
               Items.RemoveAt(index);
            }
         }

         // Hide filtered ones
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            if (!doesMatchFilter(fmk))
            {
               Items.RemoveAt(index);
            }
         }

         recalcRowHeightForMergeRequestListView();

         EndUpdate();
      }

      internal void updateItemsWithGroups()
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         IEnumerable<ProjectKey> projectKeys = Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);

         BeginUpdate();

         updateGroups();

         // Add missing merge requests and update existing ones
         foreach (ProjectKey projectKey in projectKeys)
         {
            if (isGroupCollapsed(projectKey))
            {
               continue;
            }
            foreach (FullMergeRequestKey fmk in getAllProjectItemsFromCache(projectKey))
            {
               ListViewItem item = Items.Cast<ListViewItem>().FirstOrDefault(
                  x => ((FullMergeRequestKey)x.Tag).Equals(fmk)); // item=`null` if not found
               if (item == null)
               {
                  item = createListViewMergeRequestItem(fmk);
                  Items.Add(item);
               }
               else
               {
                  item.Tag = fmk;
               }
               setListViewSubItemsTags(item, fmk);
            }
         }

         // Delete summary items from groups that are no longer collapsed
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            if (!isGroupCollapsed(Items[index].Group) && isSummaryItem(Items[index]))
            {
               Items.RemoveAt(index);
            }
         }

         // Remove normal items from collapsed groups
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            if (isGroupCollapsed(Items[index].Group))
            {
               Items.RemoveAt(index);
            }
         }

         // Create summary items
         foreach (ListViewGroup group in Groups)
         {
            if (isGroupCollapsed(group))
            {
               string[] subitems = Enumerable.Repeat(String.Empty, Columns.Count).ToArray();
               FullMergeRequestKey fmk = new FullMergeRequestKey(getGroupProjectKey(group), null);
               ListViewItem item = createListViewMergeRequestItem(fmk);
               Items.Add(item);
               setListViewSubItemsTagsForSummary(item);
            }
         }

         // Remove deleted merge requests
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            if (!isGroupCollapsed(fmk.ProjectKey)
             && !getAllProjectItemsFromCache(fmk.ProjectKey).Any(x => x.MergeRequest.IId == fmk.MergeRequest.IId))
            {
               Items.RemoveAt(index);
            }
         }

         // Hide filtered ones
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            bool isCollapsed = isGroupCollapsed(fmk.ProjectKey);
            bool removeItem = false;
            if (isCollapsed)
            {
               bool isThereAnyMatchingItem = getMatchingFilterProjectItems(fmk.ProjectKey).Any();
               removeItem = !isThereAnyMatchingItem;
            }
            else
            {
               bool doesItemMatchFilter = doesMatchFilter(fmk);
               removeItem = !doesItemMatchFilter;
            }
            if (removeItem)
            {
               Items.RemoveAt(index);
            }
         }

         // update a number of MR which is probably displayed
         _suppressSelectionChange = true;
         try
         {
            Groups.Cast<ListViewGroup>().ToList().ForEach(group => updateGroupCaption(group));
         }
         finally
         {
            _suppressSelectionChange = false;
         }

         recalcRowHeightForMergeRequestListView();

         EndUpdate();
      }

      internal MergeRequestListViewContextMenu GetContextMenu()
      {
         return ContextMenuStrip as MergeRequestListViewContextMenu;
      }

      internal FullMergeRequestKey? GetSelectedMergeRequest()
      {
         if (SelectedIndices.Count > 0)
         {
            ListViewItem item = SelectedItems[0];
            return isSummaryItem(item) ? new FullMergeRequestKey?() : (FullMergeRequestKey)(item.Tag);
         }
         return null;
      }

      internal void MuteSelectedMergeRequestFor(TimeSpan timeSpan)
      {
         FullMergeRequestKey? selectedMergeRequest = GetSelectedMergeRequest();
         if (selectedMergeRequest.HasValue)
         {
            muteMergeRequestFor(selectedMergeRequest.Value, timeSpan);
            onContentChanged();
            Trace.TraceInformation(
               "[MergeRequestListView] Muted MR with IId {0} (project {1}) in LV \"{2}\" for {3}",
               selectedMergeRequest.Value.MergeRequest.IId,
               selectedMergeRequest.Value.ProjectKey.ProjectName,
               getIdentity(),
               timeSpan.ToString());
         }
      }

      internal void UnmuteSelectedMergeRequest()
      {
         FullMergeRequestKey? selectedMergeRequest = GetSelectedMergeRequest();
         if (selectedMergeRequest.HasValue && unmuteMergeRequest(selectedMergeRequest.Value))
         {
            onContentChanged();
            Trace.TraceInformation(
               "[MergeRequestListView] Unmuted MR with IId {0} (project {1}) in LV \"{2}\"",
               selectedMergeRequest.Value.MergeRequest.IId,
               selectedMergeRequest.Value.ProjectKey.ProjectName,
               getIdentity());
         }
      }

      internal Color? GetSummaryColor()
      {
         IEnumerable<FullMergeRequestKey> keys = excludeMuted(getAllMergeRequests());
         return getMergeRequestCollectionColor(keys, EColorSchemeItemsKind.Preview);
      }

      protected override void Dispose(bool disposing)
      {
         _unmuteTimer.Tick -= onUnmuteTimerTick;
         _unmuteTimer.Stop();
         _unmuteTimer.Dispose();
         _toolTip?.Dispose();
         SmallImageList?.Dispose();
         base.Dispose(disposing);
      }

      private bool _processingHandleCreated = false;
      protected override void OnHandleCreated(EventArgs e)
      {
         _processingHandleCreated = true;
         try
         {
            base.OnHandleCreated(e);
         }
         finally
         {
            _processingHandleCreated = false;
         }
         restoreColumns();
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         base.OnVisibleChanged(e);
         if (Visible)
         {
            restoreColumns();
         }
      }

      protected override void OnMouseLeave(EventArgs e)
      {
         // this callback is called not only when mouse leaves the list view so let's check if we need to cancel tooltip
         _toolTip.CancelIfNeeded(Cursor.Position);

         base.OnMouseLeave(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         ListViewHitTestInfo hit = HitTest(e.Location);
         _toolTip.UpdateOnMouseMove(e.Location);
         Cursor = getCursor(hit);

         base.OnMouseMove(e);
      }

      protected override void OnMouseDown(MouseEventArgs e)
      {
         ListViewHitTestInfo testAtCursor = HitTest(e.Location);
         if (needShowGroups())
         {
            int headerHeight = LogicalToDeviceUnits(GroupHeaderHeight);
            ListViewHitTestInfo testBelowCursor = HitTest(e.Location.X, e.Location.Y + headerHeight);
            if (testAtCursor.Item == null && testBelowCursor.Item != null)
            {
               ProjectKey projectKey = getGroupProjectKey(testBelowCursor.Item.Group);
               setGroupCollapsing(projectKey, !isGroupCollapsed(projectKey));
               return;
            }
         }
         onUrlClick(testAtCursor);
         base.OnMouseDown(e);
      }

      protected override void OnMouseDoubleClick(MouseEventArgs e)
      {
         base.OnMouseDoubleClick(e);
         if (GetSelectedMergeRequest() != null)
         {
            GetContextMenu()?.LaunchDefaultAction();
         }
         else if (needShowGroups())
         {
            ListViewHitTestInfo testAtCursor = HitTest(e.Location);
            ProjectKey projectKey = getGroupProjectKey(testAtCursor.Item.Group);
            if (isGroupCollapsed(projectKey))
            {
               setGroupCollapsing(projectKey, false);
            }
         }
      }

      protected override void OnColumnClick(ColumnClickEventArgs e)
      {
         base.OnColumnClick(e);
         ColumnType columnType = (ColumnType)Columns[e.Column].Tag;
         setSortedByColumn(columnType);
      }

      protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
      {
         base.OnColumnWidthChanged(e);
         if (!_restoringColumns && ! _processingHandleCreated)
         {
            saveColumnWidths();
         }
      }

      protected override void WndProc(ref Message message)
      {
         if (message.Msg == NativeMethods.WM_NOTIFY)
         {
            if (Win32Tools.IsDoubleClickOnDivider(WinFormsHelpers.GetNotifyMessageHeader(message)))
            {
               int columnIndex = WinFormsHelpers.GetHeaderControlNotifyMessage(message).iItem;
               bool isValidColumnIndex = Columns != null && Columns.Count > 0 && columnIndex < Columns.Count;
               if (isValidColumnIndex && autoResizeColumnHeaderWidth(Columns[columnIndex]))
               {
                  return; // We did it. Do not pass message to default WndProc.
               }
            }
         }

         base.WndProc(ref message);
      }

      protected override void OnColumnReordered(ColumnReorderedEventArgs e)
      {
         base.OnColumnReordered(e);
         if (!_restoringColumns && ! _processingHandleCreated)
         {
            saveColumIndices(e.OldDisplayIndex, e.NewDisplayIndex);
         }
      }

      protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
      {
         if (_suppressSelectionChange)
         {
            return;
         }
         base.OnItemSelectionChanged(e);
      }

      protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
      {
         if (e.Header.ListView == null)
         {
            return;
         }

         using (Brush brush = new SolidBrush(Color.DarkGray))
         {
            // Fill rectangle with dark color
            e.Graphics.FillRectangle(brush, e.Bounds);
         }
         using (Brush brush = new SolidBrush(Color.Gray))
         {
            // Fill rectangle with lighter color but leave 1px for a "border".
            // 1px is enough no matter which DPI is used.
            Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1 /* px */, e.Bounds.Height);
            e.Graphics.FillRectangle(SystemBrushes.ButtonFace, rect);
         }

         bool isSortedByThisColumn = e.ColumnIndex == getColumnByType(getSortedByColumn()).Index;
         if (e.ColumnIndex == getColumnByType(ColumnType.Color).Index)
         {
            Color? penColor = isSortedByThisColumn ? Color.Black : new Color?();
            Color fillColor = GetSummaryColor() ?? Color.Gray;
            drawColorColumnEllipse(e.Graphics, e.Bounds, fillColor, penColor);
         }
         else
         {
            using (Font font = getColumnHeaderFont(isSortedByThisColumn))
            {
               using (StringFormat format = new StringFormat(StringFormatFlags.LineLimit))
               {
                  format.Trimming = StringTrimming.EllipsisCharacter;
                  e.Graphics.DrawString(e.Header.Text, font, Brushes.Black, e.Bounds, format);
               }
            }
         }
      }

      protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
      {
         base.OnDrawSubItem(e);

         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Rectangle bounds = e.Bounds;
         if (e.ColumnIndex == 0 && e.Item.ListView.Columns[0].DisplayIndex != 0)
         {
            bounds = WinFormsHelpers.GetFirstColumnCorrectRectangle(e.Item.ListView, e.Item);
         }

         bool isSelected = e.Item.Selected;
         FullMergeRequestKey fmk = (FullMergeRequestKey)(e.Item.Tag);
         Color defaultColor = Color.Transparent;
         Color backgroundColor = isMuted(fmk) ? defaultColor : getMergeRequestColor(fmk, defaultColor);
         WinFormsHelpers.FillRectangle(e, bounds, backgroundColor, isSelected);

         ColumnType columnType = getColumnType(e.SubItem);
         using (StringFormat format = new StringFormat(getSubItemStringFormatFlags(e.SubItem)))
         {
            format.Trimming = StringTrimming.EllipsisCharacter;

            string text = ((ListViewSubItemInfo)(e.SubItem.Tag)).Text;
            bool isClickable = ((ListViewSubItemInfo)(e.SubItem.Tag)).Clickable;
            if (columnType == ColumnType.Color)
            {
               Color color = getMergeRequestColor(fmk, Color.Transparent, EColorSchemeItemsKind.Preview);
               drawColorColumnEllipse(e.Graphics, bounds, color, null);
               if (isPinnedToMe(fmk))
               {
                  drawColorColumnImage(e.Graphics, bounds, Properties.Resources.pin_transparent_alpha);
               }
            }
            else if (isClickable)
            {
               using (Font font = new Font(e.Item.ListView.Font, FontStyle.Underline))
               {
                  Brush brush = Brushes.Blue;
                  e.Graphics.DrawString(text, font, brush, bounds, format);
               }
            }
            else if (isSelected && columnType == ColumnType.Labels)
            {
               Color defaultLabelColor = SystemColors.Window;
               Color color = isMuted(fmk) ? defaultLabelColor : getMergeRequestColor(fmk, defaultLabelColor);
               using (Brush brush = new SolidBrush(color))
               {
                  e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
               }
            }
            else if (columnType == ColumnType.Avatar)
            {
               if (!isSummaryItem(e.Item))
               {
                  Image avatar = _avatarImageCache.GetAvatar(fmk.MergeRequest.Author);
                  drawAvatar(e.Graphics, e.Bounds, avatar);
               }
            }
            else if (columnType == ColumnType.Resolved)
            {
               using (Brush brush = new SolidBrush(getDiscussionCountColor(fmk, isSelected)))
               {
                  e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
               }
            }
            else if (columnType == ColumnType.TotalTime)
            {
               Brush brush = text == Constants.NotAllowedTimeTrackingText ? Brushes.Gray : Brushes.Black;
               e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
            }
            else
            {
               Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
               e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, bounds, format);
            }
         }
      }

      private StringFormatFlags getSubItemStringFormatFlags(ListViewItem.ListViewSubItem subItem)
      {
         ColumnType columnType = getColumnType(subItem);
         bool isWrappableColumnItem =
               columnType == ColumnType.Title
            || columnType == ColumnType.SourceBranch
            || columnType == ColumnType.TargetBranch
            || columnType == ColumnType.Jira
            || columnType == ColumnType.Author
            || columnType == ColumnType.Project;
         bool needWordWrap = isWrappableColumnItem && Program.Settings.WordWrapLongRows;
         return needWordWrap ? StringFormatFlags.LineLimit : StringFormatFlags.NoWrap;
      }

      bool _restoringColumns = false;
      private void restoreColumns()
      {
         _restoringColumns = true;
         try
         {
            Dictionary<string, int> widths = Program.Settings == null
               ? null : ConfigurationHelper.GetColumnWidths(Program.Settings, getIdentity());
            if (widths != null)
            {
               setColumnWidths(widths);
            }

            Dictionary<string, int> indices = Program.Settings == null
               ? null : ConfigurationHelper.GetColumnIndices(Program.Settings, getIdentity());
            if (indices != null)
            {
               setColumnIndices(indices);
            }
         }
         finally
         {
            _restoringColumns = false;
         }
      }

      private void ensureSelectionIsVisible()
      {
         if (SelectedIndices.Count > 0)
         {
            EnsureVisible(SelectedIndices[0]);
         }
      }

      private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
      {
         MergeRequestListViewContextMenu contextMenu = ((MergeRequestListViewContextMenu)(sender));
         FullMergeRequestKey? fmkOpt = GetSelectedMergeRequest();
         if (WinFormsHelpers.TestListViewHeaderHit(this, Cursor.Position) || !fmkOpt.HasValue)
         {
            contextMenu.DisableAll();
         }
         else
         {
            FullMergeRequestKey fmk = fmkOpt.Value;
            contextMenu.EnableAll();
            contextMenu.SetUnmuteActionEnabled(isMuted(fmk));
            contextMenu.SetExcludeAbilityState(!isExplicitlyExcluded(fmk));
            contextMenu.SetPinItemText(isPinned(fmk) ? _unpinText : _pinText);
         }
         contextMenu.UpdateItemState();

         _toolTip.Cancel();
      }

      private float getColorEllipsePreferrableWidth()
      {
         double textHeight = WinFormsHelpers.GetFontSizeInPixels(this);
         return (float)(textHeight + 0.20 * textHeight);
      }

      private void drawColorColumnEllipse(Graphics g, Rectangle bounds, Color fillColor, Color? penColor)
      {
         int colWidth = getColumnWidth(ColumnType.Color);
         float ellipseWidthByText = getColorEllipsePreferrableWidth();
         float ellipseWidth = Math.Min(ellipseWidthByText, colWidth);
         if (ellipseWidth < ellipseWidthByText / 2)
         {
            return;
         }

         float ellipseHeight = ellipseWidth;
         float ellipseX = (bounds.Width - ellipseWidth) / 2;
         float ellipseY = (bounds.Height - ellipseHeight) / 2;
         using (Brush ellipseBrush = new SolidBrush(fillColor))
         {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            RectangleF ellipseRect = new RectangleF(
               bounds.X + ellipseX, bounds.Y + ellipseY, ellipseWidth, ellipseHeight);
            g.FillEllipse(ellipseBrush, ellipseRect);

            if (penColor.HasValue)
            {
               using (Pen ellipsePen = new Pen(penColor.Value, 2))
               {
                  g.DrawEllipse(ellipsePen, ellipseRect);
               }
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
         }
      }

      private float getColorImagePreferrableWidth()
      {
         double textHeight = WinFormsHelpers.GetFontSizeInPixels(this);
         return (float)(textHeight - 0.20 * textHeight);
      }

      private void drawColorColumnImage(Graphics g, Rectangle bounds, Image image)
      {
         Debug.Assert(image != null);

         float imageWidthByText = getColorImagePreferrableWidth();
         int colWidth = getColumnWidth(ColumnType.Color);
         float imageWidth = Math.Min(imageWidthByText, colWidth);
         if (imageWidth < imageWidthByText / 2)
         {
            return;
         }

         float imageHeight = imageWidth;
         float imageX = (bounds.Width - imageWidth) / 2;
         float imageY = (bounds.Height - imageHeight) / 2;

         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

         RectangleF imageRect = new RectangleF(
            bounds.X + imageX, bounds.Y + imageY, imageWidth, imageHeight);
         g.DrawImage(image, imageRect);

         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
      }

      private void drawAvatar(Graphics g, Rectangle bounds, Image avatar)
      {
         int colWidth = getColumnWidth(ColumnType.Avatar);
         int rowHeight = WinFormsHelpers.GetListViewRowHeight(this);
         int rectangleSide = Math.Min(colWidth, rowHeight);
         if (rectangleSide < rowHeight / 2)
         {
            return;
         }

         Size avatarSize = getAvatarSize(rectangleSide);
         int imageX = (colWidth - avatarSize.Width) / 2;
         int imageY = (rowHeight - avatarSize.Height) / 2;
         Rectangle imageRect = new Rectangle(
            bounds.X + imageX, bounds.Y + imageY, avatarSize.Width, avatarSize.Height);

         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
         WinFormsHelpers.DrawClippedCircleImage(g, avatar, imageRect);
         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
      }

      private Size getAvatarSize(int rectangleSide)
      {
         int imageWidth = (int)Math.Ceiling(rectangleSide - 0.05 * rectangleSide); // 5% less
         int imageHeight = imageWidth;
         return new Size(imageWidth, imageHeight);
      }

      enum EColorSchemeItemsKind
      {
         All,
         Preview,
         Sorting
      }

      private IEnumerable<ColorSchemeItem> getColorSchemeItems(EColorSchemeItemsKind kind)
      {
         switch (kind)
         {
            case EColorSchemeItemsKind.All:
               return _colorScheme?.GetColors("MergeRequests");

            case EColorSchemeItemsKind.Preview:
               return getColorSchemeItems(EColorSchemeItemsKind.All).Where(item => item.UseForPreview);

            case EColorSchemeItemsKind.Sorting:
               return getColorSchemeItems(EColorSchemeItemsKind.All).Where(item => item.UseForSorting);

            default:
               Debug.Assert(false);
               break;
         }
         return Array.Empty<ColorSchemeItem>();
      }

      private Color getMergeRequestColor(FullMergeRequestKey fmk, Color defaultColor,
         EColorSchemeItemsKind kind = EColorSchemeItemsKind.All)
      {
         IEnumerable<FullMergeRequestKey> mergeRequests = isSummaryKey(fmk)
            ? getMatchingFilterProjectItems(fmk.ProjectKey)
            : new List<FullMergeRequestKey>{ fmk };
         return getMergeRequestCollectionColor(mergeRequests, kind) ?? defaultColor;
      }

      private Color? getMergeRequestCollectionColor(IEnumerable<FullMergeRequestKey> keys, EColorSchemeItemsKind kind)
      {
         var colorSchemeItems = getColorSchemeItems(kind);
         return colorSchemeItems?
            .FirstOrDefault(colorSchemeItem =>
            {
               IEnumerable<string> conditions = colorSchemeItem.Conditions;
               IEnumerable<string> resolvedConditions = conditions?
                  .Select(condition => _expressionResolver.Resolve(condition)) ?? Array.Empty<string>();
               return keys.Any(fmk => checkConditions(resolvedConditions, fmk));
            })?
            .Color;
      }

      private bool checkConditions(IEnumerable<string> conditions, FullMergeRequestKey fmk)
      {
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         IEnumerable<User> approvedBy = _dataCache?.MergeRequestCache?.GetApprovals(mrk)?.Approved_By?
            .Select(item => item.User) ?? Array.Empty<User>();
         IEnumerable<string> labels = fmk.MergeRequest.Labels;
         User author = fmk.MergeRequest.Author;
         bool isExcluded = !wouldMatchFilter(fmk);
         bool isTrackingTime = _timeTrackingCheckingCallback(mrk);
         bool isPinned = isPinnedToMe(fmk);
         return GitLabClient.Helpers.CheckConditions(conditions, approvedBy, labels, author,
            isExcluded, isTrackingTime, isPinned);
      }

      private Color getDiscussionCountColor(FullMergeRequestKey fmk, bool isSelected)
      {
         if (isSummaryKey(fmk))
         {
            return Color.Black;
         }

         DiscussionCount dc = _dataCache?.DiscussionCache?.GetDiscussionCount(
            new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId)) ?? default(DiscussionCount);

         if (dc.Status != DiscussionCount.EStatus.Ready || dc.Resolvable == null || dc.Resolved == null)
         {
            return Color.Black;
         }

         if (dc.Resolvable.Value == dc.Resolved.Value)
         {
            return isSelected ? Color.SpringGreen : Color.Green;
         }

         Debug.Assert(dc.Resolvable.Value > dc.Resolved.Value);
         return isSelected ? Color.Orange : Color.Red;
      }

      private string getDiscussionCount(MergeRequestKey mrk)
      {
         if (_dataCache?.DiscussionCache == null)
         {
            return "N/A";
         }

         DiscussionCount dc = _dataCache.DiscussionCache.GetDiscussionCount(mrk);
         switch (dc.Status)
         {
            case DiscussionCount.EStatus.NotAvailable:
               return "N/A";

            case DiscussionCount.EStatus.Loading:
               return "Loading...";

            case DiscussionCount.EStatus.Ready:
               return String.Format("{0} / {1}", dc.Resolved.Value, dc.Resolvable.Value);
         }

         Debug.Assert(false);
         return "N/A";
      }

      private string getSize(MergeRequestKey key)
      {
         if (_diffStatisticProvider == null)
         {
            return String.Empty;
         }

         DiffStatistic? diffStatistic = _diffStatisticProvider.GetStatistic(key, out string errMsg);
         return diffStatistic?.Format() ?? errMsg;
      }

      private string getTotalTimeText(MergeRequestKey key, User author)
      {
         ITotalTimeCache totalTimeCache = _dataCache?.TotalTimeCache;
         if (totalTimeCache == null)
         {
            return String.Empty;
         }

         User currentUser = _getCurrentUser();
         bool isTimeTrackingAllowed = TimeTrackingHelpers.IsTimeTrackingAllowed(
            author, key.ProjectKey.HostName, currentUser);
         return TimeTrackingHelpers.ConvertTotalTimeToText(totalTimeCache.GetTotalTime(key), isTimeTrackingAllowed);
      }

      private DateTime? getRefreshedTime(MergeRequestKey key)
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return null;
         }

         return mergeRequestCache.GetMergeRequestRefreshTime(key);
      }

      private string getRefreshed(MergeRequestKey key, bool tooltipText)
      {
         DateTime? refreshedTime = getRefreshedTime(key);
         return tooltipText ? TimeUtils.DateTimeOptToString(refreshedTime)
                            : TimeUtils.DateTimeOptToStringAgo(refreshedTime);
      }

      private DateTime? getLatestCommitTime(MergeRequestKey key)
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return null;
         }

         Commit latestCommit = mergeRequestCache
            .GetCommits(key)?
            .OrderByDescending(commit => commit.Created_At)
            .FirstOrDefault();
         return latestCommit?.Created_At;
      }

      private string getActivities(DateTime createdAt, MergeRequestKey key, bool tooltipText)
      {
         DateTime? latestCommitTime = getLatestCommitTime(key);
         return String.Format("Created: {0}\r\nLatest commit: {1}",
            tooltipText ? TimeUtils.DateTimeToString(createdAt)
                        : TimeUtils.DateTimeToStringAgo(createdAt),
            tooltipText ? TimeUtils.DateTimeOptToString(latestCommitTime)
                        : TimeUtils.DateTimeOptToStringAgo(latestCommitTime));
      }

      private string getJiraTask(MergeRequest mergeRequest) => GitLabClient.Helpers.GetJiraTask(mergeRequest);

      private string getJiraTaskUrl(MergeRequest mergeRequest) => GitLabClient.Helpers.GetJiraTaskUrl(
         mergeRequest, Program.ServiceManager.GetJiraServiceUrl());

      private string getId(MergeRequest mergeRequest)
      {
         return Program.Settings.ShowHiddenMergeRequestIds
            ? String.Format("{0}\r\n({1})", mergeRequest.IId, mergeRequest.Id) : String.Format("{0}", mergeRequest.IId);
      }

      private void recalcRowHeightForMergeRequestListView()
      {
         if (Items.Count == 0)
         {
            return;
         }

         int getMaxRowCountInColumn(ColumnType type)
         {
            int labelsColumnIndex = getColumnByType(type).Index;
            IEnumerable<string> rows = Items.Cast<ListViewItem>()
               .Select(item => ((ListViewSubItemInfo)(item.SubItems[labelsColumnIndex].Tag)).Text);
            IEnumerable<int> rowCounts = rows
                  .Select(thing => thing.Count(y => y == '\n'));
            return rowCounts.Max() + 1;
         }

         int maxLineCount = Math.Max(getMaxRowCountInColumn(ColumnType.Labels),
                                     getMaxRowCountInColumn(ColumnType.Author));
         WinFormsHelpers.SetListViewRowHeight(this, maxLineCount);
      }

      private ColumnHeader getColumnByType(ColumnType columnType)
      {
         return Columns
            .Cast<ColumnHeader>()
            .SingleOrDefault(x => (ColumnType)x.Tag == columnType);
      }

      private IEnumerable<FullMergeRequestKey> getAllProjectItemsFromCache(ProjectKey projectKey)
      {
         return _dataCache?
            .MergeRequestCache?
            .GetMergeRequests(projectKey)
            .Select(mergeRequest => new FullMergeRequestKey(projectKey, mergeRequest))
            ?? Array.Empty<FullMergeRequestKey>();
      }

      private IEnumerable<FullMergeRequestKey> getMatchingFilterProjectItems(ProjectKey projectKey)
      {
         return getAllProjectItemsFromCache(projectKey).Where(fmk => doesMatchFilter(fmk));
      }

      private IEnumerable<FullMergeRequestKey> getAllMergeRequests()
      {
         return _dataCache?.MergeRequestCache?.GetProjects()
            .SelectMany(projectKey => getAllProjectItemsFromCache(projectKey)) ?? Array.Empty<FullMergeRequestKey>();
      }

      private bool doesMatchFilter(FullMergeRequestKey fmk)
      {
         return _mergeRequestFilter?.DoesMatchFilter(fmk) ?? true;
      }

      private bool wouldMatchFilter(FullMergeRequestKey fmk)
      {
         if (_mergeRequestFilter == null)
         {
            return true;
         }

         MergeRequestFilterState filterState = new MergeRequestFilterState(
            _mergeRequestFilter.Filter.Keywords.ToString(), FilterState.Enabled);
         MergeRequestFilter filter = new MergeRequestFilter(filterState);
         return filter.DoesMatchFilter(fmk);
      }

      private ListViewItem createListViewMergeRequestItem(FullMergeRequestKey fmk)
      {
         ListViewGroup group = needShowGroups() ? Groups[fmk.ProjectKey.ProjectName] : null;
         string[] subitems = Enumerable.Repeat(String.Empty, Columns.Count).ToArray();
         ListViewItem item = new ListViewItem(subitems, group)
         {
            Tag = fmk
         };
         return item;
      }

      private void setListViewSubItemsTags(ListViewItem item, FullMergeRequestKey fmk)
      {
         Debug.Assert(item.ListView == this);
         Debug.Assert(!isSummaryItem(item));

         MergeRequest mr = fmk.MergeRequest;
         string author = String.Format("{0}\n({1}{2})", mr.Author.Name,
            Constants.AuthorLabelPrefix, mr.Author.Username);

         IEnumerable<string> groupedLabels = GitLabClient.Helpers.GroupLabels(fmk,
            Program.ServiceManager.GetUnimportantSuffices(), _getCurrentUser());
         Dictionary<bool, string> labels = new Dictionary<bool, string>
         {
            [false] = StringUtils.JoinSubstringsLimited(groupedLabels, MaxListViewRows, MoreListViewRowsHint),
            [true] = StringUtils.JoinSubstrings(groupedLabels)
         };

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, mr.IId);
         setSubItemTag(item, ColumnType.IId, x => getId(mr), () => mr.Web_Url);
         setSubItemTag(item, ColumnType.Color);
         setSubItemTag(item, ColumnType.Avatar, x => author);
         setSubItemTag(item, ColumnType.Author, x => author);
         setSubItemTag(item, ColumnType.Title, x => mr.Title);
         setSubItemTag(item, ColumnType.Labels, x => labels[x]);
         setSubItemTag(item, ColumnType.Size, x => getSize(mrk));
         setSubItemTag(item, ColumnType.Jira, x => getJiraTask(mr), () => getJiraTaskUrl(mr));
         setSubItemTag(item, ColumnType.TotalTime, x => getTotalTimeText(mrk, mr.Author));
         setSubItemTag(item, ColumnType.SourceBranch, x => mr.Source_Branch);
         setSubItemTag(item, ColumnType.TargetBranch, x => mr.Target_Branch);
         setSubItemTag(item, ColumnType.State, x => mr.State);
         setSubItemTag(item, ColumnType.Resolved, x => getDiscussionCount(mrk));
         setSubItemTag(item, ColumnType.RefreshTime, x => getRefreshed(mrk, x));
         setSubItemTag(item, ColumnType.Activities, x => getActivities(mr.Created_At, mrk, x));
         setSubItemTag(item, ColumnType.Project, x => fmk.ProjectKey.ProjectName);
      }

      private Dictionary<ListViewItem, int> _colorOrderCache = new Dictionary<ListViewItem, int>();
      private int getColorOrder(ListViewItem item)
      {
         if (_colorOrderCache.TryGetValue(item, out int value))
         {
            return value;
         }
         int r = getColorOrder((FullMergeRequestKey)item.Tag);
         _colorOrderCache[item] = r;
         return r;
      }

      private void initColorOrderCache()
      {
         foreach (ListViewItem item in Items)
         {
            int colorOrder = getColorOrder(item);
            _colorOrderCache[item] = colorOrder;
         }
      }

      private void clearColorOrderCache()
      {
         _colorOrderCache.Clear();
      }

      private int getColorOrder(FullMergeRequestKey fmk)
      {
         Color color = getMergeRequestColor(fmk, Color.Transparent, EColorSchemeItemsKind.Sorting);
         ColorSchemeItem[] items = getColorSchemeItems(EColorSchemeItemsKind.Sorting).ToArray();
         for (int iColor = 0; iColor < items.Length; ++iColor)
         {
            if (color == items[iColor].Color)
            {
               return items.Length - iColor;
            }
         }
         return -1;
      }

      private void setSubItemTag(ListViewItem item, ColumnType columnType,
         Func<bool, string> p1 = null, Func<string> p2 = null)
      {
         ColumnHeader columnHeader = getColumnByType(columnType);
         if (columnHeader == null)
         {
            return;
         }

         ListViewSubItemInfo subItemInfo = new ListViewSubItemInfo(p1, p2, columnType);
         item.SubItems[columnHeader.Index].Tag = subItemInfo;
      }

      private ListViewSubItemInfo getSubItemTag(ListViewItem item, ColumnType columnType)
      {
         ColumnHeader columnHeader = getColumnByType(columnType);
         if (columnHeader == null)
         {
            return null;
         }

         return (ListViewSubItemInfo)item.SubItems[columnHeader.Index]?.Tag;
      }

      private void setListViewSubItemsTagsForSummary(ListViewItem item)
      {
         Debug.Assert(needShowGroups());
         Debug.Assert(isSummaryItem(item));

         IEnumerable<FullMergeRequestKey> fullKeys = getMatchingFilterProjectItems(getGroupProjectKey(item.Group));

         User currentUser = fullKeys.Any() ? _getCurrentUser() : null;
         Debug.Assert(fullKeys.All(key => _getCurrentUser().Id == (currentUser?.Id ?? 0)));
         IEnumerable<string> groupedLabels = GitLabClient.Helpers.GroupLabels(fullKeys,
            Program.ServiceManager.GetUnimportantSuffices(), currentUser);
         Dictionary<bool, string> labels = new Dictionary<bool, string>
         {
            [false] = groupedLabels.Any() ? AllListViewRowsHint : String.Empty,
            [true] = StringUtils.JoinSubstrings(groupedLabels.OrderBy(group => group))
         };

         Dictionary<bool, string> titles = new Dictionary<bool, string>
         {
            [false] = String.Format("{0} item(s)", fullKeys.Count()),
            [true] = StringUtils.JoinSubstrings(fullKeys.Select(x => x.MergeRequest.Title).OrderBy(title => title))
         };

         IEnumerable<string> allAuthors = fullKeys.Select(key => key.MergeRequest.Author.Name);
         IEnumerable<string> distinctAuthors = allAuthors.Distinct();
         Dictionary<bool, string> authors = new Dictionary<bool, string>
         {
            [false] = String.Format("{0} author(s)", distinctAuthors.Count()),
            [true] = StringUtils.JoinSubstrings(distinctAuthors.OrderBy(author => author))
         };

         IEnumerable<string> allJiraTasks = fullKeys.Select(key => GitLabClient.Helpers.GetJiraTask(key.MergeRequest));
         IEnumerable<string> distinctJiraTasks = allJiraTasks.Distinct().Where(id => !String.IsNullOrEmpty(id));
         Dictionary<bool, string> jiraTasks = new Dictionary<bool, string>
         {
            [false] = distinctJiraTasks.Any() ? String.Format("{0} task(s)", distinctJiraTasks.Count()) : String.Empty,
            [true] = StringUtils.JoinSubstrings(distinctJiraTasks.OrderBy(jira => jira))
         };

         IEnumerable<string> allSourceBranches = fullKeys.Select(key => key.MergeRequest.Source_Branch);
         IEnumerable<string> distinctSourceBranches = allSourceBranches.Distinct();
         Dictionary<bool, string> sourceBranches = new Dictionary<bool, string>
         {
            [false] = String.Format("{0} branch(es)", distinctSourceBranches.Count()),
            [true] = StringUtils.JoinSubstrings(distinctSourceBranches.OrderBy(branch => branch))
         };

         IEnumerable<string> allTargetBranches = fullKeys.Select(key => key.MergeRequest.Target_Branch);
         IEnumerable<string> distinctTargetBranches = allTargetBranches.Distinct();
         Dictionary<bool, string> targetBranches = new Dictionary<bool, string>
         {
            [false] = String.Format("{0} branch(es)", distinctTargetBranches.Count()),
            [true] = StringUtils.JoinSubstrings(distinctTargetBranches.OrderBy(branch => branch))
         };

         setSubItemTag(item, ColumnType.IId);
         setSubItemTag(item, ColumnType.Color);
         setSubItemTag(item, ColumnType.Avatar);
         setSubItemTag(item, ColumnType.Author, x => authors[x]);
         setSubItemTag(item, ColumnType.Title, x => titles[x]);
         setSubItemTag(item, ColumnType.Labels, x => labels[x]);
         setSubItemTag(item, ColumnType.Size);
         setSubItemTag(item, ColumnType.Jira, x => jiraTasks[x]);
         setSubItemTag(item, ColumnType.TotalTime);
         setSubItemTag(item, ColumnType.SourceBranch, x => sourceBranches[x]);
         setSubItemTag(item, ColumnType.TargetBranch, x => targetBranches[x]);
         setSubItemTag(item, ColumnType.State);
         setSubItemTag(item, ColumnType.Resolved);
         setSubItemTag(item, ColumnType.RefreshTime);
         setSubItemTag(item, ColumnType.Activities);
         setSubItemTag(item, ColumnType.Project, x => getGroupProjectKey(item.Group).ProjectName);
      }

      private void setColumnWidths(Dictionary<string, int> widths)
      {
         foreach (ColumnHeader column in Columns)
         {
            string columnName = column.Tag.ToString();
            if (widths.ContainsKey(columnName))
            {
               column.Width = widths[columnName];
            }
         }
      }

      private int getColumnWidth(ColumnType columnType)
      {
         foreach (ColumnHeader column in Columns)
         {
            if ((ColumnType)column.Tag == columnType)
            {
               return column.Width;
            }
         }
         return 0;
      }

      private void setColumnIndices(Dictionary<string, int> indices)
      {
         try
         {
            WinFormsHelpers.ReorderListViewColumns(this, indices);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("[MergeRequestListView] Cannot restore list view column display indices", ex);
            ConfigurationHelper.SetColumnIndices(Program.Settings,
               WinFormsHelpers.GetListViewDisplayIndices(this), getIdentity());
         }
      }

      private void saveColumIndices(int oldIndex, int newIndex)
      {
         var indices = WinFormsHelpers.GetListViewDisplayIndicesOnColumnReordered(this, oldIndex, newIndex);
         ConfigurationHelper.SetColumnIndices(Program.Settings, indices, getIdentity());
      }

      private void saveColumnWidths()
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (ColumnHeader column in Columns)
         {
            columnWidths[column.Tag.ToString()] = column.Width;
         }
         ConfigurationHelper.SetColumnWidths(Program.Settings, columnWidths, getIdentity());
      }

      private ProjectKey getGroupProjectKey(ListViewGroup group)
      {
         return ((ProjectKey)group.Tag);
      }

      private bool isGroupCollapsed(ProjectKey projectKey)
      {
         return needShowGroups() && _collapsedProjects.Data.Contains(projectKey);
      }

      private bool isGroupCollapsed(ListViewGroup group)
      {
         return isGroupCollapsed(getGroupProjectKey(group));
      }

      private void setGroupCollapsing(ProjectKey projectKey, bool collapse)
      {
         bool isCollapsed = _collapsedProjects.Data.Contains(projectKey);
         if (isCollapsed == collapse)
         {
            return;
         }

         if (collapse)
         {
            _collapsedProjects.Add(projectKey);
         }
         else
         {
            _collapsedProjects.Remove(projectKey);
         }

         NativeMethods.LockWindowUpdate(Handle);
         int vScrollPosition = Win32Tools.GetVerticalScrollPosition(Handle);
         onContentChanged();
         Win32Tools.SetVerticalScrollPosition(Handle, vScrollPosition);
         NativeMethods.LockWindowUpdate(IntPtr.Zero);
      }

      private void updateGroupCaption(ListViewGroup group)
      {
         if (group == null)
         {
            return;
         }

         string action = isGroupCollapsed(group) ? "expand" : "collapse";
         string groupHeader = String.Format(
            "{0} -- click to {1} {2} item(s)",
            group.Name, action, getMatchingFilterProjectItems(getGroupProjectKey(group)).Count());
         if (groupHeader != group.Header)
         {
            group.Header = groupHeader;
         }
      }

      private void muteMergeRequestFor(FullMergeRequestKey fmk, TimeSpan timeSpan)
      {
         if (!isMuteSupported())
         {
            return;
         }

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         _mutedMergeRequests[mrk] = DateTime.Now + timeSpan;
      }

      private bool unmuteMergeRequest(FullMergeRequestKey fmk)
      {
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         return unmuteMergeRequest(mrk);
      }

      private bool unmuteMergeRequest(MergeRequestKey mrk)
      {
         if (!isMuteSupported())
         {
            return false;
         }

         if (!_mutedMergeRequests.Data.ContainsKey(mrk))
         {
            return false;
         }

         _mutedMergeRequests.Remove(mrk);
         return true;
      }

      private IEnumerable<FullMergeRequestKey> excludeMuted(IEnumerable<FullMergeRequestKey> keys)
      {
         return keys.Where(fmk => !isMuted(fmk));
      }

      private bool isMuted(FullMergeRequestKey fmk)
      {
         if (!isMuteSupported())
         {
            return false;
         }

         if (isSummaryKey(fmk))
         {
            return getMatchingFilterProjectItems(fmk.ProjectKey).Any(key => isMuted(key));
         }

         return _mutedMergeRequests.Data
            .Any(mrk => mrk.Key.IId == fmk.MergeRequest.IId
                     && mrk.Key.ProjectKey.Equals(fmk.ProjectKey));
      }

      private bool isExplicitlyExcluded(FullMergeRequestKey fmk)
      {
         return _mergeRequestFilter?.Filter.Keywords.IsExcluded(fmk.MergeRequest.Id.ToString()) ?? false;
      }

      private void onUnmuteTimerTick(object sender, EventArgs e)
      {
         if (cleanUpMutedMergeRequests())
         {
            onContentChanged();
         }
      }

      private bool cleanUpMutedMergeRequests()
      {
         if (!isMuteSupported())
         {
            return false;
         }

         // temporary copy because original collection is changed inside the loop
         Dictionary<MergeRequestKey, DateTime> temp =
            _mutedMergeRequests.Data.ToDictionary(kv => kv.Key, kv => kv.Value);

         bool changed = false;
         foreach (KeyValuePair<MergeRequestKey, DateTime> mr in temp)
         {
            DateTime now = DateTime.Now;
            if (mr.Value <= now)
            {
               bool unmuteSucceeded = unmuteMergeRequest(mr.Key);
               Debug.Assert(unmuteSucceeded);
               changed |= unmuteSucceeded;
            }
         }
         return changed;
      }

      private bool isMuteSupported()
      {
         return _mutedMergeRequests != null;
      }

      private void onContentChanged()
      {
         ContentChanged?.Invoke(this);
      }

      private string getIdentity()
      {
         return _identity;
      }

      private ColumnType getColumnType(ListViewItem.ListViewSubItem subItem)
      {
         return (subItem.Tag as ListViewSubItemInfo).ColumnType;
      }

      private string getToolTipText(ListViewItem.ListViewSubItem subItem)
      {
         return (subItem.Tag as ListViewSubItemInfo).TooltipText;
      }

      private string getText(ListViewItem.ListViewSubItem subItem)
      {
         return (subItem.Tag as ListViewSubItemInfo).Text;
      }

      private Rectangle getBounds(ListViewItem.ListViewSubItem subItem)
      {
         var width = getColumnWidth((subItem.Tag as ListViewSubItemInfo).ColumnType);
         return new Rectangle(subItem.Bounds.X, subItem.Bounds.Y, width, subItem.Bounds.Height);
      }

      private bool getForceShowToolTip(ListViewItem.ListViewSubItem subItem)
      {
         ListViewSubItemInfo info = (subItem.Tag as ListViewSubItemInfo);
         ColumnType columnType = info.ColumnType;
         switch (columnType)
         {
            case ColumnType.Labels:
               return getText(subItem).Contains(MoreListViewRowsHint)
                   || getText(subItem).Contains(AllListViewRowsHint);
            case ColumnType.Activities:
               return true;
         }
         bool containsUrl = !String.IsNullOrWhiteSpace(info.Url);
         return containsUrl;
      }

      private static bool isSummaryKey(FullMergeRequestKey fmk)
      {
         return fmk.MergeRequest == null;
      }

      private static bool isSummaryItem(ListViewItem item)
      {
         return item != null && isSummaryKey((FullMergeRequestKey)(item.Tag));
      }

      private void onUrlClick(ListViewHitTestInfo hit)
      {
         if (hit.SubItem == null)
         {
            return;
         }

         ListViewSubItemInfo info = (ListViewSubItemInfo)(hit.SubItem.Tag);
         if (!info.Clickable)
         {
            return;
         }

         if (info.ColumnType == ColumnType.IId && _openMergeRequestUrlCallback != null)
         {
            FullMergeRequestKey? fmkOpt = (FullMergeRequestKey?)(hit.Item.Tag);
            if (fmkOpt.HasValue)
            {
               FullMergeRequestKey fmk = fmkOpt.Value;
               MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
               _openMergeRequestUrlCallback(mrk, info.Url);
            }
            else
            {
               Debug.Assert(false);
            }
            return;
         }

         UrlHelper.OpenBrowser(info.Url);
      }

      private bool autoResizeColumnHeaderWidth(ColumnHeader column)
      {
         int? width = getColumnAutoWidth(column);
         if (width.HasValue)
         {
            column.Width = width.Value;
         }
         return width.HasValue;
      }

      private int? getColumnAutoWidth(ColumnHeader column)
      {
         ColumnType? columnType = getColumnTypeByName(column.Text);
         if (!columnType.HasValue)
         {
            return new int?();
         }

         switch (columnType.Value)
         {
            case ColumnType.Color:
               return (int)Math.Ceiling(Math.Max(getColorEllipsePreferrableWidth(), getColorImagePreferrableWidth()));

            case ColumnType.Avatar:
               {
                  int rowHeight = WinFormsHelpers.GetListViewRowHeight(this);
                  Size avatarSize = getAvatarSize(rowHeight);
                  return avatarSize.Width + AvatarPaddingX;
               }
         }
         return getColumnAutoWidthByContent(column);
      }

      private int getColumnAutoWidthByContent(ColumnHeader column)
      {
         ColumnType? columnTypeOpt = getColumnTypeByName(column.Text);
         Debug.Assert(columnTypeOpt.HasValue);
         ColumnType columnType = columnTypeOpt.Value;

         int MaxAllowedColumnWidth = scale(1000);

         using (Graphics g = CreateGraphics())
         {
            using (StringFormat noTrimmingFormat = new StringFormat() { Trimming = StringTrimming.None })
            {
               float headerWidth = 0;
               using (Font headerFont = getColumnHeaderFont(getSortedByColumn() == columnType))
               {
                  headerWidth = g.MeasureString(column.Text, headerFont, MaxAllowedColumnWidth, noTrimmingFormat).Width;
               }

               Font contentFont = this.Font;
               float widestContentWidth = Items
                  .Cast<ListViewItem>()
                  .Select(item => getSubItemTag(item, columnType))
                  .Max(subItem => g.MeasureString(subItem.Text, contentFont, MaxAllowedColumnWidth, noTrimmingFormat).Width);

               return Convert.ToInt32(Math.Ceiling(Math.Max(headerWidth, widestContentWidth)));
            }
         }
      }

      private Font getColumnHeaderFont(bool isSortedByThisColumn)
      {
         FontStyle fontStyle = !isSortedByThisColumn ? FontStyle.Regular : FontStyle.Bold;
         return new Font(this.Font, fontStyle);
      }

      private void setSortedByColumn(ColumnType columnType)
      {
         bool prevNeedShowGroups = needShowGroups();
         if (getSortedByColumn() == columnType)
         {
            SortingDirection sortingDirection = getSortingDirection();
            sortingDirection = sortingDirection == SortingDirection.Ascending ?
               SortingDirection.Descending : SortingDirection.Ascending;
            ConfigurationHelper.SetSortingDirection(Program.Settings, sortingDirection, getIdentity());
         }
         else
         {
            ColumnHeader columnHeader = getColumnByType(columnType);
            ConfigurationHelper.SetSortedByColumn(Program.Settings, columnHeader.Text, getIdentity());
         }

         if (prevNeedShowGroups != needShowGroups())
         {
            if (needShowGroups())
            {
               createGroups();
            }
            else
            {
               Groups.Clear();
            }
            Items.Clear();
            UpdateItems();
         }

         applySortModeFromConfiguration();
         ensureSelectionIsVisible();
         Invalidate(true);
      }

      private void applySortModeFromConfiguration()
      {
         var comparisonFunction = getComparisonFunction(getSortedByColumn(), getSortingDirection());
         ListViewItemSorter = new ListViewItemComparer(comparisonFunction);
      }

      private void createGroups()
      {
         Debug.Assert(needShowGroups());
         IEnumerable<string> projectNames = ConfigurationHelper.GetEnabledProjects(_hostname, Program.Settings);
         foreach (string projectName in projectNames)
         {
            ProjectKey projectKey = new ProjectKey(_hostname, projectName);
            createGroupForProject(projectKey, false);
         }
      }

      private ColumnType? getColumnTypeByName(string columnTypeName)
      {
         if (columnTypeName == null)
         {
            return new ColumnType?();
         }

         ColumnHeader columnHeader = Columns
            .Cast<ColumnHeader>()
            .SingleOrDefault(x => x.Text == columnTypeName);
         return columnHeader == null ? new ColumnType?() : (ColumnType)columnHeader.Tag;
      }

      private ColumnType getSortedByColumn()
      {
         const ColumnType DefaultColumnTypeForSorting = ColumnType.Color;

         string columnTypeName = Program.Settings != null ?
            ConfigurationHelper.GetSortedByColumn(Program.Settings, getIdentity()) : null;
         ColumnType? columnType = getColumnTypeByName(columnTypeName);
         return columnType == null ? DefaultColumnTypeForSorting : columnType.Value;
      }

      private SortingDirection getSortingDirection()
      {
         const SortingDirection DefaultSortingDirection = SortingDirection.Descending;

         return Program.Settings == null
            ? DefaultSortingDirection
            : ConfigurationHelper.GetSortingDirection(Program.Settings, getIdentity());
      }

      private bool needShowGroups()
      {
         return getSortedByColumn() == ColumnType.Project;
      }

      private bool isPinned(FullMergeRequestKey fmk)
      {
         if (_fnIsPinned == null || isGroupCollapsed(fmk.ProjectKey))
         {
            return false;
         }

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         return _fnIsPinned(mrk);
      }

      // is pinned to this list view instance
      private bool isPinnedToMe(FullMergeRequestKey fmk)
      {
         return _doesSupportPin && isPinned(fmk);
      }

      private int compare(ListViewItem item1, ListViewItem item2, ColumnType columnType)
      {
         FullMergeRequestKey fmk1 = ((FullMergeRequestKey)item1.Tag);
         FullMergeRequestKey fmk2 = ((FullMergeRequestKey)item2.Tag);
         if (fmk1.MergeRequest == null)
         {
            return fmk2.MergeRequest == null ? 0 : -1;
         }
         else if (fmk2.MergeRequest == null)
         {
            return 1;
         }

         int fallbackToIIdComparison() => compare(item1, item2, ColumnType.IId);

         MergeRequestKey mrk1 = new MergeRequestKey(fmk1.ProjectKey, fmk1.MergeRequest.IId);
         MergeRequestKey mrk2 = new MergeRequestKey(fmk2.ProjectKey, fmk2.MergeRequest.IId);
         switch (columnType)
         {
            case ColumnType.IId:
               {
                  if (mrk1.IId == mrk2.IId)
                  {
                     Debug.Assert(fmk1.MergeRequest.Id != fmk2.MergeRequest.Id);
                     return fmk1.MergeRequest.Id > fmk2.MergeRequest.Id ? 1 : -1;
                  }
                  return mrk1.IId > mrk2.IId ? 1 : -1;
               }

            case ColumnType.Activities:
               {
                  DateTime latestActivity1 = getLatestCommitTime(mrk1) ?? fmk1.MergeRequest.Created_At;
                  DateTime latestActivity2 = getLatestCommitTime(mrk2) ?? fmk2.MergeRequest.Created_At;
                  return latestActivity1 == latestActivity2 ? fallbackToIIdComparison()
                      : (latestActivity1 > latestActivity2 ? 1 : -1);
               }

            case ColumnType.Color:
               {
                  bool isPinned1 = isPinnedToMe(fmk1);
                  bool isPinned2 = isPinnedToMe(fmk2);
                  if (isPinned1 != isPinned2)
                  {
                     return isPinned1 ? 1 : -1;
                  }
                  int colorOrder1 = getColorOrder(item1);
                  int colorOrder2 = getColorOrder(item2);
                  return colorOrder1 == colorOrder2 ? fallbackToIIdComparison() : (colorOrder1 > colorOrder2 ? 1 : -1);
               }

            case ColumnType.Size:
               {
                  DiffStatistic? diffStatistic1 = _diffStatisticProvider?.GetStatistic(mrk1, out string _);
                  DiffStatistic? diffStatistic2 = _diffStatisticProvider?.GetStatistic(mrk2, out string _);
                  if (diffStatistic1.HasValue && diffStatistic2.HasValue)
                  {
                     int size1 = diffStatistic1.Value.Insertions + diffStatistic1.Value.Deletions;
                     int size2 = diffStatistic2.Value.Insertions + diffStatistic2.Value.Deletions;
                     return size1 == size2 ? fallbackToIIdComparison() : (size1 > size2 ? 1 : -1);
                  }
                  return diffStatistic1.HasValue ? 1 : (diffStatistic2.HasValue ? -1 : fallbackToIIdComparison());
               }

            case ColumnType.TotalTime:
               {
                  double? totalTime1 = _dataCache?.TotalTimeCache?.GetTotalTime(mrk1).Amount?.TotalSeconds;
                  double? totalTime2 = _dataCache?.TotalTimeCache?.GetTotalTime(mrk2).Amount?.TotalSeconds;
                  if (totalTime1.HasValue && totalTime2.HasValue)
                  {
                     return totalTime1 == totalTime2 ? 0 : (totalTime1 > totalTime2 ? 1 : -1);
                  }
                  return totalTime1.HasValue ? 1 : (totalTime2.HasValue ? -1 : fallbackToIIdComparison());
               }

            case ColumnType.Avatar:
               return compare(item1, item2, ColumnType.Author);

            default:
               {
                  ListViewSubItemInfo key1 = getSubItemTag(item1, columnType);
                  ListViewSubItemInfo key2 = getSubItemTag(item2, columnType);
                  int cmp = String.Compare(key1.Text, key2.Text, StringComparison.OrdinalIgnoreCase);
                  return cmp == 0 ? fallbackToIIdComparison() : cmp;
               }
         }
      }

      private Func<ListViewItem, ListViewItem, int> getComparisonFunction(
         ColumnType columnType, SortingDirection sortingDirection)
      {
         columnType = needShowGroups() ? ColumnType.IId : columnType;
         return (item1, item2) =>
         {
            ListViewSubItemInfo key1 = getSubItemTag(item1, columnType);
            ListViewSubItemInfo key2 = getSubItemTag(item2, columnType);
            if (key1 == null)
            {
               return key2 == null ? 0 : -1;
            }
            else if (key2 == null)
            {
               return 1;
            }
            int comparisonResult = compare(item1, item2, columnType);
            if (comparisonResult == 0)
            {
               if (columnType == ColumnType.Color)
               {
                  comparisonResult = -compare(item1, item2, ColumnType.Author);
               }
               else
               {
                  comparisonResult = compare(item1, item2, ColumnType.IId);
               }
            }
            return sortingDirection == SortingDirection.Ascending ? comparisonResult : -comparisonResult;
         };
      }

      private static Cursor getCursor(ListViewHitTestInfo hit)
      {
         if (hit.SubItem != null)
         {
            ListViewSubItemInfo info = (ListViewSubItemInfo)(hit.SubItem.Tag);
            return info.Clickable ? Cursors.Hand : Cursors.Default;
         }
         return Cursors.Default;
      }

      private readonly ListViewToolTip _toolTip;
      private IDiffStatisticProvider _diffStatisticProvider;
      private Func<User> _getCurrentUser;
      private DataCache _dataCache;
      private MergeRequestFilter _mergeRequestFilter;
      private ColorScheme _colorScheme;
      private bool _suppressSelectionChange;
      private HashSetWrapper<ProjectKey> _collapsedProjects;
      private DictionaryWrapper<MergeRequestKey, DateTime> _mutedMergeRequests;
      private ExpressionResolver _expressionResolver;
      private string _identity;
      private Action<MergeRequestKey, string> _openMergeRequestUrlCallback;
      private string _hostname;
      private bool _doesSupportPin;
      private Func<MergeRequestKey, bool> _timeTrackingCheckingCallback;
      private AvatarImageCache _avatarImageCache;
      private Func<MergeRequestKey, bool> _fnIsPinned;
      private string _pinText;
      private string _unpinText;
      private static readonly int MaxListViewRows = 3;
      private static readonly string MoreListViewRowsHint = "See more labels in tooltip";
      private static readonly string AllListViewRowsHint = "See all labels in tooltip";

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private int GroupHeaderHeight => scale(20); // found experimentally

      private int AvatarPaddingX => scale(10);

      private static readonly int UnmuteTimerInterval = 60 * 1000; // 1 minute
      private readonly Timer _unmuteTimer = new Timer
      {
         Interval = UnmuteTimerInterval
      };
   }
}

