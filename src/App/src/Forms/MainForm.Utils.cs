using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client;
using mrHelper.App.Controls;
using mrHelper.Client.Updates;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using System.Drawing;
using mrHelper.App.Helpers;
using mrHelper.CommonControls;
using System.Text;
using System.Text.RegularExpressions;

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
            return new MergeRequestKey(fmk.HostName, fmk.Project.Path_With_Namespace, fmk.MergeRequest.IId);
         }
         Debug.Assert(false);
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

         comboBoxHost.SelectedIndex = -1;
         comboBoxHost.Items.Clear();

         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            HostComboBoxItem hostItem = new HostComboBoxItem
            {
               Host = item.Text,
               AccessToken = item.SubItems[1].Text
            };
            comboBoxHost.Items.Add(hostItem);
         }
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

      private void showTooltipBalloon(string title, string text)
      {
         notifyIcon.BalloonTipTitle = title;
         notifyIcon.BalloonTipText = text;
         notifyIcon.ShowBalloonTip(notifyTooltipTimeout);

         Trace.TraceInformation(String.Format("Tooltip: Title \"{0}\" Text \"{1}\"", title, text)); 
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

         var item = new ListViewItem(getHostWithPrefix(host));
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private string getHostWithPrefix(string host)
      {
         if (!host.StartsWith("http://") && !host.StartsWith("https://"))
         {
            return "https://" + host;
         }
         return host;
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
               if (scheme == _settings.ColorSchemeFileName)
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
               ExceptionHandlers.Handle(ex, "Cannot create a color scheme");
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
         SelectionPreservingComboBox spComboBox = (SelectionPreservingComboBox)comboBox;
         spComboBox.DroppedDown = false;
         spComboBox.SelectedIndex = -1;
         spComboBox.Items.Clear();
         spComboBox.Enabled = false;

         spComboBox.DropDownStyle = ComboBoxStyle.DropDown;
         spComboBox.Text = text;
      }

      private void enableComboBox(ComboBox comboBox)
      {
         SelectionPreservingComboBox spComboBox = (SelectionPreservingComboBox)comboBox;
         spComboBox.Enabled = true;
         spComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
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

      private void updateMergeRequestDetails(MergeRequest? mergeRequest)
      {
         richTextBoxMergeRequestDescription.Text =
            mergeRequest.HasValue ? mergeRequest.Value.Description : String.Empty;
         richTextBoxMergeRequestDescription.Update();
         linkLabelConnectedTo.Text = mergeRequest.HasValue ? mergeRequest.Value.Web_Url : String.Empty;
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
            labelTimeTrackingTrackedLabel.Text = "Tracked Time:";
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrk.HasValue)
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            labelTimeTrackingTrackedTime.Text = String.Empty;
            buttonEditTime.Enabled = false;
         }
         else
         {
            labelTimeTrackingTrackedLabel.Text = "Total Time:";
            labelTimeTrackingTrackedTime.Text = _timeTrackingManager.GetTotalTime(mrk.Value).ToString(@"hh\:mm\:ss");
            buttonEditTime.Enabled = true;
         }
      }

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Visible = enabled;
         buttonAddComment.Enabled = enabled;
         buttonNewDiscussion.Enabled = enabled;
      }

      private void enableCommitActions(bool enabled)
      {
         buttonDiscussions.Enabled = enabled; // not a commit action but depends on git
         buttonDiffTool.Enabled = enabled;
         groupBoxActions.Controls.Cast<Control>().ToList().ForEach((x) => x.Enabled = enabled);
      }

      private void addCommitsToComboBoxes(List<Commit> commits, string baseSha, string targetBranch)
      {
         CommitComboBoxItem latestCommitItem = new CommitComboBoxItem(commits[0])
         {
            IsLatest = true
         };
         comboBoxLeftCommit.Items.Add(latestCommitItem);
         for (int i = 1; i < commits.Count; i++)
         {
            CommitComboBoxItem item = new CommitComboBoxItem(commits[i]);
            if (comboBoxLeftCommit.Items.Cast<CommitComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLeftCommit.Items.Add(item);
            comboBoxRightCommit.Items.Add(item);
         }

         // Add target branch to the right combo-box
         CommitComboBoxItem baseCommitItem = new CommitComboBoxItem(baseSha, targetBranch + " [Base]", null)
         {
            IsBase = true
         };
         comboBoxRightCommit.Items.Add(baseCommitItem);

         selectNotReviewedCommits(out int leftSelectedIndex, out int rightSelectedIndex);
         comboBoxLeftCommit.SelectedIndex = leftSelectedIndex;
         comboBoxRightCommit.SelectedIndex = rightSelectedIndex;
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

      private static string formatMergeRequestForDropdown(MergeRequest mergeRequest)
      {
         return String.Format("{0} [{1}] [{2}]",
            mergeRequest.Title, mergeRequest.Author.Username, String.Join(", ", mergeRequest.Labels.ToArray()));
      }

      /// <summary>
      /// Typically called from another thread
      /// </summary>
      private void updateGitStatusText(string text)
      {
         if (labelGitStatus.InvokeRequired)
         {
            UpdateTextCallback fn = new UpdateTextCallback(updateGitStatusText);
            Invoke(fn, new object [] { text });
         }
         else
         {
            labelGitStatus.Text = text;
         }
      }

      private void notifyOnMergeRequestEvent(string projectName, MergeRequest mergeRequest, string title)
      {
         showTooltipBalloon(title, "\""
            + mergeRequest.Title
            + "\" from "
            + mergeRequest.Author.Name
            + " in project "
            + (projectName == String.Empty ? "N/A" : projectName));
      }

      private void notifyOnMergeRequestUpdates(List<UpdatedMergeRequest> updates)
      {
         List<UpdatedMergeRequest> filtered = FilterMergeRequests(updates, _settings);

         filtered.Where((x) => x.UpdateKind == UpdateKind.New).ToList().ForEach((x) =>
            notifyOnMergeRequestEvent(x.Project.Path_With_Namespace, x.MergeRequest,
               "New merge request"));

         filtered.Where((x) => x.UpdateKind == UpdateKind.CommitsUpdated).ToList().ForEach((x) =>
            notifyOnMergeRequestEvent(x.Project.Path_With_Namespace, x.MergeRequest,
               "New commits in merge request"));
      }

      private static List<string> GetLabels(MergeRequest x) => x.Labels;
      private static List<string> GetLabels(UpdatedMergeRequest x) => GetLabels(x.MergeRequest);

      private static List<string> SplitLabels(string labels)
      {
         List<string> result = new List<string>();
         foreach (var item in labels.Split(','))
         {
            result.Add(item.Trim(' '));
         }
         return result;
      }

      private static List<T> FilterMergeRequests<T>(List<T> mergeRequests, UserDefinedSettings settings)
      {
         if (!settings.CheckedLabelsFilter)
         {
            return mergeRequests;
         }

         List<string> splittedLabels = SplitLabels(settings.LastUsedLabels);
         return mergeRequests.Where(
            (x) =>
         {
            List<string> mrLabels = GetLabels((dynamic)x);
            return splittedLabels.Intersect(mrLabels).Count() != 0;
         }).ToList();
      }

      private static bool IsFilteredMergeRequest(MergeRequest mergeRequest, UserDefinedSettings settings)
      {
         if (!settings.CheckedLabelsFilter)
         {
            return false;
         }

         List<string> splittedLabels = SplitLabels(settings.LastUsedLabels);
         return splittedLabels.Intersect(mergeRequest.Labels).Count() == 0;
      }

      private GitClientFactory getGitClientFactory(string localFolder)
      {
         if (_gitClientFactory == null || _gitClientFactory.ParentFolder != localFolder)
         {
            _gitClientFactory?.Dispose();

            try
            {
               _gitClientFactory = new GitClientFactory(localFolder, _updateManager.GetProjectWatcher(), this);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(ex, String.Format("Cannot create GitClientFactory"));

               try
               {
                  Directory.CreateDirectory(localFolder);
               }
               catch (Exception ex2)
               {
                  string message = String.Format("Cannot create folder \"{0}\"", localFolder);
                  ExceptionHandlers.Handle(ex2, message);
                  MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return null;
               }
            }
         }
         return _gitClientFactory;
      }

      /// <summary>
      /// Make some checks and create a Client
      /// </summary>
      /// <returns>null if could not create a GitClient</returns>
      private GitClient getGitClient(ProjectKey key)
      {
         GitClientFactory factory = getGitClientFactory(_settings.LocalGitFolder);
         if (factory == null)
         {
            return null;
         }

         GitClient client;
         try
         {
            client = factory.GetClient(key.HostName, key.ProjectName);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot create GitClient"));
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
         }

         Debug.Assert(client != null);
         return client;
      }

      private string getInitialHostName()
      {
         // If Last Selected Host is in the list, select it as initial host.
         // Otherwise, select the first host from the list.
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            if (_settings.KnownHosts[iKnownHost] == _initialHostName)
            {
               return _initialHostName;
            }
         }
         return _settings.KnownHosts.Count > 0 ? _settings.KnownHosts[0] : String.Empty;
      }

      private bool isTrackingTime()
      {
         return _timeTracker != null;
      }

      private System.Drawing.Color getMergeRequestColor(MergeRequest mergeRequest)
      {
         foreach (KeyValuePair<string, Color> color in _colorScheme)
         {
            // by author
            {
               string colorName = String.Format("MergeRequests_{{Author:{0}}}", mergeRequest.Author.Username);
               if (colorName == color.Key)
               {
                  return color.Value;
               }
            }

            // by labels
            foreach (string label in mergeRequest.Labels)
            {
               string colorName = String.Format("MergeRequests_{{Label:{0}}}", label);
               if (colorName == color.Key)
               {
                  return color.Value;
               }
            }
         }
         return SystemColors.Window;
      }

      private System.Drawing.Color getCommitComboBoxItemColor(CommitComboBoxItem item)
      {
         Debug.Assert(getMergeRequestKey().HasValue);
         MergeRequestKey mrk = getMergeRequestKey().Value;
         bool wasReviewed = _reviewedCommits.ContainsKey(mrk) && _reviewedCommits[mrk].Contains(item.SHA);
         return wasReviewed || item.IsBase ? SystemColors.Window :
            _colorScheme.GetColorOrDefault("Commits_NotReviewed", SystemColors.Window);
      }

      /// <summary>
      /// Clean up records that correspond to merge requests that have been closed
      /// </summary>
      private void cleanupReviewedCommits(string hostname, string projectname, List<MergeRequest> mergeRequests)
      {
         MergeRequestKey[] toRemove = _reviewedCommits.Keys.Where(
            (x) => x.ProjectKey.HostName == hostname
                && x.ProjectKey.ProjectName == projectname
                && !mergeRequests.Any((y) => x.IId == y.IId)).ToArray();
         foreach (MergeRequestKey key in toRemove)
         {
            _reviewedCommits.Remove(key);
         }
      }

      private void fillListViewMergeRequests(List<FullMergeRequestKey> keys, bool clear)
      {
         if (clear)
         {
            listViewMergeRequests.Groups.Cast<ListViewGroup>().ToList().ForEach(group => group.Items.Clear());
         }

         keys.ForEach(key => addListViewMergeRequestItem(key));
         recalcRowHeightForMergeRequestListView(listViewMergeRequests);
      }

      private void addListViewMergeRequestItem(FullMergeRequestKey fmk)
      {
         if (IsFilteredMergeRequest(fmk.MergeRequest, _settings))
         {
            return;
         }

         ListViewItem item = listViewMergeRequests.Items.Add(new ListViewItem(new string[]
            {
               String.Empty, // Column IId (stub)
               String.Empty, // Column Author (stub)
               String.Empty, // Column Title (stub)
               String.Empty, // Column Labels (stub)
               String.Empty, // Column Jira (stub)
            }, listViewMergeRequests.Groups[fmk.Project.Path_With_Namespace]));
         setListViewItemTag(item, fmk.HostName, fmk.Project, fmk.MergeRequest);
      }

      private void setListViewItemTag(ListViewItem item, string hostname, Project project, MergeRequest mergeRequest)
      {
         item.Tag = new FullMergeRequestKey(hostname, project, mergeRequest);

         string jiraServiceUrl = _serviceManager.GetJiraServiceUrl();
         string jiraTask = getJiraTask(mergeRequest);
         string jiraTaskUrl = jiraServiceUrl != String.Empty && jiraTask != String.Empty ?
            jiraServiceUrl + "/browse/" + jiraTask : String.Empty;

         item.SubItems[0].Tag = new ListViewSubItemInfo(() => mergeRequest.IId.ToString(), () => mergeRequest.Web_Url);
         item.SubItems[1].Tag = new ListViewSubItemInfo(() => mergeRequest.Author.Name,    () => String.Empty);
         item.SubItems[2].Tag = new ListViewSubItemInfo(() => mergeRequest.Title,          () => String.Empty);
         item.SubItems[3].Tag = new ListViewSubItemInfo(() => formatLabels(mergeRequest),  () => String.Empty);
         item.SubItems[4].Tag = new ListViewSubItemInfo(() => jiraTask,                    () => jiraTaskUrl);
      }

      private void recalcRowHeightForMergeRequestListView(ListView listView)
      {
         if (listView.Items.Count == 0)
         {
            return;
         }
         
         int maxLineCount = listView.Items.Cast<ListViewItem>().
            Select((x) => formatLabels(((FullMergeRequestKey)(x.Tag)).MergeRequest).Count((y) => y == '\n')).Max() + 1;
         setListViewRowHeight(listView, listView.Font.Height * maxLineCount + 2);
      }

      private static string formatLabels(MergeRequest mergeRequest)
      {
         mergeRequest.Labels.Sort();

         var query = mergeRequest.Labels.GroupBy(
            (label) => label.StartsWith("@") && label.IndexOf('-') != -1 ? label.Substring(0, label.IndexOf('-')) : label,
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
         try
         {
            Process.Start(text);
         }
         catch (Exception ex) // see Process.Start exception list
         {
            ExceptionHandlers.Handle(ex, "Cannot open URL");
            MessageBox.Show("Cannot open URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}

