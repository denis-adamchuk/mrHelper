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
using ListViewSubItemInfo = mrHelper.App.Controls.MergeRequestListViewSubItemInfo;

namespace mrHelper.App.Controls
{
   class ListViewItemComparer : IComparer
   {
      public ListViewItemComparer()
      {
      }

      public int Compare(object x, object y)
      {
         ListViewItem item1 = x as ListViewItem;
         ListViewItem item2 = y as ListViewItem;
         FullMergeRequestKey key1 = (FullMergeRequestKey)item1.Tag;
         FullMergeRequestKey key2 = (FullMergeRequestKey)item2.Tag;
         if (key1.MergeRequest == null && key2.MergeRequest == null)
         {
            return 0;
         }
         else if (key1.MergeRequest == null && key2.MergeRequest != null)
         {
            return -1;
         }
         else if (key1.MergeRequest != null && key2.MergeRequest == null)
         {
            return 1;
         }
         Debug.Assert(key1.MergeRequest != null && key2.MergeRequest != null);
         int id1 = key1.MergeRequest.Id;
         int id2 = key2.MergeRequest.Id;
         if (id1 > id2)
         {
            return -1;
         }
         if (id1 < id2)
         {
            return 1;
         }
         return 0;
      }
   }

   public partial class MergeRequestListView : ListViewEx
   {
      private static readonly int MaxListViewRows = 3;
      private static readonly string MoreListViewRowsHint = "See more labels in tooltip";

      public MergeRequestListView()
      {
         ListViewItemSorter = new ListViewItemComparer();
         _toolTip = new MergeRequestListViewToolTip(this);
         Tag = "DesignTimeName";
      }

      internal void Initialize()
      {
         setColumnWidths(ConfigurationHelper.GetColumnWidths(Program.Settings, getIdentity()));
         setColumnIndices(ConfigurationHelper.GetColumnIndices(Program.Settings, getIdentity()));
      }

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         _toolTip?.Dispose();
         base.Dispose(disposing);
      }

      internal void SetDiffStatisticProvider(IDiffStatisticProvider diffStatisticProvider)
      {
         _diffStatisticProvider = diffStatisticProvider;
      }

      internal void SetCurrentUserGetter(Func<string, User> funcGetter)
      {
         _getCurrentUser = funcGetter;
      }

      internal void SetPersistentStorage(PersistentStorage persistentStorage)
      {
         if (_persistentStorage != null)
         {
            _persistentStorage.OnDeserialize -= onDeserialize;
            _persistentStorage.OnSerialize -= onSerialize;
         }
         _persistentStorage = persistentStorage;
         _persistentStorage.OnDeserialize += onDeserialize;
         _persistentStorage.OnSerialize += onSerialize;
      }

      internal void SetDataCache(DataCache dataCache)
      {
         _dataCache = dataCache;
      }

      internal void SetFilter(MergeRequestFilter filter)
      {
         _mergeRequestFilter = filter;
      }

      internal void SetColorScheme(ColorScheme colorScheme)
      {
         _colorScheme = colorScheme;
      }

      internal void DisableListView()
      {
         Enabled = false;
         DeselectAllListViewItems();
         Items.Clear();
      }

      internal void AssignContextMenu(MergeRequestListViewContextMenu contextMenu)
      {
         ContextMenuStrip = contextMenu;
         ContextMenuStrip.Opening += ContextMenuStrip_Opening;
      }

      private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
      {
         MergeRequestListViewContextMenu contextMenu = ((MergeRequestListViewContextMenu)(sender));
         if (GetSelectedMergeRequest() == null)
         {
            contextMenu.DisableAll();
         }
         else
         {
            contextMenu.EnableAll();
         }
         contextMenu.UpdateItemState();
      }

      internal MergeRequestListViewContextMenu GetContextMenu()
      {
         return ContextMenuStrip as MergeRequestListViewContextMenu;
      }

      internal void EnsureSelectionVisible()
      {
         if (SelectedIndices.Count > 0)
         {
            EnsureVisible(SelectedIndices[0]);
         }
      }

      internal void EnsureGroupIsNotCollapsed(ProjectKey projectKey)
      {
         setGroupCollapsing(projectKey, false);
      }

      internal FullMergeRequestKey? GetSelectedMergeRequest()
      {
         if (SelectedIndices.Count > 0)
         {
            ListViewItem item = SelectedItems[0];
            return isSummaryItem(item) ? new Nullable<FullMergeRequestKey>() : (FullMergeRequestKey)(item.Tag);
         }
         return null;
      }

      internal IEnumerable<FullMergeRequestKey> GetMatchingFilterMergeRequests()
      {
         List<FullMergeRequestKey> keys = new List<FullMergeRequestKey>();
         foreach (ListViewGroup group in Groups)
         {
            keys.AddRange(getMatchingFilterProjectItems(getGroupProjectKey(group)));
         }
         return keys;
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
            return true;
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
               return true;
            }
         }

         if (exact)
         {
            return false;
         }

         // selected an item from the proper group
         foreach (ListViewGroup group in Groups)
         {
            if (mrk.Value.ProjectKey.MatchProject(group.Name) && group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         // select whatever
         foreach (ListViewGroup group in Groups)
         {
            if (group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         return false;
      }

      public void CreateGroupForProject(ProjectKey projectKey, bool isSortNeeded)
      {
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

      public void UpdateGroups()
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         BeginUpdate();

         // Add missing project groups
         IEnumerable<ProjectKey> allProjects = mergeRequestCache.GetProjects();
         foreach (ProjectKey projectKey in allProjects)
         {
            if (!Groups.Cast<ListViewGroup>().Any(x => projectKey.Equals((ProjectKey)(x.Tag))))
            {
               CreateGroupForProject(projectKey, true);
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

         EndUpdate();
      }

      public void UpdateItems()
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         IEnumerable<ProjectKey> projectKeys = Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);

         BeginUpdate();

         // Add missing merge requests and update existing ones
         foreach (ProjectKey projectKey in projectKeys)
         {
            if (isGroupCollapsed(projectKey))
            {
               continue;
            }
            foreach (FullMergeRequestKey fmk in getAllProjectItems(projectKey))
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
             && !getAllProjectItems(fmk.ProjectKey).Any(x => x.MergeRequest.IId == fmk.MergeRequest.IId))
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
               bool doesItemMatchFilter = doesMatchFilter(fmk.MergeRequest);
               removeItem = !doesItemMatchFilter;
            }
            if (removeItem)
            {
               Items.RemoveAt(index);
            }
         }

         // update a number of MR which is probably displayed
         foreach (ListViewGroup group in Groups)
         {
            updateGroupCaption(group);
         }

         recalcRowHeightForMergeRequestListView();

         EndUpdate();
      }

      private ListViewItem createListViewMergeRequestItem(FullMergeRequestKey fmk)
      {
         ListViewGroup group = Groups[fmk.ProjectKey.ProjectName];
         string[] subitems = Enumerable.Repeat(String.Empty, Columns.Count).ToArray();
         ListViewItem item = new ListViewItem(subitems, group)
         {
            Tag = fmk
         };
         return item;
      }

      void setSubItemTag(ListViewItem item, string columnTag, ListViewSubItemInfo subItemInfo)
      {
         ColumnHeader columnHeader = getColumnByTag(columnTag);
         if (columnHeader == null)
         {
            return;
         }

         item.SubItems[columnHeader.Index].Tag = subItemInfo;
      }

      private void setListViewSubItemsTags(ListViewItem item, FullMergeRequestKey fmk)
      {
         Debug.Assert(item.ListView == this);
         Debug.Assert(!isSummaryItem(item));

         MergeRequest mr = fmk.MergeRequest;
         string author = String.Format("{0}\n({1}{2})", mr.Author.Name,
            Constants.AuthorLabelPrefix, mr.Author.Username);

         IEnumerable<string> groupedLabels = GitLabClient.Helpers.GroupLabels(fmk,
            Program.ServiceManager.GetUnimportantSuffices(), _getCurrentUser(fmk.ProjectKey.HostName));
         Dictionary<bool, string> labels = new Dictionary<bool, string>
         {
            [false] = StringUtils.JoinSubstringsLimited(groupedLabels, MaxListViewRows, MoreListViewRowsHint),
            [true] = StringUtils.JoinSubstrings(groupedLabels)
         };

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, mr.IId);
         setSubItemTag(item, "IId", new ListViewSubItemInfo(x => mr.IId.ToString(), () => mr.Web_Url));
         setSubItemTag(item, "Author", new ListViewSubItemInfo(x => author, () => String.Empty));
         setSubItemTag(item, "Title", new ListViewSubItemInfo(x => mr.Title, () => String.Empty));
         setSubItemTag(item, "Labels", new ListViewSubItemInfo(x => labels[x], () => String.Empty));
         setSubItemTag(item, "Size", new ListViewSubItemInfo(x => getSize(mrk), () => String.Empty));
         setSubItemTag(item, "Jira", new ListViewSubItemInfo(x => getJiraTask(mr), () => getJiraTaskUrl(mr)));
         setSubItemTag(item, "TotalTime", new ListViewSubItemInfo(x => getTotalTimeText(mrk, mr.Author), () => String.Empty));
         setSubItemTag(item, "SourceBranch", new ListViewSubItemInfo(x => mr.Source_Branch, () => String.Empty));
         setSubItemTag(item, "TargetBranch", new ListViewSubItemInfo(x => mr.Target_Branch, () => String.Empty));
         setSubItemTag(item, "State", new ListViewSubItemInfo(x => mr.State, () => String.Empty));
         setSubItemTag(item, "Resolved", new ListViewSubItemInfo(x => getDiscussionCount(mrk), () => String.Empty));
         setSubItemTag(item, "RefreshTime", new ListViewSubItemInfo(x => getRefreshed(mrk, x), () => String.Empty));
         setSubItemTag(item, "Activities", new ListViewSubItemInfo(x => getActivities(mr.Created_At, mrk, x), () => String.Empty));
      }

      private void recalcRowHeightForMergeRequestListView()
      {
         if (Items.Count == 0)
         {
            return;
         }

         int getMaxRowCountInColumn(string columnName)
         {
            int labelsColumnIndex = getColumnByTag(columnName).Index;
            IEnumerable<string> rows = Items.Cast<ListViewItem>()
               .Select(item => ((ListViewSubItemInfo)(item.SubItems[labelsColumnIndex].Tag)).Text);
            IEnumerable<int> rowCounts = rows
                  .Select(thing => thing.Count(y => y == '\n'));
            return rowCounts.Max() + 1;
         }

         int maxLineCount = Math.Max(getMaxRowCountInColumn("Labels"), getMaxRowCountInColumn("Author"));
         setListViewRowHeight(this, maxLineCount);
      }

      private void setListViewSubItemsTagsForSummary(ListViewItem item)
      {
         Debug.Assert(isSummaryItem(item));

         IEnumerable<FullMergeRequestKey> fullKeys = getMatchingFilterProjectItems(getGroupProjectKey(item.Group));

         User currentUser = fullKeys.Any() ? _getCurrentUser(fullKeys.First().ProjectKey.HostName) : null;
         Debug.Assert(fullKeys.All(key => _getCurrentUser(key.ProjectKey.HostName).Id == (currentUser?.Id ?? 0)));
         IEnumerable<string> groupedLabels = GitLabClient.Helpers.GroupLabels(fullKeys,
            Program.ServiceManager.GetUnimportantSuffices(), currentUser);
         Dictionary<bool, string> labels = new Dictionary<bool, string>
         {
            [false] = groupedLabels.Any() ? "See all labels in tooltip" : String.Empty,
            [true] = StringUtils.JoinSubstrings(groupedLabels.OrderBy(group => group))
         };

         Dictionary<bool, string> titles = new Dictionary<bool, string>
         {
            [false] = String.Format("{0} item(s)", fullKeys.Count()),
            [true] = StringUtils.JoinSubstrings(fullKeys.Select(fmk => fmk.MergeRequest.Title).OrderBy(title => title))
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

         setSubItemTag(item, "IId", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "Author", new ListViewSubItemInfo(x => authors[x], () => String.Empty));
         setSubItemTag(item, "Title", new ListViewSubItemInfo(x => titles[x], () => String.Empty));
         setSubItemTag(item, "Labels", new ListViewSubItemInfo(x => labels[x], () => String.Empty));
         setSubItemTag(item, "Size", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "Jira", new ListViewSubItemInfo(x => jiraTasks[x], () => String.Empty));
         setSubItemTag(item, "TotalTime", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "SourceBranch", new ListViewSubItemInfo(x => sourceBranches[x], () => String.Empty));
         setSubItemTag(item, "TargetBranch", new ListViewSubItemInfo(x => targetBranches[x], () => String.Empty));
         setSubItemTag(item, "State", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "Resolved", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "RefreshTime", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
         setSubItemTag(item, "Activities", new ListViewSubItemInfo(x => String.Empty, () => String.Empty));
      }

      ColumnHeader getColumnByTag(string tag)
      {
         return Columns
            .Cast<ColumnHeader>()
            .SingleOrDefault(x => x.Tag.ToString() == tag);
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

      string getSize(MergeRequestKey key)
      {
         if (_diffStatisticProvider == null)
         {
            return String.Empty;
         }

         DiffStatistic? diffStatistic = _diffStatisticProvider.GetStatistic(key, out string errMsg);
         return diffStatistic?.ToString() ?? errMsg;
      }

      string getTotalTimeText(MergeRequestKey key, User author)
      {
         ITotalTimeCache totalTimeCache = _dataCache?.TotalTimeCache;
         if (totalTimeCache == null)
         {
            return String.Empty;
         }

         User currentUser = _getCurrentUser(key.ProjectKey.HostName);
         bool isTimeTrackingAllowed = TimeTrackingHelpers.IsTimeTrackingAllowed(
            author, key.ProjectKey.HostName, currentUser);
         return TimeTrackingHelpers.ConvertTotalTimeToText(totalTimeCache.GetTotalTime(key), isTimeTrackingAllowed);
      }

      DateTime? getRefreshedTime(MergeRequestKey key)
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return null;
         }

         return mergeRequestCache.GetMergeRequestRefreshTime(key);
      }

      string getRefreshed(MergeRequestKey key, bool tooltipText)
      {
         DateTime? refreshedTime = getRefreshedTime(key);
         return tooltipText ? TimeUtils.DateTimeOptToString(refreshedTime)
                            : TimeUtils.DateTimeOptToStringAgo(refreshedTime);
      }

      DateTime? getLatestCommitTime(MergeRequestKey key)
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

      string getActivities(DateTime createdAt, MergeRequestKey key, bool tooltipText)
      {
         DateTime? latestCommitTime = getLatestCommitTime(key);
         return String.Format("Created: {0}\r\nLatest commit: {1}",
            tooltipText ? TimeUtils.DateTimeToString(createdAt)
                        : TimeUtils.DateTimeToStringAgo(createdAt),
            tooltipText ? TimeUtils.DateTimeOptToString(latestCommitTime)
                        : TimeUtils.DateTimeOptToStringAgo(latestCommitTime));
      }

      string getJiraTask(MergeRequest mergeRequest) => GitLabClient.Helpers.GetJiraTask(mergeRequest);

      string getJiraTaskUrl(MergeRequest mergeRequest) => GitLabClient.Helpers.GetJiraTaskUrl(
         mergeRequest, Program.ServiceManager.GetJiraServiceUrl());

      public event Action<ListView> CollapsingToggled;

      protected override void OnMouseLeave(EventArgs e)
      {
         // this callback is called not only when mouse leaves the list view so let's check if we need to cancel tooltip
         ListViewHitTestInfo hit = HitTest(this.PointToClient(Cursor.Position));
         _toolTip.CancelIfNeeded(hit);

         base.OnMouseLeave(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         ListViewHitTestInfo hit = HitTest(e.Location);
         _toolTip.UpdateOnMouseMove(e.Location);
         changeCursor(hit);

         base.OnMouseMove(e);
      }

      static readonly int GroupHeaderHeight = 20; // found experimentally

      protected override void OnMouseDown(MouseEventArgs e)
      {
         int headerHeight = LogicalToDeviceUnits(GroupHeaderHeight);
         ListViewHitTestInfo testAtCursor = HitTest(e.Location);
         ListViewHitTestInfo testBelowCursor = HitTest(e.Location.X, e.Location.Y + headerHeight);
         if (testAtCursor.Item == null && testBelowCursor.Item != null)
         {
            ProjectKey projectKey = getGroupProjectKey(testBelowCursor.Item.Group);
            setGroupCollapsing(projectKey, !isGroupCollapsed(projectKey));
            return;
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
      }

      protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
      {
         base.OnColumnWidthChanged(e);
         saveColumnWidths();
      }

      protected override void OnColumnReordered(ColumnReorderedEventArgs e)
      {
         base.OnColumnReordered(e);
         saveColumIndices(e.OldDisplayIndex, e.NewDisplayIndex);
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
            columnWidths[(string)column.Tag] = column.Width;
         }
         ConfigurationHelper.SetColumnWidths(Program.Settings, columnWidths, getIdentity());
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

         FullMergeRequestKey fmk = (FullMergeRequestKey)(e.Item.Tag);

         bool isSelected = e.Item.Selected;
         WinFormsHelpers.FillRectangle(e, bounds, getMergeRequestColor(fmk, Color.Transparent), isSelected);

         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

         string text = ((ListViewSubItemInfo)(e.SubItem.Tag)).Text;
         bool isClickable = ((ListViewSubItemInfo)(e.SubItem.Tag)).Clickable;

         int labelsColumnIndex = getColumnByTag("Labels").Index;
         int? resolvedCountColumnIndex = getColumnByTag("Resolved")?.Index;
         int? totalTimeColumnIndex = getColumnByTag("TotalTime")?.Index;
         int? titleColumnIndex = getColumnByTag("Title")?.Index;
         int? sourceBranchColumnIndex = getColumnByTag("SourceBranch")?.Index;
         int? targetBranchColumnIndex = getColumnByTag("TargetBranch")?.Index;
         int? jiraColumnIndex = getColumnByTag("Jira")?.Index;
         int? authorColumnIndex = getColumnByTag("Author")?.Index;

         bool isLabelsColumnItem = e.ColumnIndex == labelsColumnIndex;
         bool isResolvedColumnItem = resolvedCountColumnIndex.HasValue && e.ColumnIndex == resolvedCountColumnIndex.Value;
         bool isTotalTimeColumnItem = totalTimeColumnIndex.HasValue && e.ColumnIndex == totalTimeColumnIndex.Value;
         bool isTitleColumnItem = titleColumnIndex.HasValue && e.ColumnIndex == titleColumnIndex.Value;
         bool isSourceBranchColumnItem = sourceBranchColumnIndex.HasValue && e.ColumnIndex == sourceBranchColumnIndex.Value;
         bool isTargetBranchColumnItem = targetBranchColumnIndex.HasValue && e.ColumnIndex == targetBranchColumnIndex.Value;
         bool isJiraColumnItem = jiraColumnIndex.HasValue && e.ColumnIndex == jiraColumnIndex.Value;
         bool isAuthorColumnItem = authorColumnIndex.HasValue && e.ColumnIndex == authorColumnIndex.Value;

         bool isWrappableColumnItem =
               isTitleColumnItem
            || isSourceBranchColumnItem
            || isTargetBranchColumnItem
            || isJiraColumnItem
            || isAuthorColumnItem;
         bool needWordWrap = isWrappableColumnItem && Program.Settings.WordWrapLongRows;
         StringFormatFlags formatFlags = needWordWrap ? StringFormatFlags.LineLimit : StringFormatFlags.NoWrap;
         StringFormat format = new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = formatFlags
            };

         if (isClickable)
         {
            using (Font font = new Font(e.Item.ListView.Font, FontStyle.Underline))
            {
               Brush brush = Brushes.Blue;
               e.Graphics.DrawString(text, font, brush, bounds, format);
            }
         }
         else if (isSelected && isLabelsColumnItem)
         {
            using (Brush brush = new SolidBrush(getMergeRequestColor(fmk, SystemColors.Window)))
            {
               e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
            }
         }
         else if (isResolvedColumnItem)
         {
            using (Brush brush = new SolidBrush(getDiscussionCountColor(fmk, isSelected)))
            {
               e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
            }
         }
         else if (isTotalTimeColumnItem)
         {
            Brush brush = text == Constants.NotAllowedTimeTrackingText ? Brushes.Gray : Brushes.Black;
            e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
         }
         else
         {
            e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, bounds, format);
         }
      }

      protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
      {
         base.OnDrawColumnHeader(e);
         e.DrawDefault = true;
      }

      private static void onUrlClick(ListViewHitTestInfo hit)
      {
         bool clickable = hit.SubItem != null && ((ListViewSubItemInfo)(hit.SubItem.Tag)).Clickable;
         if (clickable)
         {
            UrlHelper.OpenBrowser(((ListViewSubItemInfo)(hit.SubItem.Tag)).Url);
         }
      }

      private void changeCursor(ListViewHitTestInfo hit)
      {
         bool clickable = hit.SubItem != null && ((ListViewSubItemInfo)(hit.SubItem.Tag)).Clickable;
         Cursor = clickable ? Cursors.Hand : Cursors.Default;
      }

      private System.Drawing.Color getMergeRequestColor(FullMergeRequestKey fmk, Color defaultColor)
      {
         IEnumerable<MergeRequest> mergeRequests = fmk.MergeRequest == null
            ? getMatchingFilterProjectItems(fmk.ProjectKey).Select(key => key.MergeRequest)
            : new List<MergeRequest>{ fmk.MergeRequest };

         return _colorScheme?.GetColors("MergeRequests")
            .FirstOrDefault(colorSchemeItem =>
               GitLabClient.Helpers.CheckConditions(colorSchemeItem.Conditions, mergeRequests))?.Color
            ?? defaultColor;
      }

      private System.Drawing.Color getDiscussionCountColor(FullMergeRequestKey fmk, bool isSelected)
      {
         if (fmk.MergeRequest == null)
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

      private static void setListViewRowHeight(ListView listView, int maxLineCount)
      {
         // It is expected to use font size in pixels here
         int height = listView.Font.Height * maxLineCount + 2;

         ImageList imgList = new ImageList
         {
            ImageSize = new Size(1, height)
         };
         listView.SmallImageList = imgList;
      }

      private bool isGroupCollapsed(ProjectKey projectKey)
      {
         return _collapsedProjects.Contains(projectKey);
      }

      private bool isGroupCollapsed(ListViewGroup group)
      {
         return isGroupCollapsed(getGroupProjectKey(group));
      }

      private void setGroupCollapsing(ProjectKey projectKey, bool collapse)
      {
         bool isCollapsed = _collapsedProjects.Contains(projectKey);
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
         CollapsingToggled?.Invoke(this);
         Win32Tools.SetVerticalScrollPosition(Handle, vScrollPosition);
         NativeMethods.LockWindowUpdate(IntPtr.Zero);
      }
 
      internal void setCollapsedProjects(IEnumerable<ProjectKey> projectKeys)
      {
         _collapsedProjects.Clear();
         foreach (ProjectKey projectKey in projectKeys)
         {
            _collapsedProjects.Add(projectKey);
         }
         UpdateItems();
      }

      private void updateGroupCaption(ListViewGroup group)
      {
         if (group == null)
         {
            return;
         }

         string action = isGroupCollapsed(group) ? "expand" : "collapse";
         group.Header = String.Format(
            "{0} -- click to {1} {2} item(s)",
            group.Name, action, getMatchingFilterProjectItems(getGroupProjectKey(group)).Count());
      }

      private static bool isSummaryItem(ListViewItem item)
      {
         if (item == null)
         {
            return false;
         }
         return ((FullMergeRequestKey)(item.Tag)).MergeRequest == null;
      }

      private ProjectKey getGroupProjectKey(ListViewGroup group)
      {
         return ((ProjectKey)group.Tag);
      }

      private ListViewGroup getGroupByProjectKey(ProjectKey key)
      {
         return Groups.Cast<ListViewGroup>().SingleOrDefault(group => getGroupProjectKey(group).Equals(key));
      }

      private IEnumerable<FullMergeRequestKey> getAllProjectItems(ProjectKey projectKey)
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return Array.Empty<FullMergeRequestKey>();
         }

         return mergeRequestCache
            .GetMergeRequests(projectKey)
            .Select(mergeRequest => new FullMergeRequestKey(projectKey, mergeRequest));
      }

      private IEnumerable<FullMergeRequestKey> getMatchingFilterProjectItems(ProjectKey projectKey)
      {
         return getAllProjectItems(projectKey).Where(fmk => doesMatchFilter(fmk.MergeRequest));
      }

      bool doesMatchFilter(MergeRequest mergeRequest)
      {
         return _mergeRequestFilter?.DoesMatchFilter(mergeRequest) ?? true;
      }

      private void onSerialize(IPersistentStateSetter writer)
      {
         string recordName = String.Format("CollapsedProjects_{0}", getIdentity());
         new PersistentStateSaveHelper(recordName, writer).Save(_collapsedProjects);
      }

      private void onDeserialize(IPersistentStateGetter reader)
      {
         string recordName = String.Format("CollapsedProjects_{0}", getIdentity());
         new PersistentStateLoadHelper(recordName, reader).Load(out HashSet<ProjectKey> collapsedProjectsHashSet);
         if (collapsedProjectsHashSet != null)
         {
            setCollapsedProjects(collapsedProjectsHashSet);
         }
      }

      private void setColumnWidths(Dictionary<string, int> widths)
      {
         foreach (ColumnHeader column in Columns)
         {
            string columnName = (string)column.Tag;
            if (widths.ContainsKey(columnName))
            {
               column.Width = widths[columnName];
            }
         }
      }

      private void setColumnIndices(Dictionary<string, int> indices)
      {
         try
         {
            WinFormsHelpers.ReorderListViewColumns(this, indices);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("[MainForm] Cannot restore list view column display indices", ex);
            ConfigurationHelper.SetColumnIndices(Program.Settings,
               WinFormsHelpers.GetListViewDisplayIndices(this), getIdentity());
         }
      }

      private string getIdentity()
      {
         return Tag.ToString();
      }

      private readonly MergeRequestListViewToolTip _toolTip;
      private IDiffStatisticProvider _diffStatisticProvider;
      private Func<string, User> _getCurrentUser;
      private DataCache _dataCache;
      private MergeRequestFilter _mergeRequestFilter;
      private ColorScheme _colorScheme;
      private PersistentStorage _persistentStorage;
      private readonly HashSet<ProjectKey> _collapsedProjects = new HashSet<ProjectKey>();
   }
}

