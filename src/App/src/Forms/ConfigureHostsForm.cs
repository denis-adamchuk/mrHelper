using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
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

         checkHelpAvailability();
      }

      internal bool Changed { get; private set; }

      async private void configureHostsForm_Load(object sender, System.EventArgs e)
      {
         loadKnownHosts();
         loadWorkflowType();

         foreach (KeyValuePair<string, string> kv in _hosts)
         {
            await loadUsersForHost(kv.Key);
            loadProjectsForHost(kv.Key);
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
         Changed |= saveWorkflowType();
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

      private void radioButtonWorkflowType_CheckedChanged(object sender, EventArgs e)
      {
         updateConfigureWorkflowListState();
      }

      private void buttonEditUsers_Click(object sender, EventArgs e)
      {
         launchEditUserListDialog();
      }

      private void buttonEditProjects_Click(object sender, EventArgs e)
      {
         launchEditProjectListDialog();
      }

      private void linkLabelWorkflowDescription_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
      {
         showHelp();
      }

      private void comboBoxProjectName_Format(object sender, ListControlConvertEventArgs e)
      {
         formatProjectListItem(e);
      }

      private void comboBoxUser_Format(object sender, ListControlConvertEventArgs e)
      {
         formatUserListItem(e);
      }

      private void linkLabelWorkflowDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         showHelp();
      }

      private void checkHelpAvailability()
      {
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         bool isHelpAvailable = helpUrl != String.Empty;
         if (isHelpAvailable)
         {
            toolTip.SetToolTip(linkLabelWorkflowDescription, helpUrl);
         }
         linkLabelWorkflowDescription.Visible = isHelpAvailable;
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

      private void loadWorkflowType()
      {
         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            radioButtonSelectByProjects.Checked = true;
         }
         else
         {
            radioButtonSelectByUsernames.Checked = true;
         }
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
            projects = DefaultWorkflowLoader.GetDefaultProjectsForHost(hostname);
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

      private void updateConfigureWorkflowListState()
      {
         listViewProjects.Enabled = radioButtonSelectByProjects.Checked;
         listViewUsers.Enabled = radioButtonSelectByUsernames.Checked;
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
         listViewProjects.Items.Clear();

         string hostname = getSelectedHostName();
         if (hostname != null)
         {
            getProjectsForHost(hostname)
               .Where(item => item.Item2)
               .ToList()
               .ForEach(item => listViewProjects.Items.Add(item.Item1));
         }
      }

      private void updateUsersListView()
      {
         listViewUsers.Items.Clear();

         string hostname = getSelectedHostName();
         if (hostname != null)
         {
            StringToBooleanCollection users = getUsersForHost(hostname);
            users
               .Where(item => item.Item2)
               .ToList()
               .ForEach(item => listViewUsers.Items.Add(item.Item1));
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

      private static void formatProjectListItem(ListControlConvertEventArgs e)
      {
         e.Value = e.ListItem.ToString();
      }

      private static void formatUserListItem(ListControlConvertEventArgs e)
      {
         e.Value = (e.ListItem as User).Name;
      }

      private void launchAddKnownHostDialog()
      {
         using (AddKnownHostForm form = new AddKnownHostForm())
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            BeginInvoke(new Action(async () =>
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

      private bool saveWorkflowType()
      {
         if (radioButtonSelectByProjects.Checked)
         {
            bool wasProjectBased = ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings);
            ConfigurationHelper.SelectProjectBasedWorkflow(Program.Settings);
            return !wasProjectBased;
         }
         else
         {
            bool wasUserBased = !ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings);
            ConfigurationHelper.SelectUserBasedWorkflow(Program.Settings);
            return !wasUserBased;
         }
      }

      private void updateEnablementsOfWorkflowSelectors()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
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

      private void launchEditProjectListDialog()
      {
         string hostname = getSelectedHostName();
         if (hostname == null)
         {
            return;
         }

         StringToBooleanCollection projects = getProjectsForHost(hostname);
         Debug.Assert(projects != null);

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, Program.Settings, this);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Projects",
            "Add project", "Type project name in group/project format",
            projects, new EditProjectsListViewCallback(rawDataAccessor), true))
         {
            if (form.ShowDialog() == DialogResult.OK && !Enumerable.SequenceEqual(projects, form.Items))
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

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, Program.Settings, this);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Users",
            "Add username", "Type a name of GitLab user, teams allowed",
            users, new EditUsersListViewCallback(rawDataAccessor), false))
         {
            if (form.ShowDialog() == DialogResult.OK && !Enumerable.SequenceEqual(users, form.Items))
            {
               setUsersForHost(hostname, form.Items);
               updateUsersListView();
            }
         }
      }

      private void updateAddRemoveButtonState()
      {
         bool enableRemoveButton = listViewKnownHosts.SelectedItems.Count > 0;
         buttonRemoveKnownHost.Enabled = enableRemoveButton;
      }

      private void onKnownHostSelectionChanged()
      {
         updateAddRemoveButtonState();
         updateEnablementsOfWorkflowSelectors();
         updateProjectsListView();
         updateUsersListView();
         updateLinkLabel();
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

