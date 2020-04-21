using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using static mrHelper.App.Helpers.ServiceManager;
using mrHelper.Client.Discussions;
using mrHelper.Client.Types;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private string getHostName()
      {
         return comboBoxHost.SelectedItem != null ? ((HostComboBoxItem)comboBoxHost.SelectedItem).Host : String.Empty;
      }

      private bool isSearchMode()
      {
         return tabControlMode.SelectedTab == tabPageSearch;
      }

      private MergeRequest? getMergeRequest(ListView proposedListView)
      {
         ListView currentListView = isSearchMode() ? listViewFoundMergeRequests : listViewMergeRequests;
         ListView listView = proposedListView ?? currentListView;
         if (listView.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listView.SelectedItems[0].Tag;
            return fmk.MergeRequest;
         }
         Debug.Assert(false);
         return null;
      }

      private MergeRequestKey? getMergeRequestKey(ListView proposedListView)
      {
         ListView currentListView = isSearchMode() ? listViewFoundMergeRequests : listViewMergeRequests;
         ListView listView = proposedListView ?? currentListView;
         if (listView.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listView.SelectedItems[0].Tag;
            return new MergeRequestKey { ProjectKey = fmk.ProjectKey, IId = fmk.MergeRequest.IId };
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

      private void createListViewGroupsForProjects(ListView listView,
         string hostname, IEnumerable<Project> projects)
      {
         listView.Items.Clear();
         listView.Groups.Clear();
         foreach (Project project in projects)
         {
            createListViewGroupForProject(listView, hostname, project);
         }
      }

      private void createListViewGroupForProject(ListView listView,
         string hostname, Project project)
      {
         ListViewGroup group = listView.Groups.Add(
            project.Path_With_Namespace, project.Path_With_Namespace);
         group.Tag = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
      }

      private bool selectMergeRequest(ListView listView, string projectname, int iid, bool exact)
      {
         // CAUTION: this method fires ListViewMergeRequests_ItemSelectionChanged event handler which is an async method.
         // However, as the event is fired by .NET Framework internally, we cannot await it and execution does not stop.

         foreach (ListViewItem item in listView.Items)
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
         foreach (ListViewGroup group in listView.Groups)
         {
            if (projectname == group.Name && group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         // select whatever
         foreach (ListViewGroup group in listView.Groups)
         {
            if (group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         return false;
      }

      private static void selectNotReviewedCommits(bool isSearchMode,
         ComboBox comboBoxLatestCommit, ComboBox comboBoxEarliestCommit, HashSet<string> reviewedCommits)
      {
         Tuple<int, int> getCommitIndices()
         {
            int DefaultLatestIndex = 0;
            int DefaultEarliestIndex = comboBoxEarliestCommit.Items.Count - 1;

            int? iNewestReviewedCommitIndex = new Nullable<int>();
            if (!isSearchMode)
            {
               for (int iItem = 0; iItem < comboBoxLatestCommit.Items.Count; ++iItem)
               {
                  string sha = ((CommitComboBoxItem)(comboBoxLatestCommit.Items[iItem])).SHA;
                  if (reviewedCommits.Contains(sha))
                  {
                     iNewestReviewedCommitIndex = iItem;
                     break;
                  }
               }
            }

            if (!iNewestReviewedCommitIndex.HasValue)
            {
               return new Tuple<int, int>(DefaultLatestIndex, DefaultEarliestIndex);
            }

            if (Program.Settings.AutoSelectNewestCommit)
            {
               return new Tuple<int, int>(0, Math.Max(0, iNewestReviewedCommitIndex.Value - 1));
            }
            else
            {
               // note that `earliest` should not be `latest + 1` because Latest CB is shifted comparing to Earliest CB
               int latest = Math.Max(0, iNewestReviewedCommitIndex.Value - 1);
               int earliest = Math.Min(latest, comboBoxEarliestCommit.Items.Count - 1);
               return new Tuple<int, int>(latest, earliest);
            }
         }

         Debug.Assert(comboBoxLatestCommit.Items.Count == comboBoxEarliestCommit.Items.Count);
         Tuple<int, int> indices = getCommitIndices();
         comboBoxLatestCommit.SelectedIndex = indices.Item1;
         comboBoxEarliestCommit.SelectedIndex = indices.Item2;
      }

      private HashSet<string> getReviewedCommits(MergeRequestKey mrk)
      {
         return _reviewedCommits.ContainsKey(mrk) ? _reviewedCommits[mrk] : new HashSet<string>();
      }

      private static void checkComboboxCommitsOrder(ComboBox comboBoxLatestCommit, ComboBox comboBoxEarliestCommit,
         bool shouldReorderRightCombobox)
      {
         if (comboBoxLatestCommit.SelectedItem == null || comboBoxEarliestCommit.SelectedItem == null)
         {
            return;
         }

         // Latest combobox cannot select a commit older than in Earliest combobox (replicating gitlab web ui behavior)
         CommitComboBoxItem latestCBItem = (CommitComboBoxItem)(comboBoxLatestCommit.SelectedItem);
         CommitComboBoxItem earliestCBItem = (CommitComboBoxItem)(comboBoxEarliestCommit.SelectedItem);
         Debug.Assert(latestCBItem.TimeStamp.HasValue);

         if (earliestCBItem.TimeStamp.HasValue)
         {
            // Check if order is broken
            if (latestCBItem.TimeStamp.Value <= earliestCBItem.TimeStamp.Value)
            {
               if (shouldReorderRightCombobox)
               {
                  comboBoxEarliestCommit.SelectedIndex = comboBoxLatestCommit.SelectedIndex;
               }
               else
               {
                  comboBoxLatestCommit.SelectedIndex = comboBoxEarliestCommit.SelectedIndex;
               }
            }
         }
         else
         {
            // It is ok because a commit w/o timestamp is the oldest one
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
         bool createColorScheme(string filename)
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
         }

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
         deselectAllListViewItems(listView);

         if (clear)
         {
            listView.Items.Clear();
         }
      }

      private void enableListView(ListView listView)
      {
         listView.Enabled = true;
      }

      private void deselectAllListViewItems(ListView listView)
      {
         foreach (ListViewItem item in listView.Items)
         {
            item.Selected = false;
         }
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

      private void enableControlsOnGitAsyncOperation(bool enabled, string operation)
      {
         linkLabelAbortGit.Visible = !enabled;
         linkLabelAbortGit.Tag = operation;

         tabPageSettings.Controls.Cast<Control>().ToList().ForEach((x) => x.Enabled = enabled);

         buttonDiffTool.Enabled = enabled;
         buttonDiscussions.Enabled = enabled;
         listViewMergeRequests.Enabled = enabled;
         listViewFoundMergeRequests.Enabled = enabled;
         enableMergeRequestFilterControls(enabled);
         enableMergeRequestSearchControls(enabled);

         buttonReloadList.Enabled = enabled;
         _suppressExternalConnections = !enabled;
         _canSwitchTab = enabled;

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

      private void enableMergeRequestSearchControls(bool enabled)
      {
         textBoxSearch.Enabled = enabled;
      }

      private void updateMergeRequestDetails(FullMergeRequestKey? fmk)
      {
         string body = fmk.HasValue
            ? MarkDownUtils.ConvertToHtml(
               fmk.Value.MergeRequest.Description,
               String.Format("{0}/{1}",
                  StringUtils.GetHostWithPrefix(fmk.Value.ProjectKey.HostName), fmk.Value.ProjectKey.ProjectName),
               _mergeRequestDescriptionMarkdownPipeline)
            : String.Empty;

         richTextBoxMergeRequestDescription.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         richTextBoxMergeRequestDescription.Update();
         linkLabelConnectedTo.Text = fmk.HasValue ? fmk.Value.MergeRequest.Web_Url : String.Empty;
      }

      private void updateTimeTrackingMergeRequestDetails(bool enabled, string title = null,
         ProjectKey projectKey = default(ProjectKey), User author = default(User))
      {
         if (isTrackingTime())
         {
            return;
         }

         if (!isTimeTrackingAllowed(author, projectKey.HostName))
         {
            enabled = false;
         }

         labelTimeTrackingMergeRequestName.Visible = enabled;
         buttonTimeTrackingStart.Enabled = enabled;

         if (enabled)
         {
            Debug.Assert(!String.IsNullOrEmpty(title) && !projectKey.Equals(default(ProjectKey)));
            labelTimeTrackingMergeRequestName.Text = String.Format("{0}   [{1}]", title, projectKey.ProjectName);
         }

         labelTimeTrackingMergeRequestName.Refresh();
      }

      private void updateTotalTime(MergeRequestKey? mrk = null, User author = default(User), string hostname = null)
      {
         if (isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text =
               String.Format("Tracked Time: {0}", _timeTracker.Elapsed.ToString(@"hh\:mm\:ss"));
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrk.HasValue || !isTimeTrackingAllowed(author, hostname))
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            buttonEditTime.Enabled = false;
         }
         else
         {
            TimeSpan? span = getTotalTime(mrk.Value);
            labelTimeTrackingTrackedLabel.Text = String.Format("Total Time: {0}",
               convertTotalTimeToText(span, true));
            buttonEditTime.Enabled = span.HasValue;
         }

         // Update total time column in the table
         listViewMergeRequests.Invalidate();
         labelTimeTrackingTrackedLabel.Refresh();
      }

      private TimeSpan? getTotalTime(MergeRequestKey? mrk)
      {
         if (!mrk.HasValue)
         {
            return null;
         }

         return _timeTrackingManager.GetTotalTime(mrk.Value);
      }

      private string convertTotalTimeToText(TimeSpan? span, bool isTimeTrackingAllowed)
      {
         if (!span.HasValue)
         {
            return "N/A";
         }

         // See comment for TimeSpan.MinValue in TimeTrackingManager
         if (span.Value == TimeSpan.MinValue)
         {
            return "Loading...";
         }

         if (span.Value != TimeSpan.Zero)
         {
            return span.Value.ToString(@"hh\:mm\:ss");
         }

         return isTimeTrackingAllowed ? "Not Started" : "Not Allowed";
      }

      private bool isTimeTrackingAllowed(User mergeRequestAuthor, string hostname)
      {
         if (mergeRequestAuthor.Id == default(User).Id || String.IsNullOrWhiteSpace(hostname))
         {
            return true;
         }

         return !_currentUser.ContainsKey(hostname)
              || _currentUser[hostname].Id != mergeRequestAuthor.Id
              || Program.Settings.AllowAuthorToTrackTime;
      }

      private string getDiscussionCount(MergeRequestKey mrk)
      {
         if (_discussionManager == null)
         {
            return "N/A";
         }

         DiscussionManager.DiscussionCount dc = _discussionManager.GetDiscussionCount(mrk);
         switch (dc.Status)
         {
            case DiscussionManager.DiscussionCount.EStatus.NotAvailable:
               return "N/A";

            case DiscussionManager.DiscussionCount.EStatus.Loading:
               return "Loading...";

            case DiscussionManager.DiscussionCount.EStatus.Ready:
               return String.Format("{0} / {1}", dc.Resolved.Value, dc.Resolvable.Value);
         }

         Debug.Assert(false);
         return "N/A";
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

      private void enableCommitActions(bool enabled, IEnumerable<string> labels, User author)
      {
         buttonDiscussions.Enabled = enabled; // not a commit action but depends on git
         buttonDiffTool.Enabled = enabled;
         checkBoxShowVersions.Enabled = enabled;
         enableCustomActions(enabled, labels, author);
      }

      private bool isCustomActionEnabled(IEnumerable<string> labels, User author, string dependency)
      {
         if (String.IsNullOrEmpty(dependency))
         {
            return true;
         }

         if (labels.Any(x => StringUtils.DoesMatchPattern(dependency, "{{Label:{0}}}", x)))
         {
            return true;
         }

         if (StringUtils.DoesMatchPattern(dependency, "{{Author:{0}}}", author.Username))
         {
            return true;
         }

         return false;
      }

      private void enableCustomActions(bool enabled, IEnumerable<string> labels, User author)
      {
         if (!enabled)
         {
            groupBoxActions.Controls.Cast<Control>().ToList().ForEach(x => x.Enabled = false);
            return;
         }

         foreach (Control control in groupBoxActions.Controls)
         {
            string dependency = (string)control.Tag;
            string resolvedDependency =
               String.IsNullOrEmpty(dependency) ? String.Empty : _expressionResolver.Resolve(dependency);
            control.Enabled = isCustomActionEnabled(labels, author, resolvedDependency);
         }
      }

      private static CommitComboBoxItem getItem(Commit commit,
         ECommitComboBoxItemStatus status, EComparableEntityType type)
      {
         Debug.Assert(type == EComparableEntityType.Commit);
         return new CommitComboBoxItem(commit.Id, commit.Title, commit.Created_At, commit.Message,
            status, EComparableEntityType.Commit);
      }

      private static CommitComboBoxItem getItem(GitLabSharp.Entities.Version version, int? index,
         ECommitComboBoxItemStatus status, EComparableEntityType type)
      {
         string sha = version.Head_Commit_SHA;
         string versionNumber = index.HasValue ? index.ToString() : String.Empty;
         return new CommitComboBoxItem(
            /* sha */            sha,
            /* title */          String.Format("Version {0}", versionNumber),
            /* timestamp */      version.Created_At,
            /* commit message */ String.Empty,
            /* status */         status,
            /* type */           type);
      }

      private static void addCommitsToComboBoxes(ComboBox comboBoxLatestCommit, ComboBox comboBoxEarliestCommit,
         IEnumerable<object> commits, string baseSha, string targetBranch)
      {
         EComparableEntityType getType()
         {
            if (!commits.Any())
            {
               Debug.Assert(false);
               return EComparableEntityType.Commit;
            }

            object first = commits.First();
            if (first is Commit)
            {
               return EComparableEntityType.Commit;
            }

            Debug.Assert(first is GitLabSharp.Entities.Version);
            return EComparableEntityType.Version;
         }

         CommitComboBoxItem getCommitItem(object commit, int? index, ECommitComboBoxItemStatus status)
         {
            CommitComboBoxItem result = new CommitComboBoxItem();
            if (commit is Commit c)
            {
               result = getItem(c, status, getType());
            }
            else if (commit is GitLabSharp.Entities.Version v)
            {
               result = getItem(v, index, status, getType());
            }
            return result;
         }

         // Add latest commit
         CommitComboBoxItem latestCommitItem = getCommitItem(commits.First(), null, ECommitComboBoxItemStatus.Latest);
         comboBoxLatestCommit.Items.Add(latestCommitItem);

         // Add other commits
         int iCommit = commits.Count() - 1;
         foreach (object commit in commits.Skip(1))
         {
            CommitComboBoxItem item = getCommitItem(commit, iCommit--, ECommitComboBoxItemStatus.Normal);

            if (getType() == EComparableEntityType.Commit
             && comboBoxLatestCommit.Items.Cast<CommitComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLatestCommit.Items.Add(item);
            comboBoxEarliestCommit.Items.Add(item);
         }

         // Add target branch to the right combo-box
         CommitComboBoxItem baseCommitItem = new CommitComboBoxItem(baseSha, targetBranch, null, String.Empty,
            ECommitComboBoxItemStatus.Base, getType());
         comboBoxEarliestCommit.Items.Add(baseCommitItem);
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
               _gitClientFactory = new LocalGitRepositoryFactory(
                  localFolder, this, Program.Settings.UseShallowClone);
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
            LocalGitRepositoryFactory factory = _gitClientFactory;
            _gitClientFactory = null;
            await factory.DisposeAsync();
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

         ILocalGitRepository repo = factory.GetRepository(key.HostName, key.ProjectName);
         if (repo == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format(
               "Cannot initialize git repository for project {0} in \"{1}\"",
               key.ProjectName, Program.Settings.LocalGitFolder), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         return repo;
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

      private System.Drawing.Color getDiscussionCountColor(FullMergeRequestKey fmk, bool isSelected)
      {
         DiscussionManager.DiscussionCount dc = _discussionManager.GetDiscussionCount(
            new MergeRequestKey
            {
               IId = fmk.MergeRequest.IId,
               ProjectKey = fmk.ProjectKey
            });

         if (dc.Status != DiscussionManager.DiscussionCount.EStatus.Ready
          || dc.Resolvable == null || dc.Resolved == null)
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

      private System.Drawing.Color getCommitComboBoxItemColor(CommitComboBoxItem item)
      {
         if (!getMergeRequestKey(null).HasValue)
         {
            return SystemColors.Window;
         }

         MergeRequestKey mrk = getMergeRequestKey(null).Value;
         bool wasReviewed = _reviewedCommits.ContainsKey(mrk) && _reviewedCommits[mrk].Contains(item.SHA);
         return wasReviewed || item.Status == ECommitComboBoxItemStatus.Base ? SystemColors.Window :
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
                  ListViewItem item = addListViewMergeRequestItem(listViewMergeRequests, projectKey);
                  setListViewItemTag(item, mrk);
               }
               else
               {
                  setListViewItemTag(listViewMergeRequests.Items[index], mrk);
               }
            }
         }

         for (int index = listViewMergeRequests.Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listViewMergeRequests.Items[index].Tag;
            if (!_mergeRequestCache.GetMergeRequests(fmk.ProjectKey).Any(x => x.IId == fmk.MergeRequest.IId)
               || !_mergeRequestFilter.DoesMatchFilter(fmk.MergeRequest))
            {
               listViewMergeRequests.Items.RemoveAt(index);
            }
         }

         recalcRowHeightForMergeRequestListView(listViewMergeRequests);
         listViewMergeRequests.Invalidate();

         updateTrayIcon();
      }

      private ListViewItem addListViewMergeRequestItem(ListView listView, ProjectKey projectKey)
      {
         ListViewGroup group = listView.Groups[projectKey.ProjectName];
         string[] items = Enumerable.Repeat(String.Empty, listView.Columns.Count).ToArray();
         ListViewItem item = listView.Items.Add(new ListViewItem(items, group));
         Debug.Assert(item.SubItems.Count == listView.Columns.Count);
         return item;
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

         setListViewItemTag(item, mrk.ProjectKey, mergeRequest.Value);
      }

      private void setListViewItemTag(ListViewItem item, ProjectKey projectKey, MergeRequest mr)
      {
         MergeRequestKey mrk = new MergeRequestKey
         {
            ProjectKey = projectKey,
            IId = mr.IId
         };

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
            String.Format("{0}/browse/{1}", jiraServiceUrl, jiraTask) : String.Empty;

         string getTotalTimeText(MergeRequestKey key) =>
            convertTotalTimeToText(getTotalTime(key), isTimeTrackingAllowed(mr.Author, projectKey.HostName));

         void setSubItemTag(string columnTag, ListViewSubItemInfo subItemInfo)
         {
            ColumnHeader columnHeader = item.ListView.Columns
               .Cast<ColumnHeader>()
               .SingleOrDefault(x => x.Tag.ToString() == columnTag);
            if (columnHeader == null)
            {
               return;
            }

            item.SubItems[columnHeader.Index].Tag = subItemInfo;
         }

         setSubItemTag("IId",          new ListViewSubItemInfo(() => mr.IId.ToString(),         () => mr.Web_Url));
         setSubItemTag("Author",       new ListViewSubItemInfo(() => author,                    () => String.Empty));
         setSubItemTag("Title",        new ListViewSubItemInfo(() => mr.Title,                  () => String.Empty));
         setSubItemTag("Labels",       new ListViewSubItemInfo(() => formatLabels(mr),          () => String.Empty));
         setSubItemTag("Size",         new ListViewSubItemInfo(() => getSize(fmk),              () => String.Empty));
         setSubItemTag("Jira",         new ListViewSubItemInfo(() => jiraTask,                  () => jiraTaskUrl));
         setSubItemTag("TotalTime",    new ListViewSubItemInfo(() => getTotalTimeText(mrk),     () => String.Empty));
         setSubItemTag("SourceBranch", new ListViewSubItemInfo(() => mr.Source_Branch,          () => String.Empty));
         setSubItemTag("TargetBranch", new ListViewSubItemInfo(() => mr.Target_Branch,          () => String.Empty));
         setSubItemTag("State",        new ListViewSubItemInfo(() => mr.State,                  () => String.Empty));
         setSubItemTag("Resolved",     new ListViewSubItemInfo(() => getDiscussionCount(mrk),   () => String.Empty));
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

      private void processUpdate(Client.Types.UserEvents.MergeRequestEvent e)
      {
         scheduleSilentUpdate(e.FullMergeRequestKey.ProjectKey);

         updateVisibleMergeRequests();

         if (e.New)
         {
            MergeRequestKey mrk = new MergeRequestKey
            {
               ProjectKey = e.FullMergeRequestKey.ProjectKey,
               IId = e.FullMergeRequestKey.MergeRequest.IId
            };

            enqueueCheckForUpdates(mrk, new[] {
               Program.Settings.OneShotUpdateOnNewMergeRequestFirstChanceDelayMs,
               Program.Settings.OneShotUpdateOnNewMergeRequestSecondChanceDelayMs});
         }

         if (listViewMergeRequests.SelectedItems.Count == 0 || isSearchMode())
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

         if (String.IsNullOrEmpty(info.Value.InstallerFilePath) || !System.IO.File.Exists(info.Value.InstallerFilePath))
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

            Debug.Assert(!System.IO.File.Exists(destFilePath));

            try
            {
               System.IO.File.Copy(info.Value.InstallerFilePath, destFilePath);
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

         void loadNotifyIconFromFile(string filename)
         {
            try
            {
               notifyIcon.Icon = new Icon(filename);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot create an icon from file \"{0}\"", filename), ex);
            }
         }

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
         string cssEx = String.Format("body div {{ font-size: {0}px; }}", this.Font.Height);

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
            listViewFoundMergeRequests.BackgroundImage = mrHelper.App.Properties.Resources.SnowflakeBg;
            listViewFoundMergeRequests.BackgroundImageTiled = true;
            richTextBoxMergeRequestDescription.BaseStylesheet =
               String.Format("{0}{1}{2}", mrHelper.App.Properties.Resources.NewYear2020_CSS,
                  mrHelper.App.Properties.Resources.Common_CSS, cssEx);
         }
         else
         {
            pictureBox1.BackgroundImage = null;
            pictureBox1.Visible = false;
            pictureBox2.BackgroundImage = null;
            pictureBox2.Visible = false;
            listViewMergeRequests.BackgroundImage = null;
            listViewFoundMergeRequests.BackgroundImage = null;
            richTextBoxMergeRequestDescription.BaseStylesheet =
               String.Format("{0}{1}", mrHelper.App.Properties.Resources.Common_CSS, cssEx);
         }

         Program.Settings.VisualThemeName = theme;
      }

      private void onHostSelected()
      {
         updateProjectsListView();
         disableListView(listViewFoundMergeRequests, true);
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
         return Math.Max(
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxSelectMergeRequest)
          + calcHorzDistance(null, checkBoxLabels)
          + checkBoxLabels.MinimumSize.Width
          + calcHorzDistance(checkBoxLabels, textBoxLabels)
          + 100 /* cannot use textBoxLabels.MinimumSize.Width, see 9b65d7413c */
          + calcHorzDistance(textBoxLabels, buttonReloadList, true)
          + buttonReloadList.Size.Width
          + calcHorzDistance(buttonReloadList, null)
          + calcHorzDistance(groupBoxSelectMergeRequest, null),
            calcHorzDistance(null, groupBoxSelectMergeRequest)
          + calcHorzDistance(null, radioButtonSearchByTitleAndDescription)
          + radioButtonSearchByTitleAndDescription.Width
          + calcHorzDistance(radioButtonSearchByTitleAndDescription, radioButtonSearchByTargetBranch)
          + radioButtonSearchByTargetBranch.Width
          + 10 /* cannot use calcHorzDistance(radioButtonSearchByTargetBranch, null) because its Anchor is Top+Left */
          + calcHorzDistance(groupBoxSelectMergeRequest, null)
          + calcHorzDistance(tabControlMode, null));
      }

      private int getRightPaneMinWidth()
      {
         int calcMinWidthOfControlGroup(IEnumerable<Control> controls, int minGap) =>
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
         if (!_invalidMinSizes || tabControl.SelectedTab != tabPageMR)
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

         // Validate widths
         if (leftPaneMinWidth + rightPaneMinWidth > this.splitContainer1.Width ||
             topRightPaneMinHeight + bottomRightPaneMinHeight > this.splitContainer2.Height)
         {
            Trace.TraceError(String.Format(
               "[MainForm] SplitContainer size conflict. "
             + "SplitContainer1.Width = {0}, leftPaneMinWidth = {1}, rightPaneMinWidth = {2}. "
             + "SplitContainer2.Height = {3}, topRightPaneMinHeight = {4}, bottomRightPaneMinHeight = {5}",
               splitContainer1.Width, leftPaneMinWidth, rightPaneMinWidth,
               splitContainer2.Height, topRightPaneMinHeight, bottomRightPaneMinHeight));
            Debug.Assert(false);
            resetMinimumSizes();
            _invalidMinSizes = false;
            return;
         }

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
         int getControlX(Control control, int index) =>
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

      private void changeProjectEnabledState(string hostname, string projectname, bool state)
      {
         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            hostname, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(projects.ContainsKey(projectname));
         projects[projectname] = state;

         ConfigurationHelper.SetProjectsForHost(hostname,
            Enumerable.Zip(projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y)), Program.Settings);
         updateProjectsListView();
      }

      private void enqueueCheckForUpdates(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished = null)
      {
         bool mergeRequestUpdateFinished = false;
         bool discussionUpdateFinished = false;

         void onSingleUpdateFinished()
         {
            if (mergeRequestUpdateFinished && discussionUpdateFinished)
            {
               onUpdateFinished?.Invoke();
            }
         }

         _mergeRequestCache.CheckForUpdates(mrk, intervals,
            () => { mergeRequestUpdateFinished = true; onSingleUpdateFinished(); });
         _discussionManager.CheckForUpdates(mrk, intervals,
            () => { discussionUpdateFinished = true; onSingleUpdateFinished(); });
      }

      private static void disableSSLVerification()
      {
         if (Program.Settings.DisableSSLVerification)
         {
            try
            {
               GitTools.DisableSSLVerification();
               Program.Settings.DisableSSLVerification = false;
            }
            catch (GitTools.SSLVerificationDisableException ex)
            {
               ExceptionHandlers.Handle("Cannot disable SSL verification", ex);
            }
         }
      }

      private void saveColumnWidths(ListView listView, Action<Dictionary<string, int>> saveProperty)
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (ColumnHeader column in listView.Columns)
         {
            columnWidths[(string)column.Tag] = column.Width;
         }
         saveProperty(columnWidths);
      }

      private bool isReadyToClose()
      {
         if (_commitChainCreator != null && !_commitChainCreator.IsCancelEnabled)
         {
            MessageBox.Show("Current background operation on GitLab branches prevents immediate exit. "
               + "You will be notified when it is done.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Trace.TraceInformation("[MainForm] Cannot exit due to CommitChainCreator");
            _notifyOnCommitChainCancelEnabled = true;
            return false;
         }
         return true;
      }

      private void onCommitChainCancelEnabled(bool enabled)
      {
         if (enabled)
         {
            if (_notifyOnCommitChainCancelEnabled)
            {
               MessageBox.Show("Operation that prevented exit completed.",
                  "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
               Trace.TraceInformation("[MainForm] User notified that operation is completed");
               _notifyOnCommitChainCancelEnabled = false;
            }
         }
         else
         {
            linkLabelAbortGit.Visible = false;
         }
      }

      private void onLoadSingleMergeRequestCommon(int mergeRequestIId)
      {
         string message = mergeRequestIId != 0
            ? String.Format("Loading merge request with IId {0}...", mergeRequestIId)
            : String.Empty;
         labelWorkflowStatus.Text = message;

         enableMergeRequestActions(false);
         enableCommitActions(false, null, default(User));
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false);
         updateTotalTime();
         disableComboBox(comboBoxLatestCommit, String.Empty);
         disableComboBox(comboBoxEarliestCommit, String.Empty);

         if (mergeRequestIId != 0)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] Loading merge request with IId {0} IsSearchMode={1}",
            mergeRequestIId.ToString(), isSearchMode().ToString()));
      }

      private void onFailedLoadSingleMergeRequestCommon()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format(
            "[MainForm] Failed to load merge request IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void onSingleMergeRequestLoadedCommon(string hostname, string projectname, MergeRequest mergeRequest)
      {
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableMergeRequestActions(true);
         FullMergeRequestKey fmk = new FullMergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            MergeRequest = mergeRequest
         };
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(true, mergeRequest.Title, fmk.ProjectKey, mergeRequest.Author);
         updateTotalTime(new MergeRequestKey
            {
               ProjectKey = fmk.ProjectKey,
               IId = fmk.MergeRequest.IId
            }, mergeRequest.Author, hostname);

         labelWorkflowStatus.Text = String.Format("Merge request with IId {0} loaded", mergeRequest.IId);

         Trace.TraceInformation(String.Format(
            "[MainForm] Merge request loaded IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void onLoadComparableEntitiesCommon(ListView listView)
      {
         enableCommitActions(false, null, default(User));
         if (listView.SelectedItems.Count != 0)
         {
            disableComboBox(comboBoxLatestCommit, "Loading...");
            disableComboBox(comboBoxEarliestCommit, "Loading...");
         }
         else
         {
            disableComboBox(comboBoxLatestCommit, String.Empty);
            disableComboBox(comboBoxEarliestCommit, String.Empty);
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] Loading commits IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void onFailedLoadComparableEntitiesCommon()
      {
         disableComboBox(comboBoxLatestCommit, String.Empty);
         disableComboBox(comboBoxEarliestCommit, String.Empty);
         labelWorkflowStatus.Text = "Failed to load commits";

         Trace.TraceInformation(String.Format(
            "[MainForm] Failed to load commits IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void onComparableEntitiesLoadedCommon(MergeRequest mergeRequest,
         System.Collections.IEnumerable entities, ListView listView)
      {
         MergeRequestKey? mrk = getMergeRequestKey(listView);

         IEnumerable<object> objects = entities.Cast<object>();
         int count = objects.Count();
         if (mrk.HasValue && count > 0)
         {
            enableComboBox(comboBoxLatestCommit);
            enableComboBox(comboBoxEarliestCommit);

            addCommitsToComboBoxes(comboBoxLatestCommit, comboBoxEarliestCommit, objects,
               mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);
            selectNotReviewedCommits(listView == listViewFoundMergeRequests,
               comboBoxLatestCommit, comboBoxEarliestCommit, getReviewedCommits(mrk.Value));

            enableCommitActions(true, mergeRequest.Labels, mergeRequest.Author);
         }
         else
         {
            disableComboBox(comboBoxLatestCommit, String.Empty);
            disableComboBox(comboBoxEarliestCommit, String.Empty);
            if (count == 0)
            {
               // Just to be able to switch between versions and commits
               checkBoxShowVersions.Enabled = true;
            }
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", count);

         Trace.TraceInformation(String.Format(
            "[MainForm] Loaded {0} comparable entities IsSearchMode={1}", count, isSearchMode().ToString()));
      }

      private void disableCommonUIControls()
      {
         enableMergeRequestActions(false);
         enableCommitActions(false, null, default(User));
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false);
         updateTotalTime();
         disableComboBox(comboBoxLatestCommit, String.Empty);
         disableComboBox(comboBoxEarliestCommit, String.Empty);
      }

      private static string formatCommitComboboxItem(CommitComboBoxItem item)
      {
         switch (item.Type)
         {
            case EComparableEntityType.Commit:
               {
                  switch (item.Status)
                  {
                     case ECommitComboBoxItemStatus.Normal:
                        return item.Text;
                     case ECommitComboBoxItemStatus.Base:
                        return String.Format("{0} [Base]", item.Text);
                     case ECommitComboBoxItemStatus.Latest:
                        return String.Format("{0} [Latest]", item.Text);
                  }
                  break;
               }

            case EComparableEntityType.Version:
               {
                  string sha = String.IsNullOrWhiteSpace(item.SHA)
                     ? "No SHA" : (item.SHA.Length > 10 ? item.SHA.Substring(0, 10) : item.SHA);
                  string timestamp = item.TimeStamp.HasValue ?
                     item.TimeStamp.Value.ToLocalTime().ToString(Constants.TimeStampFormat) : String.Empty;
                  switch (item.Status)
                  {
                     case ECommitComboBoxItemStatus.Normal:
                        return String.Format("{0} ({1}) created at {2}", item.Text, sha, timestamp);
                     case ECommitComboBoxItemStatus.Base:
                        return String.Format("{0} [Base]", item.Text);
                     case ECommitComboBoxItemStatus.Latest:
                        return String.Format("Latest {0} ({1}) created at {2}", item.Text, sha, timestamp);
                  }
                  break;
               }
         }

         Debug.Assert(false);
         return item.Text;
      }

      private static void setCommitComboboxLabels(ComboBox comboBox, Label labelTimestamp)
      {
         if (comboBox.SelectedItem == null)
         {
            labelTimestamp.Text = "Created at: N/A";
            return;
         }

         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.SelectedItem);
         if (item.Status == ECommitComboBoxItemStatus.Base)
         {
            labelTimestamp.Text = "Created at: N/A";
            return;
         }

         if (item.TimeStamp != null)
         {
            labelTimestamp.Text = String.Format("Created at: {0}",
               item.TimeStamp.Value.ToLocalTime().ToString(Constants.TimeStampFormat));
         }
      }

      private MergeRequestFilterState createMergeRequestFilterState()
      {
         return new MergeRequestFilterState
         {
            Keywords = ConfigurationHelper.GetLabels(Program.Settings),
            Enabled = Program.Settings.CheckedLabelsFilter
         };
      }
   }
}

