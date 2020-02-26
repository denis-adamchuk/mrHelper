using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using static mrHelper.App.Helpers.ServiceManager;
using static mrHelper.Client.Common.UserEvents;
using mrHelper.Client.Types;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Controls;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      User? getCurrentUser()
      {
         Debug.Assert(_currentUser != null);
         return _currentUser.Value;
      }

      string getHostName()
      {
         return comboBoxHost.SelectedItem != null ? ((HostComboBoxItem)comboBoxHost.SelectedItem).Host : String.Empty;
      }

      MergeRequest? getMergeRequest()
      {
         if (listViewMergeRequests.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listViewMergeRequests.SelectedItems[0].Tag;
            return fmk.MergeRequest;
         }
         Debug.Assert(false);
         return null;
      }

      MergeRequestKey? getMergeRequestKey()
      {
         if (listViewMergeRequests.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listViewMergeRequests.SelectedItems[0].Tag;
            return new MergeRequestKey
            {
               ProjectKey = fmk.ProjectKey,
               IId = fmk.MergeRequest.IId
            };
         }
         return null;
      }

      /// <summary>
      /// Populates host list with list of known hosts from Settings
      /// </summary>
      private void updateHostsDropdownList()
      {
         if (listViewKnownHosts.Items.Count == 0)
         {
            disableComboBox(comboBoxHost, String.Empty);
            return;
         }

         enableComboBox(comboBoxHost);

         for (int idx = comboBoxHost.Items.Count - 1; idx >= 0; --idx)
         {
            HostComboBoxItem item = (HostComboBoxItem)comboBoxHost.Items[idx];
            if (!listViewKnownHosts.Items.Cast<ListViewItem>().Any(x => x.Text == item.Host))
            {
               comboBoxHost.Items.RemoveAt(idx);
            }
         }

         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            if (!comboBoxHost.Items.Cast<HostComboBoxItem>().Any(x => x.Host == item.Text))
            {
               HostComboBoxItem hostItem = new HostComboBoxItem
               {
                  Host = item.Text,
                  AccessToken = item.SubItems[1].Text
               };
               comboBoxHost.Items.Add(hostItem);
            }
         }
      }

      private enum PreferredSelection
      {
         Initial,
         Latest
      }

      private void selectHost(PreferredSelection preferred)
      {
         if (comboBoxHost.Items.Count == 0)
         {
            return;
         }

         comboBoxHost.SelectedIndex = -1;

         HostComboBoxItem initialSelectedItem = comboBoxHost.Items.Cast<HostComboBoxItem>().ToList().SingleOrDefault(
            x => x.Host == getInitialHostName());
         HostComboBoxItem defaultSelectedItem = (HostComboBoxItem)comboBoxHost.Items[comboBoxHost.Items.Count - 1];
         switch (preferred)
         {
            case PreferredSelection.Initial:
               if (!String.IsNullOrEmpty(initialSelectedItem.Host))
               {
                  comboBoxHost.SelectedItem = initialSelectedItem;
               }
               else
               {
                  comboBoxHost.SelectedItem = defaultSelectedItem;
               }
               break;

            case PreferredSelection.Latest:
               comboBoxHost.SelectedItem = defaultSelectedItem;
               break;
         }

         onHostSelected();
      }

      private bool selectMergeRequest(string projectname, int iid, bool exact)
      {
         foreach (ListViewItem item in listViewMergeRequests.Items)
         {
            FullMergeRequestKey key = (FullMergeRequestKey)(item.Tag);
            if (projectname == String.Empty ||
                (iid == key.MergeRequest.IId && projectname == key.ProjectKey.ProjectName))
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
         foreach (ListViewGroup group in listViewMergeRequests.Groups)
         {
            if (projectname == group.Name && group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         // select whatever
         foreach (ListViewGroup group in listViewMergeRequests.Groups)
         {
            if (group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         return false;
      }

      private void selectNotReviewedCommits(out int left, out int right)
      {
         Debug.Assert(comboBoxLeftCommit.Items.Count == comboBoxRightCommit.Items.Count);

         left = 0;
         right = 0;

         Debug.Assert(getMergeRequestKey().HasValue);
         MergeRequestKey mrk = getMergeRequestKey().Value;
         if (!_reviewedCommits.ContainsKey(mrk))
         {
            left = 0;
            right = comboBoxRightCommit.Items.Count - 1;
            return;
         }

         int? iNewestOfReviewedCommits = new Nullable<int>();
         HashSet<string> reviewedCommits = _reviewedCommits[mrk];
         for (int iItem = 0; iItem < comboBoxLeftCommit.Items.Count; ++iItem)
         {
            string sha = ((CommitComboBoxItem)(comboBoxLeftCommit.Items[iItem])).SHA;
            if (reviewedCommits.Contains(sha))
            {
               iNewestOfReviewedCommits = iItem;
               break;
            }
         }

         if (!iNewestOfReviewedCommits.HasValue)
         {
            return;
         }

         left = Math.Max(0, iNewestOfReviewedCommits.Value - 1);

         // note that it should not be left + 1 because Left CB is shifted comparing to Right CB
         right = Math.Min(left, comboBoxRightCommit.Items.Count - 1);
      }

      private void checkComboboxCommitsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftCommit.SelectedItem == null || comboBoxRightCommit.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a commit older than in right combobox (replicating gitlab web ui behavior)
         CommitComboBoxItem leftItem = (CommitComboBoxItem)(comboBoxLeftCommit.SelectedItem);
         CommitComboBoxItem rightItem = (CommitComboBoxItem)(comboBoxRightCommit.SelectedItem);
         Debug.Assert(leftItem.TimeStamp.HasValue);

         if (rightItem.TimeStamp.HasValue)
         {
            // Check if order is broken
            if (leftItem.TimeStamp.Value <= rightItem.TimeStamp.Value)
            {
               if (shouldReorderRightCombobox)
               {
                  comboBoxRightCommit.SelectedIndex = comboBoxLeftCommit.SelectedIndex;
               }
               else
               {
                  comboBoxLeftCommit.SelectedIndex = comboBoxRightCommit.SelectedIndex;
               }
            }
         }
         else
         {
            // It is ok because a commit w/o timestamp is the oldest one
         }
      }

      private string getGitTag(bool left)
      {
         // swap sides to be consistent with gitlab web ui
         if (!left)
         {
            Debug.Assert(comboBoxLeftCommit.SelectedItem != null);
            return ((CommitComboBoxItem)comboBoxLeftCommit.SelectedItem).SHA;
         }
         else
         {
            Debug.Assert(comboBoxRightCommit.SelectedItem != null);
            return ((CommitComboBoxItem)comboBoxRightCommit.SelectedItem).SHA;
         }
      }

      private bool addKnownHost(string host, string accessToken)
      {
         Trace.TraceInformation(String.Format("[MainForm] Adding host {0} with token {1}",
            host, accessToken));

         foreach (ListViewItem listItem in listViewKnownHosts.Items)
         {
            if (listItem.Text == host)
            {
               Trace.TraceInformation(String.Format("[MainForm] Host name already exists"));
               return false;
            }
         }

         ListViewItem item = new ListViewItem(host);
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private bool removeKnownHost()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            Trace.TraceInformation(String.Format("[MainForm] Removing host name {0}",
               listViewKnownHosts.SelectedItems[0].ToString()));

            listViewKnownHosts.Items.Remove(listViewKnownHosts.SelectedItems[0]);
            return true;
         }
         return false;
      }

      private string getHostWithPrefix(string host)
      {
         string supportedProtocolPrefix = "https://";
         string unsupportedProtocolPrefix = "http://";

         if (host.StartsWith(supportedProtocolPrefix))
         {
            return host;
         }
         else if (host.StartsWith(unsupportedProtocolPrefix))
         {
           return host.Replace(unsupportedProtocolPrefix, supportedProtocolPrefix);
         }

         return supportedProtocolPrefix + host;
      }

      private string getDefaultColorSchemeFileName()
      {
         return String.Format("{0}.{1}", DefaultColorSchemeName, ColorSchemeFileNamePrefix);
      }

      private void fillColorSchemesList()
      {
         string defaultFileName = getDefaultColorSchemeFileName();
         string defaultFilePath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

         comboBoxColorSchemes.Items.Clear();

         string selectedScheme = null;
         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         if (files.Contains(defaultFilePath))
         {
            // put Default scheme first in the list
            comboBoxColorSchemes.Items.Add(defaultFileName);
         }

         foreach (string file in files)
         {
            if (file.EndsWith(ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               if (file != defaultFilePath)
               {
                  comboBoxColorSchemes.Items.Add(scheme);
               }
               if (scheme == Program.Settings.ColorSchemeFileName)
               {
                  selectedScheme = scheme;
               }
            }
         }

         if (selectedScheme != null)
         {
            comboBoxColorSchemes.SelectedItem = selectedScheme;
         }
         else if (comboBoxColorSchemes.Items.Count > 0)
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      private void initializeColorScheme()
      {
         Func<string, bool> createColorScheme =
            (filename) =>
         {
            try
            {
               _colorScheme = new ColorScheme(filename, _expressionResolver);
               return true;
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle("Cannot create a color scheme", ex);
            }
            return false;
         };

         if (comboBoxColorSchemes.SelectedIndex < 0 || comboBoxColorSchemes.Items.Count < 1)
         {
            // nothing is selected or list is empty, create an empty scheme
            _colorScheme = new ColorScheme();
         }

         // try to create a scheme for the selected item
         else if (!createColorScheme(comboBoxColorSchemes.Text))
         {
            _colorScheme = new ColorScheme();
            MessageBox.Show("Cannot initialize color scheme", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void initializeIconScheme()
      {
         if (!System.IO.File.Exists(Constants.IconSchemeFileName))
         {
            return;
         }

         try
         {
            _iconScheme = JsonFileReader.LoadFromFile<Dictionary<string, object>>(
               Constants.IconSchemeFileName).ToDictionary(
                  item => item.Key,
                  item => item.Value.ToString());
         }
         catch (Exception ex) // whatever de-deserialization exception
         {
            ExceptionHandlers.Handle("Cannot load icon scheme", ex);
         }
      }

      private void disableListView(ListView listView, bool clear)
      {
         listView.Enabled = false;
         foreach (ListViewItem item in listView.Items)
         {
            item.Selected = false;
         }

         if (clear)
         {
            listView.Items.Clear();
         }
      }

      private void enableListView(ListView listView)
      {
         listView.Enabled = true;
      }

      private void disableComboBox(ComboBox comboBox, string text)
      {
         comboBox.DroppedDown = false;
         comboBox.SelectedIndex = -1;
         comboBox.Items.Clear();
         comboBox.Enabled = false;

         comboBox.DropDownStyle = ComboBoxStyle.DropDown;
         comboBox.Text = text;
      }

      private void enableComboBox(ComboBox comboBox)
      {
         comboBox.Enabled = true;
         comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      }

      private void enableControlsOnGitAsyncOperation(bool enabled)
      {
         linkLabelAbortGit.Visible = !enabled;
         buttonDiffTool.Enabled = enabled;
         buttonDiscussions.Enabled = enabled;
         comboBoxHost.Enabled = enabled;
         listViewMergeRequests.Enabled = enabled;
         enableMergeRequestFilterControls(enabled);
         tabPageSettings.Controls.Cast<Control>().ToList().ForEach((x) => x.Enabled = enabled);

         if (enabled)
         {
            updateGitStatusText(String.Empty);
         }
      }

      private void enableMergeRequestFilterControls(bool enabled)
      {
         checkBoxLabels.Enabled = enabled;
         textBoxLabels.Enabled = enabled;
      }

      private void updateMergeRequestDetails(FullMergeRequestKey? fmk)
      {
         richTextBoxMergeRequestDescription.Text = getMergeRequestDescriptionHtmlText(fmk);
         richTextBoxMergeRequestDescription.Update();
         linkLabelConnectedTo.Text = fmk.HasValue ? fmk.Value.MergeRequest.Web_Url : String.Empty;
      }

      private string getMergeRequestDescriptionHtmlText(FullMergeRequestKey? fmk)
      {
         string commonBegin = string.Format(@"
            <html>
               <head>
               </head>
               <body>
                  <div>");

         string commonEnd = @"
                  </div>
               </body>
            </html>";

         string htmlbody = String.Empty;
         if (fmk.HasValue)
         {
            htmlbody =
               System.Net.WebUtility.HtmlDecode(
                  Markdig.Markdown.ToHtml(
                     System.Net.WebUtility.HtmlEncode(fmk.Value.MergeRequest.Description),
                        _mergeRequestDescriptionMarkdownPipeline));

            htmlbody = htmlbody.Replace("<img src=\"/uploads/", String.Format("<img src=\"{0}/{1}/uploads/",
               getHostWithPrefix(fmk.Value.ProjectKey.HostName), fmk.Value.ProjectKey.ProjectName));
         }

         return commonBegin + htmlbody + commonEnd;
      }

      private void updateTimeTrackingMergeRequestDetails(MergeRequest? mergeRequest)
      {
         if (isTrackingTime())
         {
            return;
         }

         labelTimeTrackingMergeRequestName.Visible = mergeRequest.HasValue;
         buttonTimeTrackingStart.Enabled = mergeRequest.HasValue;

         if (mergeRequest.HasValue)
         {
            Debug.Assert(getMergeRequestKey().HasValue);

            labelTimeTrackingMergeRequestName.Text =
               mergeRequest.Value.Title + "   " + "[" + getMergeRequestKey()?.ProjectKey.ProjectName + "]";
         }
      }

      private void updateTotalTime(MergeRequestKey? mrk)
      {
         if (isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text =
               String.Format("Tracked Time: {0}", _timeTracker.Elapsed.ToString(@"hh\:mm\:ss"));
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrk.HasValue)
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            buttonEditTime.Enabled = false;
         }
         else
         {
            TimeSpan? span = getTotalTime(mrk.Value);
            labelTimeTrackingTrackedLabel.Text = String.Format("Total Time: {0}", convertTotalTimeToText(span));
            buttonEditTime.Enabled = span.HasValue;
         }

         // Update total time column in the table
         listViewMergeRequests.Invalidate();
      }

      private TimeSpan? getTotalTime(MergeRequestKey? mrk)
      {
         if (!mrk.HasValue)
         {
            return null;
         }

         return _timeTrackingManager.GetTotalTime(mrk.Value);
      }

      private string convertTotalTimeToText(TimeSpan? span)
      {
         return !span.HasValue ? "Loading..." :
            (span.Value == TimeSpan.Zero ? "Not Started" : span.Value.ToString(@"hh\:mm\:ss"));
      }

      private string getSize(FullMergeRequestKey? fmk)
      {
         if (!fmk.HasValue)
         {
            return String.Empty;
         }

         GitStatisticManager.DiffStatistic? diffStatistic =
            _gitStatManager.GetStatistic(fmk.Value, out string errMsg);
         return diffStatistic?.ToString() ?? errMsg;
      }

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Enabled = enabled;
         buttonAddComment.Enabled = enabled;
         buttonNewDiscussion.Enabled = enabled;
      }

      private void enableCommitActions(bool enabled)
      {
         buttonDiscussions.Enabled = enabled; // not a commit action but depends on git
         buttonDiffTool.Enabled = enabled;
         enableCustomActions(enabled);
      }

      private bool isCustomActionEnabled(MergeRequest mergeRequest, string dependency)
      {
         if (String.IsNullOrEmpty(dependency))
         {
            return true;
         }

         if (mergeRequest.Labels.Any(x => StringUtils.DoesMatchPattern(dependency, "{{Label:{0}}}", x)))
         {
            return true;
         }

         if (StringUtils.DoesMatchPattern(dependency, "{{Author:{0}}}", mergeRequest.Author.Username))
         {
            return true;
         }

         return false;
      }

      private void enableCustomActions(bool enabled)
      {
         if (!enabled || !getMergeRequest().HasValue)
         {
            groupBoxActions.Controls.Cast<Control>().ToList().ForEach(x => x.Enabled = false);
            return;
         }

         MergeRequest mergeRequest = getMergeRequest().Value;
         foreach (Control control in groupBoxActions.Controls)
         {
            string dependency = (string)control.Tag;
            string resolvedDependency =
               String.IsNullOrEmpty(dependency) ? String.Empty : _expressionResolver.Resolve(dependency);
            control.Enabled = isCustomActionEnabled(mergeRequest, resolvedDependency);
         }
      }

      private void addCommitsToComboBoxes(IEnumerable<Commit> commits, string baseSha, string targetBranch)
      {
         CommitComboBoxItem latestCommitItem = new CommitComboBoxItem(commits.First())
         {
            IsLatest = true
         };
         comboBoxLeftCommit.Items.Add(latestCommitItem);
         foreach (Commit commit in commits.Skip(1))
         {
            CommitComboBoxItem item = new CommitComboBoxItem(commit);
            if (comboBoxLeftCommit.Items.Cast<CommitComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLeftCommit.Items.Add(item);
            comboBoxRightCommit.Items.Add(item);
         }

         // Add target branch to the right combo-box
         CommitComboBoxItem baseCommitItem = new CommitComboBoxItem(
            baseSha, targetBranch + " [Base]", null, String.Empty)
         {
            IsBase = true
         };
         comboBoxRightCommit.Items.Add(baseCommitItem);
      }

      /// <summary>
      /// Typically called from another thread
      /// </summary>
      private void updateGitStatusText(string text)
      {
         if (labelGitStatus.InvokeRequired)
         {
            Invoke(new Action<string>(updateGitStatusText), new object [] { text });
         }
         else
         {
            labelGitStatus.Visible = true;
            labelGitStatus.Text = text;
         }
      }

      async private Task<ILocalGitRepositoryFactory> getLocalGitRepositoryFactory(string localFolder)
      {
         if (_gitClientFactory == null || _gitClientFactory.ParentFolder != localFolder)
         {
            await disposeLocalGitRepositoryFactory();

            try
            {
               _gitClientFactory = new LocalGitRepositoryFactory(localFolder,
                  _mergeRequestCache.GetProjectWatcher(), this);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot create LocalGitRepositoryFactory"), ex);
            }
         }
         return _gitClientFactory;
      }

      async private Task disposeLocalGitRepositoryFactory()
      {
         if (_gitClientFactory != null)
         {
            await _gitClientFactory.DisposeAsync();
            _gitClientFactory = null;
         }
      }

      /// <summary>
      /// Make some checks and create a repository
      /// </summary>
      /// <returns>null if could not create a repository</returns>
      async private Task<ILocalGitRepository> getRepository(ProjectKey key, bool showMessageBoxOnError)
      {
         ILocalGitRepositoryFactory factory = await getLocalGitRepositoryFactory(Program.Settings.LocalGitFolder);
         if (factory == null)
         {
            return null;
         }

         try
         {
            return factory.GetRepository(key.HostName, key.ProjectName);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot create LocalGitRepository"), ex);
            if (showMessageBoxOnError)
            {
               MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
         }
      }

      private string getInitialHostName()
      {
         // If Last Selected Host is in the list, select it as initial host.
         // Otherwise, select the first host from the list.
         for (int iKnownHost = 0; iKnownHost < Program.Settings.KnownHosts.Count(); ++iKnownHost)
         {
            if (Program.Settings.KnownHosts[iKnownHost] == _initialHostName)
            {
               return _initialHostName;
            }
         }
         return Program.Settings.KnownHosts.Count() > 0 ? Program.Settings.KnownHosts[0] : String.Empty;
      }

      private bool isTrackingTime()
      {
         return _timeTracker != null;
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

      private System.Drawing.Color getCommitComboBoxItemColor(CommitComboBoxItem item)
      {
         if (!getMergeRequestKey().HasValue)
         {
            return SystemColors.Window;
         }

         MergeRequestKey mrk = getMergeRequestKey().Value;
         bool wasReviewed = _reviewedCommits.ContainsKey(mrk) && _reviewedCommits[mrk].Contains(item.SHA);
         return wasReviewed || item.IsBase ? SystemColors.Window :
            _colorScheme.GetColorOrDefault("Commits_NotReviewed", SystemColors.Window);
      }

      /// <summary>
      /// Clean up records that correspond to merge requests that have been closed
      /// </summary>
      private void cleanupReviewedCommits(string hostname, string projectname, IEnumerable<MergeRequest> mergeRequests)
      {
         IEnumerable<MergeRequestKey> toRemove = _reviewedCommits.Keys.Where(
            (x) => x.ProjectKey.HostName == hostname
                && x.ProjectKey.ProjectName == projectname
                && !mergeRequests.Any(y => x.IId == y.IId));
         foreach (MergeRequestKey key in toRemove.ToArray())
         {
            _reviewedCommits.Remove(key);
         }
      }

      private void updateVisibleMergeRequests()
      {
         IEnumerable<ProjectKey> projectKeys = listViewMergeRequests.Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);
         foreach (ProjectKey projectKey in projectKeys)
         {
            foreach (MergeRequest mergeRequest in _mergeRequestCache.GetMergeRequests(projectKey))
            {
               MergeRequestKey mrk = new MergeRequestKey { ProjectKey = projectKey, IId = mergeRequest.IId };
               int index = listViewMergeRequests.Items.Cast<ListViewItem>().ToList().FindIndex(
                  x =>
               {
                  FullMergeRequestKey fmk = (FullMergeRequestKey)x.Tag;
                  return fmk.ProjectKey.Equals(mrk.ProjectKey) && fmk.MergeRequest.IId == mrk.IId;
               });
               if (index == -1)
               {
                  addListViewMergeRequestItem(mrk);
               }
               else
               {
                  setListViewItemTag(listViewMergeRequests.Items[index], mrk);
               }
            }
         }

         string[] selected = ConfigurationHelper.GetLabels(Program.Settings);
         for (int index = listViewMergeRequests.Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listViewMergeRequests.Items[index].Tag;
            if (!_mergeRequestCache.GetMergeRequests(fmk.ProjectKey).Any(x => x.IId == fmk.MergeRequest.IId)
               || MergeRequestFilter.IsFilteredMergeRequest(fmk.MergeRequest, selected))
            {
               listViewMergeRequests.Items.RemoveAt(index);
            }
         }

         recalcRowHeightForMergeRequestListView(listViewMergeRequests);
         listViewMergeRequests.Invalidate();

         updateTrayIcon();
      }

      private void addListViewMergeRequestItem(MergeRequestKey mrk)
      {
         ListViewGroup group = listViewMergeRequests.Groups[mrk.ProjectKey.ProjectName];
         string[] items = Enumerable.Repeat(String.Empty, listViewMergeRequests.Columns.Count).ToArray();
         ListViewItem item = listViewMergeRequests.Items.Add(new ListViewItem(items, group));
         Debug.Assert(item.SubItems.Count == listViewMergeRequests.Columns.Count);
         setListViewItemTag(item, mrk);
      }

      private void setListViewItemTag(ListViewItem item, MergeRequestKey mrk)
      {
         MergeRequest? mergeRequest = _mergeRequestCache.GetMergeRequest(mrk);
         if (!mergeRequest.HasValue)
         {
            Trace.TraceError(String.Format("[MainForm] setListViewItemTag() cannot find MR with IId {0}", mrk.IId));
            Debug.Assert(false);
            return;
         }

         MergeRequest mr = mergeRequest.Value;

         FullMergeRequestKey fmk = new FullMergeRequestKey
         {
            ProjectKey = mrk.ProjectKey,
            MergeRequest = mr
         };
         item.Tag = fmk;

         string author = String.Format("{0}\n({1}{2})", mr.Author.Name,
            Constants.AuthorLabelPrefix, mr.Author.Username);

         string jiraServiceUrl = Program.ServiceManager.GetJiraServiceUrl();
         string jiraTask = getJiraTask(mr);
         string jiraTaskUrl = jiraServiceUrl != String.Empty && jiraTask != String.Empty ?
            jiraServiceUrl + "/browse/" + jiraTask : String.Empty;

         Func<MergeRequestKey, string> getTotalTimeText = (key) => convertTotalTimeToText(getTotalTime(key));

         Action<string, ListViewSubItemInfo> setSubItemTag = (columnTag, subItemInfo) =>
         {
            ColumnHeader columnHeader = listViewMergeRequests.Columns
               .Cast<ColumnHeader>()
               .SingleOrDefault(x => x.Tag.ToString() == columnTag);
            if (columnHeader == null)
            {
               Debug.Assert(false);
               return;
            }

            item.SubItems[columnHeader.Index].Tag = subItemInfo;
         };

         setSubItemTag("IId",          new ListViewSubItemInfo(() => mr.IId.ToString(),       () => mr.Web_Url));
         setSubItemTag("Author",       new ListViewSubItemInfo(() => author,                  () => String.Empty));
         setSubItemTag("Title",        new ListViewSubItemInfo(() => mr.Title,                () => String.Empty));
         setSubItemTag("Labels",       new ListViewSubItemInfo(() => formatLabels(mr),        () => String.Empty));
         setSubItemTag("Size",         new ListViewSubItemInfo(() => getSize(fmk),            () => String.Empty));
         setSubItemTag("Jira",         new ListViewSubItemInfo(() => jiraTask,                () => jiraTaskUrl));
         setSubItemTag("TotalTime",    new ListViewSubItemInfo(() => getTotalTimeText(mrk),   () => String.Empty));
         setSubItemTag("SourceBranch", new ListViewSubItemInfo(() => mr.Source_Branch,        () => String.Empty));
         setSubItemTag("TargetBranch", new ListViewSubItemInfo(() => mr.Target_Branch,        () => String.Empty));
      }

      private void recalcRowHeightForMergeRequestListView(ListView listView)
      {
         if (listView.Items.Count == 0)
         {
            return;
         }

         int maxLineCountInLabels = listView.Items.Cast<ListViewItem>().
            Select((x) => formatLabels(((FullMergeRequestKey)(x.Tag)).MergeRequest).Count((y) => y == '\n')).Max() + 1;
         int maxLineCountInAuthor = 2;
         int maxLineCount = Math.Max(maxLineCountInLabels, maxLineCountInAuthor);
         setListViewRowHeight(listView, listView.Font.Height * maxLineCount + 2);
      }

      private static string formatLabels(MergeRequest mergeRequest)
      {
         List<string> sortedLabels = new List<string>(mergeRequest.Labels);
         sortedLabels.Sort();

         var query = sortedLabels.GroupBy(
            (label) => label.StartsWith(Constants.GitLabLabelPrefix) && label.IndexOf('-') != -1 ?
               label.Substring(0, label.IndexOf('-')) : label,
            (label) => label,
            (baseLabel, labels) => new
            {
               Labels = labels
            });

         StringBuilder stringBuilder = new StringBuilder();
         foreach (var group in query)
         {
            stringBuilder.Append(String.Join(",", group.Labels));
            stringBuilder.Append("\n");
         }

         return stringBuilder.ToString().TrimEnd('\n');
      }

      private static readonly Regex jira_re = new Regex(@"(?'name'(?!([A-Z0-9a-z]{1,10})-?$)[A-Z]{1}[A-Z0-9]+-\d+)");
      private static string getJiraTask(MergeRequest mergeRequest)
      {
         Match m = jira_re.Match(mergeRequest.Title);
         return !m.Success || m.Groups.Count < 1 || !m.Groups["name"].Success ? String.Empty : m.Groups["name"].Value;
      }

      private static void setListViewRowHeight(ListView listView, int height)
      {
         ImageList imgList = new ImageList
         {
            ImageSize = new Size(1, height)
         };
         listView.SmallImageList = imgList;
      }

      private void openBrowser(string text)
      {
         Trace.TraceInformation(String.Format("[Mainform] Opening browser with URL {0}", text));

         try
         {
            Process.Start(text);
         }
         catch (Exception ex) // see Process.Start exception list
         {
            string errorMessage = "Cannot open URL";
            ExceptionHandlers.Handle(errorMessage, ex);
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void processUpdate(MergeRequestEvent e)
      {
         updateVisibleMergeRequests();

         if (listViewMergeRequests.SelectedItems.Count == 0)
         {
            return;
         }

         ListViewItem selected = listViewMergeRequests.SelectedItems[0];
         FullMergeRequestKey fmk = (FullMergeRequestKey)selected.Tag;

         if (fmk.ProjectKey.Equals(e.FullMergeRequestKey.ProjectKey) &&
             fmk.MergeRequest.IId == e.FullMergeRequestKey.MergeRequest.IId)
         {
            if (e.Details)
            {
               // Details are partially updated here and partially in updateVisibleMergeRequests() above
               updateMergeRequestDetails(fmk);
            }

            if (e.Commits)
            {
               Trace.TraceInformation("[MainForm] Reloading current Merge Request");

               selected.Selected = false;
               selected.Selected = true;
            }
         }
      }

      private void checkForApplicationUpdates()
      {
         if (!_checkForUpdatesTimer.Enabled)
         {
            _checkForUpdatesTimer.Tick += new System.EventHandler(onTimerCheckForUpdates);
            _checkForUpdatesTimer.Start();
         }

         LatestVersionInformation? info = Program.ServiceManager.GetLatestVersionInfo();
         if (!info.HasValue
           || String.IsNullOrEmpty(info.Value.VersionNumber)
           || info.Value.VersionNumber == Application.ProductVersion
           || (!String.IsNullOrEmpty(_newVersionNumber) && info.Value.VersionNumber == _newVersionNumber))
         {
            return;
         }

         Trace.TraceInformation(String.Format("[CheckForUpdates] New version {0} is found", info.Value.VersionNumber));

         if (String.IsNullOrEmpty(info.Value.InstallerFilePath) || !File.Exists(info.Value.InstallerFilePath))
         {
            Trace.TraceWarning(String.Format("[CheckForUpdates] Installer cannot be found at \"{0}\"",
               info.Value.InstallerFilePath));
            return;
         }

         Task.Run(
            () =>
         {
            if (!info.HasValue)
            {
               return;
            }

            string filename = Path.GetFileName(info.Value.InstallerFilePath);
            string tempFolder = Environment.GetEnvironmentVariable("TEMP");
            string destFilePath = Path.Combine(tempFolder, filename);

            Debug.Assert(!File.Exists(destFilePath));

            try
            {
               File.Copy(info.Value.InstallerFilePath, destFilePath);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle("Cannot download a new version", ex);
               return;
            }

            _newVersionFilePath = destFilePath;
            _newVersionNumber = info.Value.VersionNumber;
            BeginInvoke(new Action(() =>
            {
               linkLabelNewVersion.Visible = true;
               updateCaption();
            }));
         });
      }

      private void cleanUpInstallers()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");
         foreach (string f in System.IO.Directory.EnumerateFiles(tempFolder, "mrHelper.*.msi"))
         {
            try
            {
               System.IO.File.Delete(f);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot delete installer \"{0}\"", f), ex);
            }
         }
      }

      private void updateCaption()
      {
         Text = Constants.MainWindowCaption
           + " (" + Application.ProductVersion + ")"
           + (!String.IsNullOrEmpty(_newVersionNumber) ? String.Format(
              "   New version {0} is available!", _newVersionNumber) : String.Empty);
      }

      private void updateTrayIcon()
      {
         notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
         if (_iconScheme == null || _iconScheme.Count == 0)
         {
            return;
         }

         Action<string> loadNotifyIconFromFile =
            (filename) =>
         {
            try
            {
               notifyIcon.Icon = new Icon(filename);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot create an icon from file \"{0}\"", filename), ex);
            }
         };

         if (isTrackingTime())
         {
            if (_iconScheme.ContainsKey("Icon_Tracking"))
            {
               loadNotifyIconFromFile(_iconScheme["Icon_Tracking"]);
            };
            return;
         }

         foreach (KeyValuePair<string, string> nameToFilename in _iconScheme)
         {
            string resolved = _expressionResolver.Resolve(nameToFilename.Key);
            if (listViewMergeRequests.Items
               .Cast<ListViewItem>()
               .Select(x => x.Tag)
               .Cast<FullMergeRequestKey>()
               .Select(x => x.MergeRequest)
               .Any(x => x.Labels.Any(y => StringUtils.DoesMatchPattern(resolved, "Icon_{{Label:{0}}}", y))))
            {
               loadNotifyIconFromFile(nameToFilename.Value);
               break;
            }
         }
      }

      private void applyTheme(string theme)
      {
         if (theme == "New Year 2020")
         {
            pictureBox1.BackgroundImage = mrHelper.App.Properties.Resources.PleaseInspect;
            pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox1.Visible = true;
            pictureBox2.BackgroundImage = mrHelper.App.Properties.Resources.Tree;
            pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox2.Visible = true;
            listViewMergeRequests.BackgroundImage = mrHelper.App.Properties.Resources.SnowflakeBg;
            listViewMergeRequests.BackgroundImageTiled = true;
            richTextBoxMergeRequestDescription.BaseStylesheet =
                 mrHelper.App.Properties.Resources.MergeRequestDescriptionCSS
               + mrHelper.App.Properties.Resources.NewYear2020_CSS;
         }
         else
         {
            pictureBox1.BackgroundImage = null;
            pictureBox1.Visible = false;
            pictureBox2.BackgroundImage = null;
            pictureBox2.Visible = false;
            listViewMergeRequests.BackgroundImage = null;
            richTextBoxMergeRequestDescription.BaseStylesheet =
               mrHelper.App.Properties.Resources.MergeRequestDescriptionCSS;
         }

         richTextBoxMergeRequestDescription.BaseStylesheet +=
            String.Format("body div {{ font-size: {0}px; }}", this.Font.Height);

         Program.Settings.VisualThemeName = theme;

         resetMinimumSizes();
      }

      private void onHostSelected()
      {
         updateProjectsListView();
      }

      private void updateProjectsListView()
      {
         listViewProjects.Items.Clear();

         Program.Settings.GetEnabledProjects(getHostName())
            .ToList()
            .ForEach(x => listViewProjects.Items.Add(x));
      }

      private int calcHorzDistance(Control leftControl, Control rightControl, bool preventOverlap = false)
      {
         int res = 0;
         if (leftControl != null && rightControl != null)
         {
            res = rightControl.Location.X - (leftControl.Location.X + leftControl.Size.Width);
         }
         else if (leftControl == null && rightControl != null)
         {
            res = rightControl.Location.X;
         }
         else if (leftControl != null && rightControl == null)
         {
            res = leftControl.Parent.Size.Width - (leftControl.Location.X + leftControl.Size.Width);
         }

         if (!preventOverlap && res < 0)
         {
            Trace.TraceWarning(
               "calcHorzDistance() returns negative value ({0}). " +
               "leftControl: {1} (Location: {{{2}, {3}}}, Size: {{{4}, {5}}}), " +
               "rightControl: {6} (Location: {{{7}, {8}}}, Size: {{{9}, {10}}}), " +
               "PreventOverlap: {11}",
               res,
               leftControl?.Name ?? "null",
               leftControl?.Location.X.ToString() ?? "N/A", leftControl?.Location.Y.ToString() ?? "N/A",
               leftControl?.Size.Width.ToString() ?? "N/A", leftControl?.Size.Height.ToString() ?? "N/A",
               rightControl?.Name ?? "null",
               rightControl?.Location.X.ToString() ?? "N/A", rightControl?.Location.Y.ToString() ?? "N/A",
               rightControl?.Size.Width.ToString() ?? "N/A", rightControl?.Size.Height.ToString() ?? "N/A",
               preventOverlap);
            Debug.Assert(false);
         }

         return res < 0 && preventOverlap ? 10 : res;
      }

      private int calcVertDistance(Control topControl, Control bottomControl, bool preventOverlap = false)
      {
         int res = 0;
         if (topControl != null && bottomControl != null)
         {
            res = bottomControl.Location.Y - (topControl.Location.Y + topControl.Size.Height);
         }
         else if (topControl == null && bottomControl != null)
         {
            res = bottomControl.Location.Y;
         }
         else if (topControl != null && bottomControl == null)
         {
            res = topControl.Parent.Size.Height - (topControl.Location.Y + topControl.Size.Height);
         }

         if (!preventOverlap && res < 0)
         {
            Trace.TraceWarning(
               "calcVertDistance() returns negative value ({0}). " +
               "topControl: {1} (Location: {{{2}, {3}}}, Size: {{{4}, {5}}}), " +
               "bottomControl: {6} (Location: {{{7}, {8}}}, Size: {{{9}, {10}}}), " +
               "PreventOverlap: {11}",
               res,
               topControl?.Name ?? "null",
               topControl?.Location.X.ToString() ?? "N/A", topControl?.Location.Y.ToString() ?? "N/A",
               topControl?.Size.Width.ToString() ?? "N/A", topControl?.Size.Height.ToString() ?? "N/A",
               bottomControl?.Name ?? "null",
               bottomControl?.Location.X.ToString() ?? "N/A", bottomControl?.Location.Y.ToString() ?? "N/A",
               bottomControl?.Size.Width.ToString() ?? "N/A", bottomControl?.Size.Height.ToString() ?? "N/A",
               preventOverlap);
            Debug.Assert(false);
         }

         return res < 0 && preventOverlap ? 10 : res;
      }

      private void resetMinimumSizes()
      {
         int defaultSplitContainerPanelMinSize = 25;
         splitContainer1.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer1.Panel2MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel2MinSize = defaultSplitContainerPanelMinSize;

         this.MinimumSize = new System.Drawing.Size(0, 0);

         _invalidMinSizes = true;
      }

      private bool _invalidMinSizes = false;

      private int getLeftPaneMinWidth()
      {
         return
            calcHorzDistance(null, groupBoxSelectMergeRequest)
          + calcHorzDistance(null, checkBoxLabels)
          + checkBoxLabels.MinimumSize.Width
          + calcHorzDistance(checkBoxLabels, textBoxLabels)
          + textBoxLabels.MinimumSize.Width
          + calcHorzDistance(textBoxLabels, buttonReloadList, true)
          + buttonReloadList.MinimumSize.Width
          + calcHorzDistance(buttonReloadList, null)
          + calcHorzDistance(groupBoxSelectMergeRequest, null);
      }

      private int getRightPaneMinWidth()
      {
         Func<IEnumerable<Control>, int, int> calcMinWidthOfControlGroup = (controls, minGap) =>
            controls.Cast<Control>().Sum(x => x.MinimumSize.Width) + (controls.Count() - 1) * minGap;

         int buttonMinDistance = calcHorzDistance(buttonAddComment, buttonNewDiscussion);

         int groupBoxReviewMinWidth =
            calcMinWidthOfControlGroup(groupBoxReview.Controls.Cast<Control>(), buttonMinDistance)
            + calcHorzDistance(null, groupBoxReview)
            + calcHorzDistance(null, buttonAddComment)
            + calcHorzDistance(buttonDiffTool, null)
            + calcHorzDistance(groupBoxReview, null);

         int groupBoxTimeTrackingMinWidth = calcMinWidthOfControlGroup(
            new Control[] { buttonTimeTrackingStart, buttonTimeTrackingCancel, buttonEditTime }, buttonMinDistance)
            + calcHorzDistance(null, groupBoxTimeTracking)
            + calcHorzDistance(null, buttonTimeTrackingStart)
            + calcHorzDistance(buttonEditTime, null)
            + calcHorzDistance(groupBoxTimeTracking, null);

         bool hasActions = groupBoxActions.Controls.Count > 0;
         int groupBoxActionsMinWidth =
            calcMinWidthOfControlGroup(groupBoxActions.Controls.Cast<Control>(), buttonMinDistance)
            + calcHorzDistance(null, groupBoxActions)
            + calcHorzDistance(null, hasActions ? buttonAddComment : null) // First button is aligned with "Add Comment"
            + calcHorzDistance(hasActions ? buttonDiffTool : null, null)   // Last button is aligned with "Diff Tool"
            + calcHorzDistance(groupBoxActions, null);

         bool hasPicture1 = pictureBox1.BackgroundImage != null;
         bool hasPicture2 = pictureBox2.BackgroundImage != null;

         int panelFreeSpaceMinWidth =
            calcHorzDistance(null, panelFreeSpace)
          + (hasPicture1 ? calcHorzDistance(null, pictureBox1) + pictureBox1.MinimumSize.Width : panelFreeSpace.MinimumSize.Width)
          + (hasPicture2 ? pictureBox2.MinimumSize.Width + calcHorzDistance(pictureBox2, null) : panelFreeSpace.MinimumSize.Width)
          + calcHorzDistance(panelFreeSpace, null);

         return Enumerable.Max(new int[]
            { groupBoxReviewMinWidth, groupBoxTimeTrackingMinWidth, groupBoxActionsMinWidth, panelFreeSpaceMinWidth });
      }

      private int getTopRightPaneMinHeight()
      {
         return
            + calcVertDistance(null, groupBoxSelectedMR)
            + calcVertDistance(null, richTextBoxMergeRequestDescription)
            + 100 /* cannot use richTextBoxMergeRequestDescription.MinimumSize.Height, see 9b65d7413c */
            + calcVertDistance(richTextBoxMergeRequestDescription, linkLabelConnectedTo, true)
            + linkLabelConnectedTo.Height
            + calcVertDistance(linkLabelConnectedTo, null)
            + calcVertDistance(groupBoxSelectedMR, null);
      }

      private int getBottomRightPaneMinHeight()
      {
         bool hasPicture1 = pictureBox1.BackgroundImage != null;
         bool hasPicture2 = pictureBox2.BackgroundImage != null;

         int panelFreeSpaceMinHeight =
            Math.Max(
               (hasPicture1 ?
                  calcVertDistance(null, pictureBox1)
                + pictureBox1.MinimumSize.Height
                + calcVertDistance(pictureBox1, null, true) : panelFreeSpace.MinimumSize.Height),
               (hasPicture2 ?
                  calcVertDistance(null, pictureBox2)
                + pictureBox2.MinimumSize.Height
                + calcVertDistance(pictureBox2, null, true) : panelFreeSpace.MinimumSize.Height));

         return
              calcVertDistance(null, groupBoxSelectCommits)
            + groupBoxSelectCommits.Height
            + calcVertDistance(groupBoxSelectCommits, groupBoxReview)
            + groupBoxReview.Height
            + calcVertDistance(groupBoxReview, groupBoxTimeTracking)
            + groupBoxTimeTracking.Height
            + calcVertDistance(groupBoxTimeTracking, groupBoxActions)
            + groupBoxActions.Height
            + calcVertDistance(groupBoxActions, panelFreeSpace)
            + panelFreeSpaceMinHeight
            + calcVertDistance(panelFreeSpace, panelStatusBar, true)
            + panelStatusBar.Height
            + calcVertDistance(panelStatusBar, panelBottomMenu)
            + panelBottomMenu.Height
            + calcVertDistance(panelBottomMenu, null);
      }

      private void updateMinimumSizes()
      {
         if (!_invalidMinSizes)
         {
            return;
         }

         if (Program.Settings.DisableSplitterRestrictions)
         {
            resetMinimumSizes();
            _invalidMinSizes = false;
            return;
         }

         int leftPaneMinWidth = getLeftPaneMinWidth();
         int rightPaneMinWidth = getRightPaneMinWidth();
         int topRightPaneMinHeight = getTopRightPaneMinHeight();
         int bottomRightPaneMinHeight = getBottomRightPaneMinHeight();

         int clientAreaMinWidth =
            calcHorzDistance(null, tabPageMR)
          + calcHorzDistance(null, splitContainer1)
          + leftPaneMinWidth
          + splitContainer1.SplitterWidth
          + rightPaneMinWidth
          + calcHorzDistance(splitContainer1, null)
          + calcHorzDistance(tabPageMR, null);
         int nonClientAreaWidth = this.Size.Width - this.ClientSize.Width;

         int clientAreaMinHeight =
            calcVertDistance(null, tabPageMR)
          + calcVertDistance(null, splitContainer1)
          + calcVertDistance(null, splitContainer2)
          + topRightPaneMinHeight
          + splitContainer2.SplitterWidth
          + bottomRightPaneMinHeight
          + calcVertDistance(splitContainer2, null)
          + calcVertDistance(splitContainer1, null)
          + calcVertDistance(tabPageMR, null);
         int nonClientAreaHeight = this.Size.Height - this.ClientSize.Height;

         // First, apply new size to the Form because this action resizes it the Format is too small for split containers
         this.MinimumSize = new Size(clientAreaMinWidth + nonClientAreaWidth, clientAreaMinHeight + nonClientAreaHeight);

         // Then, apply new sizes to split containers
         this.splitContainer1.Panel1MinSize = leftPaneMinWidth;
         this.splitContainer1.Panel2MinSize = rightPaneMinWidth;
         this.splitContainer2.Panel1MinSize = topRightPaneMinHeight;
         this.splitContainer2.Panel2MinSize = bottomRightPaneMinHeight;

         // Set default position for splitter
         this.splitContainer1.SplitterDistance = this.splitContainer1.Width - this.splitContainer1.Panel2MinSize;
         this.splitContainer2.SplitterDistance = this.splitContainer2.Height - this.splitContainer2.Panel2MinSize;

         _invalidMinSizes = false;
      }

      private void repositionCustomCommands()
      {
         Func<Control, int, int> getControlX = (control, index) =>
             control.Width * index +
                (groupBoxActions.Width - _customCommands.Count() * control.Width) *
                (index + 1) / (_customCommands.Count() + 1);

         for (int id = 0; id < groupBoxActions.Controls.Count; ++id)
         {
            Control c = groupBoxActions.Controls[id];
            c.Location = new Point { X = getControlX(c, id), Y = c.Location.Y };
         }
      }

      private void updateTabControlSelection()
      {
         bool configured = listViewKnownHosts.Items.Count > 0
                        && textBoxLocalGitFolder.Text.Length > 0
                        && Directory.Exists(textBoxLocalGitFolder.Text);
         if (configured)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else
         {
            tabControl.SelectedTab = tabPageSettings;
         }
      }
   }
}

