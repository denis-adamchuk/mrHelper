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
using mrHelper.StorageSupport;
using static mrHelper.App.Controls.MergeRequestListView;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Session;
using mrHelper.Client.TimeTracking;

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

      private MergeRequest getMergeRequest(ListView proposedListView)
      {
         ListView currentListView = isSearchMode() ? listViewFoundMergeRequests : listViewMergeRequests;
         ListView listView = proposedListView ?? currentListView;
         if (listView.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listView.SelectedItems[0].Tag;
            return fmk.MergeRequest;
         }
         return null;
      }

      private MergeRequestKey? getMergeRequestKey(ListView proposedListView)
      {
         ListView currentListView = isSearchMode() ? listViewFoundMergeRequests : listViewMergeRequests;
         ListView listView = proposedListView ?? currentListView;
         if (listView.SelectedItems.Count > 0)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listView.SelectedItems[0].Tag;
            return new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         }
         return null;
      }

      private ISession getSession(bool live)
      {
         return live ? _liveSession : _searchSession;
      }

      /// <summary>
      /// Populates host list with list of known hosts from Settings
      /// </summary>
      private void updateHostsDropdownList()
      {
         updateEnablementsOfWorkflowSelectors();

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
               HostComboBoxItem hostItem = new HostComboBoxItem(item.Text, item.SubItems[1].Text);
               comboBoxHost.Items.Add(hostItem);
            }
         }
      }

      private void updateEnablementsOfWorkflowSelectors()
      {
         if (listViewKnownHosts.Items.Count > 0)
         {
            radioButtonSelectByUsernames.Enabled = true;
            radioButtonSelectByProjects.Enabled = true;
            listViewUsers.Enabled = radioButtonSelectByUsernames.Checked;
            listViewProjects.Enabled = radioButtonSelectByProjects.Checked;
            buttonEditProjects.Enabled = true;
            buttonEditUsers.Enabled = true;
         }
         else
         {
            radioButtonSelectByUsernames.Enabled = false;
            radioButtonSelectByProjects.Enabled = false;
            listViewUsers.Enabled = false;
            listViewProjects.Enabled = false;
            buttonEditProjects.Enabled = false;
            buttonEditUsers.Enabled = false;
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

         HostComboBoxItem initialSelectedItem = comboBoxHost.Items.Cast<HostComboBoxItem>().SingleOrDefault(
            x => x.Host == getInitialHostName()); // `null` if not found

         HostComboBoxItem defaultSelectedItem = (HostComboBoxItem)comboBoxHost.Items[comboBoxHost.Items.Count - 1];
         switch (preferred)
         {
            case PreferredSelection.Initial:
               if (initialSelectedItem != null && !String.IsNullOrEmpty(initialSelectedItem.Host))
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

         updateProjectsListView();
         updateUsersListView();
      }

      private void createListViewGroupForProject(ListView listView, ProjectKey projectKey, bool sortNeeded)
      {
         ListViewGroup group = new ListViewGroup(projectKey.ProjectName, projectKey.ProjectName);
         group.Tag = projectKey;
         if (!sortNeeded)
         {
            // user defines how to sort group here
            listView.Groups.Add(group);
            return;
         }

         // sort groups alphabetically
         int indexToInsert = listView.Groups.Count;
         for (int iGroup = 0; iGroup < listView.Groups.Count; ++iGroup)
         {
            if (projectKey.ProjectName.CompareTo(listView.Groups[iGroup].Header) < 0)
            {
               indexToInsert = iGroup;
               break;
            }
         }
         listView.Groups.Insert(indexToInsert, group);
      }

      private bool selectMergeRequest(ListView listView, MergeRequestKey? mrk, bool exact)
      {
         if (!mrk.HasValue)
         {
            if (listView.Items.Count < 1)
            {
               return false;
            }

            listView.Items[0].Selected = true;
            return true;
         }

         foreach (ListViewItem item in listView.Items)
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
         foreach (ListViewGroup group in listView.Groups)
         {
            if (mrk.Value.ProjectKey.MatchProject(group.Name) && group.Items.Count > 0)
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

      private HashSet<string> getReviewedRevisions(MergeRequestKey mrk)
      {
         if (!_reviewedRevisions.ContainsKey(mrk))
         {
            _reviewedRevisions[mrk] = new HashSet<string>();
         }
         return _reviewedRevisions[mrk];
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
            _iconScheme = JsonUtils.LoadFromFile<Dictionary<string, object>>(
               Constants.IconSchemeFileName).ToDictionary(
                  item => item.Key,
                  item => item.Value.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load icon scheme", ex);
         }
      }

      private void initializeBadgeScheme()
      {
         if (!System.IO.File.Exists(Constants.BadgeSchemeFileName))
         {
            return;
         }

         try
         {
            _badgeScheme = JsonUtils.LoadFromFile<Dictionary<string, object>>(
               Constants.BadgeSchemeFileName).ToDictionary(
                  item => item.Key,
                  item => item.Value.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load badge scheme", ex);
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

      private void updateStorageDependentControlState(MergeRequestKey? mrk)
      {
         bool isEnabled = mrk.HasValue
            && !_mergeRequestsUpdatingByUserRequest.Contains(mrk.Value)
            &&  _mergeRequestsUpdatingByUserRequest.Count() < Constants.MaxMergeRequestStorageUpdatesInParallel;
         buttonDiscussions.Enabled = isEnabled;
         updateDiffToolButtonState(isEnabled, mrk);
      }

      private void onStorageUpdateStateChange()
      {
         updateAbortGitCloneButtonState();
      }

      private void updateAbortGitCloneButtonState()
      {
         ProjectKey? projectKey = getMergeRequestKey(null)?.ProjectKey ?? null;
         ILocalCommitStorage repo = projectKey.HasValue ? getCommitStorage(projectKey.Value, false) : null;

         bool enabled = repo?.Updater?.CanBeStopped() ?? false;
         linkLabelAbortGitClone.Visible = enabled;
      }

      private void onStorageUpdateProgressChange(string text, MergeRequestKey mrk)
      {
         if (labelStorageStatus.InvokeRequired)
         {
            Invoke(new Action<string, MergeRequestKey>(onStorageUpdateProgressChange), new object [] { text, mrk });
         }
         else
         {
            _latestStorageUpdateStatus[mrk] = text;

            MergeRequestKey? currentMRK = getMergeRequestKey(null);
            if (currentMRK.HasValue && currentMRK.Value.Equals(mrk))
            {
               updateStorageStatusText(text, mrk);
            }
         }
      }

      private void updateStorageStatusText(string text, MergeRequestKey? mrk)
      {
         string message = String.IsNullOrEmpty(text) || !mrk.HasValue
            ? String.Empty
            : String.Format("{0} #{1}: {2}", mrk.Value.ProjectKey.ProjectName, mrk.Value.IId.ToString(), text);
         labelStorageStatus.Text = message;
      }

      private string getStorageSummaryUpdateInformation()
      {
         if (!_mergeRequestsUpdatingByUserRequest.Any())
         {
            return String.Empty;
         }

         var mergeRequestGroups = _mergeRequestsUpdatingByUserRequest
            .Distinct()
            .GroupBy(
               group => group.ProjectKey,
               group => group,
               (group, groupedMergeRequests) => new
               {
                  Project = group.ProjectName,
                  MergeRequests = groupedMergeRequests
               });

         List<string> storages = new List<string>();
         foreach (var group in mergeRequestGroups)
         {
            IEnumerable<string> mergeRequestIds = group.MergeRequests.Select(x => String.Format("#{0}", x.IId));
            string mergeRequestIdsString = String.Join(", ", mergeRequestIds);
            string storage = String.Format("{0} ({1})", group.Project, mergeRequestIdsString);
            storages.Add(storage);
         }

         return String.Format("Updating storage{0}: {1}...",
            storages.Count() > 1 ? "s" : "", String.Join(", ", storages));
      }

      private void enableMergeRequestFilterControls(bool enabled)
      {
         checkBoxDisplayFilter.Enabled = enabled;
         textBoxDisplayFilter.Enabled = enabled;
      }

      private void enableMergeRequestSearchControls(bool enabled)
      {
         textBoxSearch.Enabled = enabled;
      }

      private void updateVisibleMergeRequestsEnablements()
      {
         if (listViewMergeRequests.Items.Count > 0 || Program.Settings.DisplayFilterEnabled)
         {
            enableMergeRequestFilterControls(true);
            enableListView(listViewMergeRequests);
         }
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

         string url = fmk.HasValue ? fmk.Value.MergeRequest.Web_Url : String.Empty;
         linkLabelConnectedTo.Text = url;
         toolTip.SetToolTip(linkLabelConnectedTo, url);
      }

      private void updateTimeTrackingMergeRequestDetails(bool enabled, string title, ProjectKey projectKey, User author)
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

      private void updateTotalTime(MergeRequestKey? mrk, User author, string hostname, ITotalTimeCache totalTimeCache)
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
            TimeSpan? span = getTotalTime(mrk.Value, totalTimeCache);
            labelTimeTrackingTrackedLabel.Text = String.Format("Total Time: {0}",
               convertTotalTimeToText(span, true));
            buttonEditTime.Enabled = span.HasValue;
         }

         // Update total time column in the table
         listViewMergeRequests.Invalidate();
         labelTimeTrackingTrackedLabel.Refresh();
      }

      private TimeSpan? getTotalTime(MergeRequestKey? mrk, ITotalTimeCache totalTimeCache)
      {
         if (!mrk.HasValue)
         {
            return null;
         }

         return totalTimeCache?.GetTotalTime(mrk.Value);
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

         return isTimeTrackingAllowed ? Constants.NotStartedTimeTrackingText : Constants.NotAllowedTimeTrackingText;
      }

      private bool isTimeTrackingAllowed(User mergeRequestAuthor, string hostname)
      {
         if (mergeRequestAuthor == null || String.IsNullOrWhiteSpace(hostname))
         {
            return true;
         }

         return !_currentUser.ContainsKey(hostname)
              || _currentUser[hostname].Id != mergeRequestAuthor.Id
              || Program.Settings.AllowAuthorToTrackTime;
      }

      private string getDiscussionCount(MergeRequestKey mrk)
      {
         ISession session = getSession(true /* supported in Live only */);
         if (session?.DiscussionCache == null)
         {
            return "N/A";
         }

         DiscussionCount dc = session.DiscussionCache.GetDiscussionCount(mrk);
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

      private string getSize(MergeRequestKey mrk)
      {
         if (_diffStatProvider == null)
         {
            return String.Empty;
         }

         DiffStatistic? diffStatistic = _diffStatProvider.GetStatistic(mrk, out string errMsg);
         return diffStatistic?.ToString() ?? errMsg;
      }

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Enabled = enabled;
         buttonAddComment.Enabled = enabled;
         buttonNewDiscussion.Enabled = enabled;
      }

      private void updateDiffToolButtonState(bool isEnabled, MergeRequestKey? mrk)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out RevisionType? type);
         switch (selected.Count())
         {
            case 1:
               buttonDiffTool.Enabled = isEnabled;
               buttonDiffTool.Text = "Diff to Base";
               string targetBranch = getMergeRequest(null)?.Target_Branch;
               if (targetBranch != null)
               {
                  this.toolTip.SetToolTip(this.buttonDiffTool, String.Format(
                     "Launch diff tool to compare selected revision to {0}", targetBranch));
               }
               break;

            case 2:
               buttonDiffTool.Enabled = isEnabled;
               buttonDiffTool.Text = "Diff Tool";
               this.toolTip.SetToolTip(this.buttonDiffTool, "Launch diff tool to compare selected revisions");
               break;

            case 0:
            default:
               buttonDiffTool.Enabled = false;
               buttonDiffTool.Text = "Diff Tool";
               break;
         }
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
            foreach (Control control in groupBoxActions.Controls) control.Enabled = false;
            return;
         }

         if (author == null)
         {
            Debug.Assert(false);
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

      private ILocalCommitStorageFactory gitCommitStorageFactory()
      {
         if (_storageFactory == null)
         {
            try
            {
               _storageFactory = new LocalCommitStorageFactory(this, getSession(true),
                  Program.Settings.LocalGitFolder, Program.Settings.UseShallowClone, Program.Settings.RevisionsToKeep);
               _storageFactory.GitRepositoryCloned += onGitRepositoryCloned;
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle("Cannot create LocalGitCommitStorageFactory", ex);
            }
         }
         return _storageFactory;
      }

      private void disposeLocalGitRepositoryFactory()
      {
         if (_storageFactory != null)
         {
            _storageFactory.GitRepositoryCloned -= onGitRepositoryCloned;
            _storageFactory.Dispose();
            _storageFactory = null;
         }
      }

      private void onGitRepositoryCloned(ILocalCommitStorage storage)
      {
         requestCommitStorageUpdate(storage.ProjectKey);
      }

      /// <summary>
      /// Make some checks and create a commit storage
      /// </summary>
      /// <returns>null if could not create a repository</returns>
      private ILocalCommitStorage getCommitStorage(ProjectKey projectKey, bool showMessageBoxOnError)
      {
         ILocalCommitStorageFactory factory = gitCommitStorageFactory();
         if (factory == null)
         {
            return null;
         }

         var type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         ILocalCommitStorage repo = factory.GetStorage(projectKey, type);
         if (repo == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format(
               "Cannot obtain disk storage for project {0} in \"{1}\"",
               projectKey.ProjectName, Program.Settings.LocalGitFolder),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
         ISession session = getSession(true /* supported in Live only */);
         DiscussionCount dc = session?.DiscussionCache?.GetDiscussionCount(
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

      /// <summary>
      /// Clean up records that correspond to merge requests that have been closed
      /// </summary>
      private void cleanupReviewedRevisions(string hostname)
      {
         if (_liveSession?.MergeRequestCache == null)
         {
            return;
         }

         IEnumerable<ProjectKey> projectKeys = _liveSession.MergeRequestCache.GetProjects();

         // gather all MR from projects that no longer in use
         IEnumerable<MergeRequestKey> toRemove1 = _reviewedRevisions.Keys
            .Where(x => !projectKeys.Any(y => y.Equals(x.ProjectKey)));

         // gather all closed MR from existing projects
         IEnumerable<MergeRequestKey> toRemove2 = _reviewedRevisions.Keys
            .Where(x => projectKeys.Any(y => y.Equals(x.ProjectKey))
               && !_liveSession.MergeRequestCache.GetMergeRequests(x.ProjectKey).Any(y => y.IId == x.IId));

         // leave only MR from the passed project
         IEnumerable<MergeRequestKey> toRemove =
            toRemove1
               .Concat(toRemove2)
               .Where(x => x.ProjectKey.HostName == hostname)
               .ToArray();

         foreach (MergeRequestKey key in toRemove)
         {
            _reviewedRevisions.Remove(key);
         }
      }

      /// <summary>
      /// Clean up records that correspond to merge requests that have been closed
      /// </summary>
      private void cleanupReviewedRevisions(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequestKey> toRemove = _reviewedRevisions.Keys.Where(x => x.Equals(mrk));
         if (!toRemove.Any())
         {
            return;
         }

         _reviewedRevisions.Remove(toRemove.First());
      }


      private void updateVisibleMergeRequests()
      {
         ISession session = getSession(true /* supported in Live only */);
         IMergeRequestCache mergeRequestCache = session?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         listViewMergeRequests.BeginUpdate();

         IEnumerable<ProjectKey> projectKeys;
         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            projectKeys = listViewMergeRequests.Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);
         }
         else
         {
            // Add missing project groups
            IEnumerable<ProjectKey> allProjects = mergeRequestCache.GetProjects();
            foreach (ProjectKey projectKey in allProjects)
            {
               if (!listViewMergeRequests.Groups.Cast<ListViewGroup>().Any(x => projectKey.Equals((ProjectKey)(x.Tag))))
               {
                  createListViewGroupForProject(listViewMergeRequests, projectKey, true);
               }
            }

            // Remove deleted project groups
            projectKeys = listViewMergeRequests.Groups.Cast<ListViewGroup>().Select(x => (ProjectKey)x.Tag);
            for (int index = listViewMergeRequests.Groups.Count - 1; index >= 0; --index)
            {
               ListViewGroup group = listViewMergeRequests.Groups[index];
               if (!allProjects.Any(x => x.Equals((ProjectKey)group.Tag)))
               {
                  listViewMergeRequests.Groups.Remove(group);
               }
            }
         }

         // Add missing merge requests and update existing ones
         foreach (ProjectKey projectKey in projectKeys)
         {
            foreach (MergeRequest mergeRequest in mergeRequestCache.GetMergeRequests(projectKey))
            {
               FullMergeRequestKey fmk = new FullMergeRequestKey(projectKey, mergeRequest);
               ListViewItem item = listViewMergeRequests.Items.Cast<ListViewItem>().FirstOrDefault(
                  x => ((FullMergeRequestKey)x.Tag).Equals(fmk)); // item=`null` if not found
               if (item == null)
               {
                  item = createListViewMergeRequestItem(listViewMergeRequests, fmk);
                  listViewMergeRequests.Items.Add(item);
               }
               else
               {
                  item.Tag = fmk;
               }
               setListViewSubItemsTags(item, fmk);
            }
         }

         // Remove deleted merge requests and hide filtered ones
         for (int index = listViewMergeRequests.Items.Count - 1; index >= 0; --index)
         {
            FullMergeRequestKey fmk = (FullMergeRequestKey)listViewMergeRequests.Items[index].Tag;
            if (!mergeRequestCache.GetMergeRequests(fmk.ProjectKey).Any(x => x.IId == fmk.MergeRequest.IId)
             || !_mergeRequestFilter.DoesMatchFilter(fmk.MergeRequest))
            {
               listViewMergeRequests.Items.RemoveAt(index);
            }
         }

         recalcRowHeightForMergeRequestListView(listViewMergeRequests);

         listViewMergeRequests.EndUpdate();

         updateVisibleMergeRequestsEnablements();
         updateTrayIcon();
         updateTaskbarIcon();
      }

      private ListViewItem createListViewMergeRequestItem(ListView listView, FullMergeRequestKey fmk)
      {
         ListViewGroup group = listView.Groups[fmk.ProjectKey.ProjectName];
         string[] subitems = Enumerable.Repeat(String.Empty, listView.Columns.Count).ToArray();
         ListViewItem item = new ListViewItem(subitems, group);
         item.Tag = fmk;
         return item;
      }

      private void setListViewSubItemsTags(ListViewItem item, FullMergeRequestKey fmk)
      {
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

         ISession session = item.ListView == listViewMergeRequests ? _liveSession : _searchSession;
         string getTotalTimeText(MergeRequestKey key)
         {
            ITotalTimeCache totalTimeCache = session?.TotalTimeCache;
            return convertTotalTimeToText(getTotalTime(key, totalTimeCache),
               isTimeTrackingAllowed(mr.Author, projectKey.HostName));
         }

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

         setSubItemTag("IId",          new ListViewSubItemInfo(x => mr.IId.ToString(),         () => mr.Web_Url));
         setSubItemTag("Author",       new ListViewSubItemInfo(x => author,                    () => String.Empty));
         setSubItemTag("Title",        new ListViewSubItemInfo(x => mr.Title,                  () => String.Empty));
         setSubItemTag("Labels",       new ListViewSubItemInfo(x => labels[x],                 () => String.Empty));
         setSubItemTag("Size",         new ListViewSubItemInfo(x => getSize(mrk),              () => String.Empty));
         setSubItemTag("Jira",         new ListViewSubItemInfo(x => jiraTask,                  () => jiraTaskUrl));
         setSubItemTag("TotalTime",    new ListViewSubItemInfo(x => getTotalTimeText(mrk),     () => String.Empty));
         setSubItemTag("SourceBranch", new ListViewSubItemInfo(x => mr.Source_Branch,          () => String.Empty));
         setSubItemTag("TargetBranch", new ListViewSubItemInfo(x => mr.Target_Branch,          () => String.Empty));
         setSubItemTag("State",        new ListViewSubItemInfo(x => mr.State,                  () => String.Empty));
         setSubItemTag("Resolved",     new ListViewSubItemInfo(x => getDiscussionCount(mrk),   () => String.Empty));
      }

      private void recalcRowHeightForMergeRequestListView(ListView listView)
      {
         if (listView.Items.Count == 0)
         {
            return;
         }

         int maxLineCountInLabels = listView.Items.Cast<ListViewItem>()
            .Select(x =>formatLabels((FullMergeRequestKey)(x.Tag), false)
               .Count(y => y == '\n'))
            .Max() + 1;
         int maxLineCountInAuthor = 2;
         int maxLineCount = Math.Max(maxLineCountInLabels, maxLineCountInAuthor);
         setListViewRowHeight(listView, maxLineCount);
      }

      private string formatLabels(FullMergeRequestKey fmk, bool tooltip)
      {
         User currentUser = _currentUser.ContainsKey(fmk.ProjectKey.HostName)
            ? _currentUser[fmk.ProjectKey.HostName]
            : null;

         IEnumerable<string> unimportantSuffices = Program.ServiceManager.GetUnimportantSuffices();

         int getPriority(IEnumerable<string> labels)
         {
            Debug.Assert(labels.Any());
            if (Client.Common.Helpers.IsUserMentioned(labels.First(), currentUser))
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
         int take = tooltip ? query.Count() : Constants.MaxLabelRows - 1;

         foreach (var x in
            query
            .OrderBy(x => x.Priority)
            .Take(take))
         {
            stringBuilder.Append(joinLabels(x.Labels));
         }

         if (!tooltip)
         {
            if (query.Count() > Constants.MaxLabelRows)
            {
               stringBuilder.Append(Constants.MoreLabelsHint);
            }
            else if (query.Count() == Constants.MaxLabelRows)
            {
               stringBuilder.Append(joinLabels(query.OrderBy(x => x.Priority).Last().Labels));
            }
         }

         return stringBuilder.ToString().TrimEnd('\n');
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
         if (e.New || e.Commits)
         {
            requestCommitStorageUpdate(e.FullMergeRequestKey.ProjectKey);
         }

         MergeRequestKey mrk = new MergeRequestKey(
            e.FullMergeRequestKey.ProjectKey, e.FullMergeRequestKey.MergeRequest.IId);

         if (e.Closed)
         {
            cleanupReviewedRevisions(mrk);
         }

         updateVisibleMergeRequests();

         if (e.New)
         {
            requestUpdates(mrk, new[] {
               Program.Settings.OneShotUpdateOnNewMergeRequestFirstChanceDelayMs,
               Program.Settings.OneShotUpdateOnNewMergeRequestSecondChanceDelayMs});
         }

         if (listViewMergeRequests.SelectedItems.Count == 0 || isSearchMode())
         {
            return;
         }

         ListViewItem selected = listViewMergeRequests.SelectedItems[0];
         FullMergeRequestKey fmk = (FullMergeRequestKey)selected.Tag;

         if (fmk.Equals(e.FullMergeRequestKey))
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

         LatestVersionInformation info = Program.ServiceManager.GetLatestVersionInfo();
         if (info == null || String.IsNullOrWhiteSpace(info.VersionNumber))
         {
            return;
         }

         // TODO Extract all this stuff into ApplicationUpdater entity
         try
         {
            System.Version currentVersion = new System.Version(Application.ProductVersion);
            System.Version latestVersion = new System.Version(info.VersionNumber);
            if (currentVersion >= latestVersion)
            {
               return;
            }

            if (!String.IsNullOrWhiteSpace(_newVersionNumber))
            {
               System.Version cachedLatestVersion = new System.Version(_newVersionNumber);
               if (cachedLatestVersion >= latestVersion)
               {
                  return;
               }
            }
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Wrong version number", ex);
            return;
         }

         Trace.TraceInformation(String.Format("[CheckForUpdates] New version {0} is found", info.VersionNumber));

         if (String.IsNullOrEmpty(info.InstallerFilePath) || !System.IO.File.Exists(info.InstallerFilePath))
         {
            Trace.TraceWarning(String.Format("[CheckForUpdates] Installer cannot be found at \"{0}\"",
               info.InstallerFilePath));
            return;
         }

         Task.Run(
            () =>
         {
            if (info == null)
            {
               return;
            }

            string filename = Path.GetFileName(info.InstallerFilePath);
            string tempFolder = Environment.GetEnvironmentVariable("TEMP");
            string destFilePath = Path.Combine(tempFolder, filename);

            Debug.Assert(!System.IO.File.Exists(destFilePath));

            try
            {
               System.IO.File.Copy(info.InstallerFilePath, destFilePath);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle("Cannot download a new version", ex);
               return;
            }

            _newVersionFilePath = destFilePath;
            _newVersionNumber = info.VersionNumber;
            BeginInvoke(new Action(() =>
            {
               linkLabelNewVersion.Visible = true;
               updateCaption();
            }));
         });
      }

      private void cleanUpTempFolder(string template)
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");
         foreach (string f in System.IO.Directory.EnumerateFiles(tempFolder, template))
         {
            try
            {
               System.IO.File.Delete(f);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot delete file \"{0}\"", f), ex);
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
         if (_iconScheme == null || !_iconScheme.Any())
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
            }
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

      private void updateTaskbarIcon()
      {
         CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(null);
         if (_badgeScheme == null || !_badgeScheme.Any())
         {
            return;
         }

         if (isTrackingTime())
         {
            if (_badgeScheme.ContainsKey("Badge_Tracking"))
            {
               CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(
                  Color.FromName(_badgeScheme["Badge_Tracking"]));
            }
            return;
         }

         foreach (KeyValuePair<string, string> nameToFilename in _badgeScheme)
         {
            string resolved = _expressionResolver.Resolve(nameToFilename.Key);
            if (listViewMergeRequests.Items
               .Cast<ListViewItem>()
               .Select(x => x.Tag)
               .Cast<FullMergeRequestKey>()
               .Select(x => x.MergeRequest)
               .Any(x => x.Labels.Any(y => StringUtils.DoesMatchPattern(resolved, "Badge_{{Label:{0}}}", y))))
            {
               CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(
                  Color.FromName(nameToFilename.Value));
               break;
            }
         }
      }

      private void applyTheme(string theme)
      {
         string cssEx = String.Format("body div {{ font-size: {0}px; }}",
            CommonControls.Tools.WinFormsHelpers.GetFontSizeInPixels(richTextBoxMergeRequestDescription));

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

      private void updateUsersListView()
      {
         listViewUsers.Items.Clear();

         foreach (string label in ConfigurationHelper.GetEnabledUsers(getHostName(), Program.Settings))
         {
            listViewUsers.Items.Add(label);
         }
      }

      private void updateProjectsListView()
      {
         listViewProjects.Items.Clear();

         foreach (string project in
            ConfigurationHelper.GetEnabledProjects(getHostName(), Program.Settings)
            .Select(x => x.Path_With_Namespace))
         {
            listViewProjects.Items.Add(project);
         }
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

      private void resetMergeRequestTabMinimumSizes()
      {
         int defaultSplitContainerPanelMinSize = 25;
         splitContainer1.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer1.Panel2MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel2MinSize = defaultSplitContainerPanelMinSize;

         this.MinimumSize = new System.Drawing.Size(0, 0);

         _initializedMinimumSizes = false;
      }

      private bool _initializedMinimumSizes = true;

      private int getLeftPaneMinWidth()
      {
         return Math.Max(
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxSelectMergeRequest)
          + calcHorzDistance(null, checkBoxDisplayFilter)
          + checkBoxDisplayFilter.MinimumSize.Width
          + calcHorzDistance(checkBoxDisplayFilter, textBoxDisplayFilter)
          + 100 /* cannot use textBoxLabels.MinimumSize.Width, see 9b65d7413c */
          + calcHorzDistance(textBoxDisplayFilter, buttonReloadList, true)
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
              calcVertDistance(null, groupBoxSelectRevisions)
            + groupBoxSelectRevisions.Height
            + calcVertDistance(groupBoxSelectRevisions, groupBoxReview)
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

      private void initializeMergeRequestTabMinimumSizes()
      {
         if (_initializedMinimumSizes || tabControl.SelectedTab != tabPageMR)
         {
            return;
         }

         if (Program.Settings.DisableSplitterRestrictions)
         {
            _initializedMinimumSizes = true;
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
            resetMergeRequestTabMinimumSizes();
            _initializedMinimumSizes = true;
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

         _initializedMinimumSizes = true;
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
                        && textBoxStorageFolder.Text.Length > 0
                        && Directory.Exists(textBoxStorageFolder.Text);
         if (configured)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else
         {
            tabControl.SelectedTab = tabPageSettings;
         }
      }

      private void changeProjectEnabledState(ProjectKey projectKey, bool state)
      {
         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            projectKey.HostName, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(projects.ContainsKey(projectKey.ProjectName));
         projects[projectKey.ProjectName] = state;

         ConfigurationHelper.SetProjectsForHost(projectKey.HostName,
            Enumerable.Zip(projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y)), Program.Settings);
         updateProjectsListView();
      }

      private void requestUpdates(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished = null)
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

         ISession session = getSession(true /* supported in Live only */);
         session?.MergeRequestCache?.RequestUpdate(mrk, intervals,
            () => { mergeRequestUpdateFinished = true; onSingleUpdateFinished(); });
         session?.DiscussionCache?.RequestUpdate(mrk, intervals,
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

      private void onSingleMergeRequestLoadedCommon(FullMergeRequestKey fmk, ISession session)
      {
         Debug.Assert(fmk.MergeRequest != null);

         enableMergeRequestActions(true);
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(
            true, fmk.MergeRequest.Title, fmk.ProjectKey, fmk.MergeRequest.Author);
         updateTotalTime(new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId),
            fmk.MergeRequest.Author, fmk.ProjectKey.HostName, session.TotalTimeCache);
         updateAbortGitCloneButtonState();

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         string status = _latestStorageUpdateStatus.TryGetValue(mrk, out string value) ? value : String.Empty;
         updateStorageStatusText(status, mrk);

         Debug.WriteLine(String.Format(
            "[MainForm] Merge request loaded IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void onComparableEntitiesLoadedCommon(GitLabSharp.Entities.Version latestVersion,
         MergeRequest mergeRequest, IEnumerable<Commit> commits, IEnumerable<GitLabSharp.Entities.Version> versions,
         ListView listView)
      {
         MergeRequestKey? mrk = getMergeRequestKey(listView);
         bool hasObjects = commits.Any() || versions.Any();
         if (mrk.HasValue && hasObjects)
         {
            RevisionBrowserModelData data = new RevisionBrowserModelData(latestVersion?.Base_Commit_SHA,
               commits, versions, getReviewedRevisions(mrk.Value));
            revisionBrowser.SetData(data, ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
            updateStorageDependentControlState(mrk);
            enableCustomActions(true, mergeRequest.Labels, mergeRequest.Author);
         }
         else
         {
            revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
         }

         Debug.WriteLine(String.Format(
            "[MainForm] Loaded comparable entities IsSearchMode={0}", isSearchMode().ToString()));
      }

      private void disableCommonUIControls()
      {
         enableMergeRequestActions(false);
         enableCustomActions(false, null, null);
         updateStorageDependentControlState(null);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false, null, default(ProjectKey), null);
         updateTotalTime(null, null, null, null);
         revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
      }

      private MergeRequestFilterState createMergeRequestFilterState()
      {
         return new MergeRequestFilterState
         (
            ConfigurationHelper.GetDisplayFilterKeywords(Program.Settings),
            Program.Settings.DisplayFilterEnabled
         );
      }

      private void applyAutostartSetting(bool enabled)
      {
         if (_runningAsUwp)
         {
            return;
         }

         string command = String.Format("{0} -m", Application.ExecutablePath);
         AutoStartHelper.ApplyAutostartSetting(enabled, "mrHelper", command);
      }

      private void onUpdating()
      {
         buttonReloadList.Text = "Updating...";
         buttonReloadList.Enabled = false;
      }

      private void onUpdated(string oldButtonText)
      {
         buttonReloadList.Text = oldButtonText;
         buttonReloadList.Enabled = true;
      }

      private void getShaForDiffTool(out string baseSha, out string left, out string right,
         out IEnumerable<string> included, out RevisionType? type)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out type);
         switch (selected.Count())
         {
            case 0:
               baseSha = String.Empty;
               left = String.Empty;
               right = String.Empty;
               included = new List<string>();
               break;

            case 1:
               baseSha = revisionBrowser.GetBaseCommitSha();
               left = baseSha;
               right = selected[0];
               included = revisionBrowser.GetIncludedSha();
               break;

            case 2:
               baseSha = revisionBrowser.GetBaseCommitSha();
               left = selected[0];
               right = selected[1];
               included = revisionBrowser.GetIncludedSha();
               break;

            default:
               Debug.Assert(false);
               baseSha = String.Empty;
               left = String.Empty;
               right = String.Empty;
               included = new List<string>();
               break;
         }
      }
   }
}

