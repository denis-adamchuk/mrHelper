using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;

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
      private static readonly int MaxLabelRows = 3;
      private static readonly string MoreLabelsHint = "See more labels in tooltip";

      public MergeRequestListView()
      {
         InitializeComponent();
         ListViewItemSorter = new ListViewItemComparer();

         _toolTipTimer = new System.Timers.Timer
         {
            Interval = 500,
            AutoReset = false,
            SynchronizingObject = this
         };

         _toolTipTimer.Elapsed +=
            (s, et) =>
         {
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.SubItem.Tag == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.Item.ListView == null)
            {
               return;
            }

            ListViewSubItemInfo info = (ListViewSubItemInfo)_lastHistTestInfo.SubItem.Tag;

            // shift tooltip position to the right of the cursor 16 pixels
            Point location = new Point(_lastMouseLocation.X + 16, _lastMouseLocation.Y);
            _toolTip.Show(info.TooltipText, _lastHistTestInfo.Item.ListView, location);
         };

         // had to use this hack, because it is not possible to prevent deselecting a row
         // on a click on empty area in ListView
         _delayedDeselectionTimer = new System.Windows.Forms.Timer
         {
            // using a very short Interval to emulate a quick deselection on clicking an empty area
            Interval = 100,
         };
         _delayedDeselectionTimer.Tick +=
            (s, ee) =>
         {
            _delayedDeselectionTimer.Stop();
            Deselected?.Invoke(this);
         };
      }

      public class ListViewSubItemInfo
      {
         public ListViewSubItemInfo(Func<bool, string> getText, Func<string> getUrl)
         {
            _getText = getText;
            _getUrl = getUrl;
         }

         public bool Clickable => _getUrl() != String.Empty;
         public string Text => _getText(false);
         public string Url => _getUrl();
         public string TooltipText
         {
            get
            {
               return !String.IsNullOrWhiteSpace(Url) ? Url : _getText(true);
            }
         }

         private readonly Func<bool, string> _getText;
         private readonly Func<string> _getUrl;
      }

      internal void SetDiffStatisticProvider(IDiffStatisticProvider diffStatisticProvider)
      {
         _diffStatisticProvider = diffStatisticProvider;
      }

      internal void SetCurrentUserGetter(Func<string, User> funcGetter)
      {
         _getCurrentUser = funcGetter;
      }

      internal void AssignDataCache(DataCache dataCache)
      {
         _dataCache = dataCache;
      }

      internal void SetColorScheme(ColorScheme colorScheme)
      {
         _colorScheme = colorScheme;
      }

      internal void SetColumnWidthSaver(Action<Dictionary<string, int>> saver)
      {
         _columnWidthSaver = saver;
      }

      internal void SetColumnIndicesSaver(Action<Dictionary<string, int>> saver)
      {
         _columnIndicesSaver = saver;
      }

      internal void SetColumnWidths(Dictionary<string, int> storedWidths)
      {
         foreach (ColumnHeader column in Columns)
         {
            string columnName = (string)column.Tag;
            if (storedWidths.ContainsKey(columnName))
            {
               column.Width = storedWidths[columnName];
            }
         }
      }

      internal void SetColumnIndices(Dictionary<string, int> storedIndices, Action<Dictionary<string, int>> storeDefaults)
      {
         try
         {
            WinFormsHelpers.ReorderListViewColumns(this, storedIndices);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("[MainForm] Cannot restore list view column display indices", ex);
            storeDefaults(WinFormsHelpers.GetListViewDisplayIndices(this));
         }
      }

      internal void DisableListView(bool clear)
      {
         Enabled = false;
         DeselectAllListViewItems();

         if (clear)
         {
            Items.Clear();
         }
      }

      internal void AssignContextMenu(MergeRequestListViewContextMenu contextMenu)
      {
         ContextMenuStrip = contextMenu;
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

      internal FullMergeRequestKey? GetSelectedMergeRequest()
      {
         if (SelectedIndices.Count > 0)
         {
            return (FullMergeRequestKey)(SelectedItems[0].Tag);
         }
         return null;
      }

      internal IEnumerable<FullMergeRequestKey> GetAllMergeRequests()
      {
         return Items
            .Cast<ListViewItem>()
            .Select(x => x.Tag)
            .Cast<FullMergeRequestKey>();
      }

      internal void EnableListView()
      {
         Enabled = true;
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

      public void RecalcRowHeightForMergeRequestListView()
      {
         if (Items.Count == 0)
         {
            return;
         }

         int maxLineCountInLabels = Items.Cast<ListViewItem>()
            .Select(x => formatLabels((FullMergeRequestKey)(x.Tag), false)
               .Count(y => y == '\n'))
            .Max() + 1;
         int maxLineCountInAuthor = 2;
         int maxLineCount = Math.Max(maxLineCountInLabels, maxLineCountInAuthor);
         setListViewRowHeight(this, maxLineCount);
      }

      public void UpdateItems(bool updateGroups, MergeRequestFilter mergeRequestFilter)
      {
         IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         BeginUpdate();

         IEnumerable<ProjectKey> projectKeys;
         if (updateGroups)
         {
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
            projectKeys = Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);
            for (int index = Groups.Count - 1; index >= 0; --index)
            {
               ListViewGroup group = Groups[index];
               if (!allProjects.Any(x => x.Equals((ProjectKey)group.Tag)))
               {
                  Groups.Remove(group);
               }
            }
         }
         else
         {
            projectKeys = Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);
         }

         // Add missing merge requests and update existing ones
         foreach (ProjectKey projectKey in projectKeys)
         {
            foreach (MergeRequest mergeRequest in mergeRequestCache.GetMergeRequests(projectKey))
            {
               FullMergeRequestKey fmk = new FullMergeRequestKey(projectKey, mergeRequest);
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

         // Remove deleted merge requests
         for (int index = Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
            if (!mergeRequestCache.GetMergeRequests(fmk.ProjectKey).Any(x => x.IId == fmk.MergeRequest.IId))
            {
               Items.RemoveAt(index);
            }
         }

         if (mergeRequestFilter != null)
         {
            // Hide filtered ones
            for (int index = Items.Count - 1; index >= 0; --index)
            {
               FullMergeRequestKey fmk = (FullMergeRequestKey)Items[index].Tag;
               if (!mergeRequestFilter.DoesMatchFilter(fmk.MergeRequest))
               {
                  Items.RemoveAt(index);
               }
            }
         }

         RecalcRowHeightForMergeRequestListView();

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

      private void setListViewSubItemsTags(ListViewItem item, FullMergeRequestKey fmk)
      {
         Debug.Assert(item.ListView == this);

         ProjectKey projectKey = fmk.ProjectKey;
         MergeRequest mr = fmk.MergeRequest;
         MergeRequestKey mrk = new MergeRequestKey(projectKey, mr.IId);

         string author = String.Format("{0}\n({1}{2})", mr.Author.Name,
            Constants.AuthorLabelPrefix, mr.Author.Username);

         Dictionary<bool, string> labels = new Dictionary<bool, string>
         {
            [false] = formatLabels(fmk, false),
            [true] = formatLabels(fmk, true)
         };

         string jiraServiceUrl = Program.ServiceManager.GetJiraServiceUrl();
         string jiraTask = getJiraTask(mr);
         string jiraTaskUrl = jiraServiceUrl != String.Empty && jiraTask != String.Empty ?
            String.Format("{0}/browse/{1}", jiraServiceUrl, jiraTask) : String.Empty;

         string getTotalTimeText(MergeRequestKey key)
         {
            ITotalTimeCache totalTimeCache = _dataCache?.TotalTimeCache;
            if (totalTimeCache == null)
            {
               return String.Empty;
            }

            User currentUser = _getCurrentUser(projectKey.HostName);
            bool isTimeTrackingAllowed = TimeTrackingHelpers.IsTimeTrackingAllowed(
               mr.Author, projectKey.HostName, currentUser);
            return TimeTrackingHelpers.ConvertTotalTimeToText(totalTimeCache.GetTotalTime(key), isTimeTrackingAllowed);
         }

         string getRefreshed(MergeRequestKey key)
         {
            IMergeRequestCache mergeRequestCache = _dataCache?.MergeRequestCache;
            if (mergeRequestCache == null)
            {
               return String.Empty;
            }

            DateTime refreshed = mergeRequestCache.GetMergeRequestRefreshTime(key);
            TimeSpan span = DateTime.Now - refreshed;
            int minutesAgo = Convert.ToInt32(Math.Floor(span.TotalMinutes));
            // round 55+ seconds to a minute
            minutesAgo += span.Seconds >= 55 ? 1 : 0; //-V3118
            return String.Format("{0} minute{1} ago", minutesAgo, minutesAgo == 1 ? String.Empty : "s");
         }

         void setSubItemTag(string columnTag, ListViewSubItemInfo subItemInfo)
         {
            ColumnHeader columnHeader = getColumnByTag(columnTag);
            if (columnHeader == null)
            {
               return;
            }

            item.SubItems[columnHeader.Index].Tag = subItemInfo;
         }

         setSubItemTag("IId", new ListViewSubItemInfo(x => mr.IId.ToString(), () => mr.Web_Url));
         setSubItemTag("Author", new ListViewSubItemInfo(x => author, () => String.Empty));
         setSubItemTag("Title", new ListViewSubItemInfo(x => mr.Title, () => String.Empty));
         setSubItemTag("Labels", new ListViewSubItemInfo(x => labels[x], () => String.Empty));
         setSubItemTag("Size", new ListViewSubItemInfo(x => getSize(mrk), () => String.Empty));
         setSubItemTag("Jira", new ListViewSubItemInfo(x => jiraTask, () => jiraTaskUrl));
         setSubItemTag("TotalTime", new ListViewSubItemInfo(x => getTotalTimeText(mrk), () => String.Empty));
         setSubItemTag("SourceBranch", new ListViewSubItemInfo(x => mr.Source_Branch, () => String.Empty));
         setSubItemTag("TargetBranch", new ListViewSubItemInfo(x => mr.Target_Branch, () => String.Empty));
         setSubItemTag("State", new ListViewSubItemInfo(x => mr.State, () => String.Empty));
         setSubItemTag("Resolved", new ListViewSubItemInfo(x => getDiscussionCount(mrk), () => String.Empty));
         setSubItemTag("RefreshTime", new ListViewSubItemInfo(x => getRefreshed(mrk), () => String.Empty));
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

      private string formatLabels(FullMergeRequestKey fmk, bool tooltip)
      {
         User currentUser = _getCurrentUser?.Invoke(fmk.ProjectKey.HostName);

         IEnumerable<string> unimportantSuffices = Program.ServiceManager.GetUnimportantSuffices();

         int getPriority(IEnumerable<string> labels)
         {
            Debug.Assert(labels.Any());
            if (GitLabClient.Helpers.IsUserMentioned(labels.First(), currentUser))
            {
               return 0;
            }
            else if (labels.Any(x => unimportantSuffices.Any(y => x.EndsWith(y))))
            {
               return 2;
            }
            return 1;
         };

         var query = fmk.MergeRequest.Labels
            .GroupBy(
               label => label
                  .StartsWith(Constants.GitLabLabelPrefix) && label.IndexOf('-') != -1
                     ? label.Substring(0, label.IndexOf('-'))
                     : label,
               label => label,
               (baseLabel, labels) => new
               {
                  Labels = labels,
                  Priority = getPriority(labels)
               });

         string joinLabels(IEnumerable<string> labels) => String.Format("{0}\n", String.Join(",", labels));

         StringBuilder stringBuilder = new StringBuilder();
         int take = tooltip ? query.Count() : MaxLabelRows - 1;

         foreach (var x in
            query
            .OrderBy(x => x.Priority)
            .Take(take))
         {
            stringBuilder.Append(joinLabels(x.Labels));
         }

         if (!tooltip)
         {
            if (query.Count() > MaxLabelRows)
            {
               stringBuilder.Append(MoreLabelsHint);
            }
            else if (query.Count() == MaxLabelRows)
            {
               stringBuilder.Append(joinLabels(query.OrderBy(x => x.Priority).Last().Labels));
            }
         }

         return stringBuilder.ToString().TrimEnd('\n');
      }


      public event Action<ListView> Deselected;

      protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
      {
         if (SelectedItems.Count < 1)
         {
            _delayedDeselectionTimer.Start();
            return;
         }

         if (_delayedDeselectionTimer.Enabled)
         {
            _delayedDeselectionTimer.Stop();
         }

         base.OnItemSelectionChanged(e);
      }

      protected override void OnMouseLeave(EventArgs e)
      {
         onLeave();
         base.OnMouseLeave(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         ListViewHitTestInfo hit = HitTest(e.Location);

         if (hit.Item == null || hit.SubItem == null)
         {
            onLeave();
            return;
         }

         if (_lastMouseLocation == e.Location)
         {
            return;
         }
         _lastMouseLocation = e.Location;

         if (!String.IsNullOrEmpty(_toolTip.GetToolTip(this)))
         {
            Debug.Assert(!_toolTipTimer.Enabled);
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.Item.Index != hit.Item.Index
             || _lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               _toolTip.Hide(this);
               _lastHistTestInfo = hit;
               _toolTipTimer.Start();
            }
         }
         else
         {
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.Item.Index != hit.Item.Index
             || _lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               if (_toolTipTimer.Enabled)
               {
                  _toolTipTimer.Stop();
               }
               _lastHistTestInfo = hit;
               _toolTipTimer.Start();
            }
         }

         changeCursor(HitTest(e.Location));

         base.OnMouseMove(e);
      }

      protected override void OnMouseDown(MouseEventArgs e)
      {
         onUrlClick(HitTest(e.Location));
         base.OnMouseDown(e);
      }

      protected override void OnMouseDoubleClick(MouseEventArgs e)
      {
         base.OnMouseDoubleClick(e);
         GetContextMenu().LaunchDefaultAction();
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
         _columnIndicesSaver?.Invoke(indices);
      }

      private void saveColumnWidths()
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (ColumnHeader column in Columns)
         {
            columnWidths[(string)column.Tag] = column.Width;
         }
         _columnWidthSaver?.Invoke(columnWidths);
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
         WinFormsHelpers.FillRectangle(e, bounds, getMergeRequestColor(fmk.MergeRequest, Color.Transparent), isSelected);

         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

         string text = ((ListViewSubItemInfo)(e.SubItem.Tag)).Text;
         bool isClickable = ((ListViewSubItemInfo)(e.SubItem.Tag)).Clickable;

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         int labelsColumnIndex = getColumnByTag("Labels").Index;
         int? resolvedCountColumnIndex = getColumnByTag("Resolved")?.Index;
         int? totalTimeColumnIndex = getColumnByTag("TotalTime")?.Index;

         bool isLabelsColumnItem = e.ColumnIndex == labelsColumnIndex;
         bool isResolvedColumnItem = resolvedCountColumnIndex.HasValue && e.ColumnIndex == resolvedCountColumnIndex.Value;
         bool isTotalTimeColumnItem = totalTimeColumnIndex.HasValue && e.ColumnIndex == totalTimeColumnIndex.Value;

         if (isClickable)
         {
            using (Font font = new Font(e.Item.ListView.Font, FontStyle.Underline))
            {
               Brush brush = Brushes.Blue;
               e.Graphics.DrawString(text, font, brush, bounds, format);
            }
         }
         else
         {
            if (isSelected && isLabelsColumnItem)
            {
               using (Brush brush = new SolidBrush(getMergeRequestColor(fmk.MergeRequest, SystemColors.Window)))
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

      private System.Drawing.Color getMergeRequestColor(MergeRequest mergeRequest, Color defaultColor)
      {
         foreach (KeyValuePair<string, Color> color in _colorScheme)
         {
            // by author
            {
               if (StringUtils.DoesMatchPattern(color.Key, "MergeRequests_{{Author:{0}}}", mergeRequest.Author.Username))
               {
                  return color.Value;
               }
            }

            // by labels
            foreach (string label in mergeRequest.Labels)
            {
               if (StringUtils.DoesMatchPattern(color.Key, "MergeRequests_{{Label:{0}}}", label))
               {
                  return color.Value;
               }
            }
         }
         return defaultColor;
      }

      private System.Drawing.Color getDiscussionCountColor(FullMergeRequestKey fmk, bool isSelected)
      {
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

      private void onLeave()
      {
         if (_toolTipTimer.Enabled)
         {
            _toolTipTimer.Stop();
         }

         if (!String.IsNullOrEmpty(_toolTip.GetToolTip(this)))
         {
            _toolTip.Hide(this);
         }

         _lastHistTestInfo = null;
      }

      private string getSize(MergeRequestKey mrk)
      {
         if (_diffStatisticProvider == null)
         {
            return String.Empty;
         }

         DiffStatistic? diffStatistic = _diffStatisticProvider.GetStatistic(mrk, out string errMsg);
         return diffStatistic?.ToString() ?? errMsg;
      }

      private static readonly Regex jira_re = new Regex(@"(?'name'(?!([A-Z0-9a-z]{1,10})-?$)[A-Z]{1}[A-Z0-9]+-\d+)");
      private static string getJiraTask(MergeRequest mergeRequest)
      {
         Match m = jira_re.Match(mergeRequest.Title);
         return !m.Success || m.Groups.Count < 1 || !m.Groups["name"].Success ? String.Empty : m.Groups["name"].Value;
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

      private readonly ToolTip _toolTip = new ToolTip();
      private readonly System.Timers.Timer _toolTipTimer;
      private Point _lastMouseLocation = new Point(-1, -1);
      private ListViewHitTestInfo _lastHistTestInfo;
      private IDiffStatisticProvider _diffStatisticProvider;
      private Func<string, User> _getCurrentUser;
      private DataCache _dataCache;
      private ColorScheme _colorScheme;
      private Action<Dictionary<string, int>> _columnWidthSaver;
      private Action<Dictionary<string, int>> _columnIndicesSaver;

      // Using System.Windows.Forms.Timer here because it remains Enabled
      // if even Interval exceeded between Start() and Stop() calls occurred
      // within a single execution thread without async processing.
      // System.Timers.Timer behaves differently. If Interval exceeds
      // between Start() and Stop() (see OnItemSelectionChanged),
      // Enabled property is reset and a timer event is already queued so
      // it will trigger when no longer needed.
      private readonly System.Windows.Forms.Timer _delayedDeselectionTimer;
   }
}

