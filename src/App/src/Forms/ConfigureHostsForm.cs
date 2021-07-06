using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Forms
{
   internal partial class ConfigureHostsForm : CustomFontForm, IHostProperties
   {
      public ConfigureHostsForm()
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         applyFont(Program.Settings.MainWindowFontSizeName);

         linkLabelCreateAccessToken.Text = String.Empty;
         linkLabelCreateAccessToken.SetLinkLabelClicked(UrlHelper.OpenBrowser);
      }

      internal bool Changed { get; private set; }

      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         e.Cancel = isChecking();
      }

      async private void configureHostsForm_Load(object sender, System.EventArgs e)
      {
         loadKnownHosts();

         onStartChecking(CheckingMode.LoadingUsers);
         try
         {
            foreach (KeyValuePair<string, string> kv in _hosts)
            {
               await loadUsersForHost(kv.Key);
               loadProjectsForHost(kv.Key);
            }
         }
         finally
         {
            onEndChecking();
         }

         updateHostsListView();
      }

      private void listViewKnownHosts_SelectedIndexChanged(object sender, EventArgs e)
      {
         onKnownHostSelectionChanged();
      }

      private void buttonOK_Click(object sender, EventArgs e)
      {
         Changed = false;
         Changed |= saveKnownHosts();
         Changed |= saveProjects();
         Changed |= saveUsers();
      }

      private void buttonAddKnownHost_Click(object sender, EventArgs e)
      {
         launchAddKnownHostDialog();
      }

      private void buttonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            removeKnownHost(listViewKnownHosts.SelectedItems[0].Text);
            updateHostsListView();
         }
         if (listViewKnownHosts.Items.Count == 0)
         {
            onKnownHostSelectionChanged();
         }
      }

      private void buttonEditUsers_Click(object sender, EventArgs e)
      {
         launchEditUserListDialog();
      }

      private void buttonEditProjects_Click(object sender, EventArgs e)
      {
         launchEditProjectListDialog();
      }

      private void linkLabelWorkflowDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         showHelp();
      }

      private static void showHelp()
      {
         Trace.TraceInformation("[ConfigureHostsForm] Clicked on link label for workflow type selection");
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         if (helpUrl != String.Empty)
         {
            UrlHelper.OpenBrowser(helpUrl);
         }
      }

      private void loadKnownHosts()
      {
         bool hasBadValues = false;
         string[] hosts = Program.Settings.KnownHosts.ToArray();
         for (int iKnownHost = 0; iKnownHost < hosts.Length; ++iKnownHost)
         {
            string host = StringUtils.GetHostWithPrefix(hosts[iKnownHost]);
            string accessToken = Program.Settings.GetAccessToken(hosts[iKnownHost]);
            hasBadValues |= accessToken == Constants.ConfigurationBadValueLoaded;
            addKnownHost(host, accessToken);
         }
         if (hasBadValues)
         {
            MessageBox.Show("For security reasons access tokens are kept in " +
               "encrypted format and cannot be used at multiple PC at once. " +
               "Please replace current access tokens with new ones.", "Bad access token",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
         }
      }

      private bool addKnownHost(string newHost, string newAccessToken)
      {
         if (_hosts.ContainsKey(newHost))
         {
            return false;
         }
         _hosts[newHost] = newAccessToken;
         return true;
      }

      private void removeKnownHost(string hostname)
      {
         _hosts.Remove(hostname);
      }

      private void forEachKnownHost(Action<string> action)
      {
         _hosts.Keys.ToList().ForEach(hostname => action(hostname));
      }

      private bool saveProjects()
      {
         bool changed = false;
         forEachKnownHost(hostname =>
         {
            var oldProjects = ConfigurationHelper.GetProjectsForHost(hostname, Program.Settings);
            var newProjects = _projects.ContainsKey(hostname) ? _projects[hostname] : new StringToBooleanCollection();
            ConfigurationHelper.SetProjectsForHost(hostname, newProjects, Program.Settings);
            changed |= !Enumerable.SequenceEqual(oldProjects, newProjects);
         });
         return changed;
      }

      private void loadProjectsForHost(string hostname)
      {
         var projects = ConfigurationHelper.GetProjectsForHost(hostname, Program.Settings);
         if (!projects.Any())
         {
            projects = DefaultWorkflowLoader.GetDefaultProjectsForHost(hostname, false);
         }
         setProjectsForHost(hostname, projects);
      }

      private void setProjectsForHost(string hostname, StringToBooleanCollection projectNames)
      {
         _projects[hostname] = projectNames;
      }

      private StringToBooleanCollection getProjectsForHost(string hostname)
      {
         if (!_projects.ContainsKey(hostname))
         {
            Debug.Assert(false);
            return new StringToBooleanCollection();
         }
         return _projects[hostname];
      }

      private bool saveUsers()
      {
         bool changed = false;
         forEachKnownHost(hostname =>
         {
            var oldUsers = ConfigurationHelper.GetUsersForHost(hostname, Program.Settings);
            var newUsers = _usernames.ContainsKey(hostname) ? _usernames[hostname] : new StringToBooleanCollection();
            ConfigurationHelper.SetUsersForHost(hostname, newUsers, Program.Settings);
            changed |= !Enumerable.SequenceEqual(oldUsers, newUsers);
         });
         return changed;
      }

      private async Task loadUsersForHost(string hostname)
      {
         var users = ConfigurationHelper.GetUsersForHost(hostname, Program.Settings);
         if (!users.Any())
         {
            GitLabInstance gitLabInstance = new GitLabInstance(hostname, this, this);
            users = await DefaultWorkflowLoader.GetDefaultUsersForHost(gitLabInstance, null);
         }
         setUsersForHost(hostname, users);
      }

      private StringToBooleanCollection getUsersForHost(string hostname)
      {
         if (!_usernames.ContainsKey(hostname))
         {
            Debug.Assert(false);
            return new StringToBooleanCollection();
         }
         return _usernames[hostname];
      }

      private void setUsersForHost(string hostname, StringToBooleanCollection usernames)
      {
         _usernames[hostname] = usernames;
      }

      private void updateHostsListView()
      {
         listViewKnownHosts.Items.Clear();

         foreach (KeyValuePair<string, string> kv in _hosts)
         {
            ListViewItem item = new ListViewItem(kv.Key);
            item.SubItems.Add(kv.Value);
            listViewKnownHosts.Items.Add(item);
         }

         if (listViewKnownHosts.Items.Count > 0)
         {
            listViewKnownHosts.Items[listViewKnownHosts.Items.Count - 1].Selected = true;
         }
      }

      private void updateProjectsListView()
      {
         ListViewGroup listViewGroupProjects = listViewWorkflow.Groups["listViewGroupProjects"];
         for (int iItem = listViewWorkflow.Items.Count - 1; iItem >= 0; --iItem)
         {
            if (listViewWorkflow.Items[iItem].Group == listViewGroupProjects)
            {
               listViewWorkflow.Items.RemoveAt(iItem);
            }
         }

         string hostname = getSelectedHostName();
         if (hostname != null)
         {
            getProjectsForHost(hostname)
               .ToList()
               .ForEach(item => listViewWorkflow.Items.Add(
                  new ListViewItem(item.Item1, listViewGroupProjects) { Tag = item }));
         }
      }

      private void updateUsersListView()
      {
         ListViewGroup listViewGroupUsers = listViewWorkflow.Groups["listViewGroupUsers"];
         for (int iItem = listViewWorkflow.Items.Count - 1; iItem >= 0; --iItem)
         {
            if (listViewWorkflow.Items[iItem].Group == listViewGroupUsers)
            {
               listViewWorkflow.Items.RemoveAt(iItem);
            }
         }

         string hostname = getSelectedHostName();
         if (hostname != null)
         {
            getUsersForHost(hostname)
               .ToList()
               .ForEach(item => listViewWorkflow.Items.Add(
                  new ListViewItem(item.Item1, listViewGroupUsers) { Tag = item }));
         }
      }

      private void updateLinkLabel()
      {
         string hostname = getSelectedHostName();
         linkLabelCreateAccessToken.Text = GitLabClient.Helpers.GetCreateAccessTokenUrl(hostname);
      }

      private string getSelectedHostName()
      {
         return listViewKnownHosts.SelectedItems.Count < 1 ? null : listViewKnownHosts.SelectedItems[0].Text;
      }

      private void launchAddKnownHostDialog()
      {
         using (AddKnownHostForm form = new AddKnownHostForm())
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, this) != DialogResult.OK)
            {
               return;
            }

            BeginInvoke(new Action(async () =>
            {
               onStartChecking(CheckingMode.CheckingHost);
               try
               {
                  string hostname = StringUtils.GetHostWithPrefix(form.Host);
                  string accessToken = form.AccessToken;
                  ConnectionCheckStatus status = await ConnectionChecker.CheckConnectionAsync(hostname, accessToken);
                  if (status != ConnectionCheckStatus.OK)
                  {
                     string message =
                        status == ConnectionCheckStatus.BadAccessToken
                           ? String.Format("Bad access token \"{0}\"", accessToken)
                           : String.Format("Invalid hostname \"{0}\"", hostname);
                     MessageBox.Show(message, "Cannot connect to the host",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                     return;
                  }

                  if (!addKnownHost(hostname, accessToken))
                  {
                     MessageBox.Show("Such host is already in the list", "Host will not be added",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     return;
                  }

                  await loadUsersForHost(hostname);
                  loadProjectsForHost(hostname);
                  updateHostsListView();
               }
               finally
               {
                  onEndChecking();
               }
            }));
         }
      }

      private bool saveKnownHosts()
      {
         IEnumerable<string> hosts = _hosts.Keys;
         IEnumerable<string> tokens = _hosts.Values;

         string[] oldHosts = Program.Settings.KnownHosts.ToArray();
         string[] oldTokens = Program.Settings.KnownAccessTokens.ToArray();

         ConfigurationHelper.SetAuthInfo(Enumerable.Zip(hosts, tokens,
            (a, b) => new Tuple<string, string>(a, b)), Program.Settings);

         return !Enumerable.SequenceEqual(oldHosts, Program.Settings.KnownHosts)
             || !Enumerable.SequenceEqual(oldTokens, Program.Settings.KnownAccessTokens);
      }

      private void updateEnablementsOfWorkflowSelectors()
      {
         bool enabled = listViewKnownHosts.SelectedItems.Count > 0;
         listViewWorkflow.Enabled = !isChecking() && enabled;
         buttonEditProjects.Enabled = !isChecking() && enabled;
         buttonEditUsers.Enabled = !isChecking() && enabled;
      }

      private void launchEditProjectListDialog()
      {
         string hostname = getSelectedHostName();
         if (hostname == null)
         {
            return;
         }

         StringToBooleanCollection projects = getProjectsForHost(hostname);
         Debug.Assert(projects != null);

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, this, this);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Projects",
            "Add project", "Type project name in group/project format",
            projects, new EditProjectsListViewCallback(rawDataAccessor), true))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) == DialogResult.OK
                && !Enumerable.SequenceEqual(projects, form.Items))
            {
               setProjectsForHost(hostname, form.Items);
               updateProjectsListView();
            }
         }
      }

      private void launchEditUserListDialog()
      {
         string hostname = getSelectedHostName();
         if (hostname == null)
         {
            return;
         }

         StringToBooleanCollection users = getUsersForHost(hostname);
         Debug.Assert(users != null);

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, this, this);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Users",
            "Add username", "Type a name of GitLab user, teams allowed",
            users, new EditUsersListViewCallback(rawDataAccessor), false))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) == DialogResult.OK
                && !Enumerable.SequenceEqual(users, form.Items))
            {
               setUsersForHost(hostname, form.Items);
               updateUsersListView();
            }
         }
      }

      private void updateAddRemoveButtonState()
      {
         bool enableRemoveButton = listViewKnownHosts.SelectedItems.Count > 0;
         buttonAddKnownHost.Enabled = !isChecking();
         buttonRemoveKnownHost.Enabled = !isChecking() && enableRemoveButton;
      }

      private void updateOkCancelButtonState()
      {
         buttonOK.Enabled = !isChecking();
         buttonCancel.Enabled = !isChecking();
      }

      private void onKnownHostSelectionChanged()
      {
         updateAddRemoveButtonState();
         updateEnablementsOfWorkflowSelectors();
         updateProjectsListView();
         updateUsersListView();
         updateLinkLabel();
      }

      enum CheckingMode
      {
         LoadingUsers,
         CheckingHost
      }

      private void onStartChecking(CheckingMode mode)
      {
         switch (mode)
         {
            case CheckingMode.LoadingUsers:
               labelChecking.Text = "Loading users...";
               break;
            case CheckingMode.CheckingHost:
               labelChecking.Text = "Checking host...";
               break;
         }

         labelChecking.Visible = true;
         updateAddRemoveButtonState();
         updateEnablementsOfWorkflowSelectors();
         updateOkCancelButtonState();
      }

      private void onEndChecking()
      {
         labelChecking.Visible = false;
         updateAddRemoveButtonState();
         updateEnablementsOfWorkflowSelectors();
         updateOkCancelButtonState();
      }

      private bool isChecking()
      {
         return labelChecking.Visible;
      }

      public string GetAccessToken(string host)
      {
         return _hosts.ContainsKey(host) ? _hosts[host] : String.Empty;
      }

      private readonly Dictionary<string, string> _hosts =
         new Dictionary<string, string>();
      private readonly Dictionary<string, StringToBooleanCollection> _projects =
         new Dictionary<string, StringToBooleanCollection>();
      private readonly Dictionary<string, StringToBooleanCollection> _usernames =
         new Dictionary<string, StringToBooleanCollection>();
   }
}

