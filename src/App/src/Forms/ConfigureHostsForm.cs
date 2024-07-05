using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
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

         labelExpirationHint.Text = String.Format("Tokens prolong automatically {0} days before expiration",
            Constants.AccessTokenDaysToExpireForNotice);
      }

      internal bool Changed { get; private set; }

      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         e.Cancel = isChecking();
      }

      async private void configureHostsForm_Load(object sender, System.EventArgs e)
      {
         onStartChecking(CheckingMode.LoadingData);

         try
         {
            await loadKnownHosts();
            foreach (KeyValuePair<string, TokenInfo> kv in _hosts)
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

      private async Task loadKnownHosts()
      {
         bool hasBadValues = false;
         string[] hosts = Program.Settings.KnownHosts.ToArray();
         for (int iKnownHost = 0; iKnownHost < hosts.Length; ++iKnownHost)
         {
            string host = StringUtils.GetHostWithPrefix(hosts[iKnownHost]);
            string accessToken = Program.Settings.GetAccessToken(hosts[iKnownHost]);
            bool isValid = accessToken != Constants.ConfigurationBadValueLoaded;
            if (isValid)
            {
               PersonalAccessToken token = await loadTokenExpirationDate(host, accessToken);
               if (token != null)
               {
                  addKnownHost(host, accessToken, token.Expires_At);
               }
               else
               {
                  Trace.TraceInformation("[ConfigureHostsForm] Token for host {0} is null", host);
               }
            }
            hasBadValues |= !isValid;
         }
         if (hasBadValues)
         {
            MessageBox.Show("For security reasons access tokens are kept in " +
               "encrypted format and cannot be used at multiple PC at once. " +
               "Please replace current access tokens with new ones.", "Bad access token",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
         }
      }

      private bool addKnownHost(string newHost, string newAccessToken, DateTime? expiresAt)
      {
         if (_hosts.ContainsKey(newHost))
         {
            return false;
         }
         _hosts[newHost] = new TokenInfo(newAccessToken, expiresAt);
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
         StringToBooleanCollection users = ConfigurationHelper.GetUsersForHost(hostname, Program.Settings);
         if (!users.Any())
         {
            GitLabInstance gitLabInstance = new GitLabInstance(hostname, this, this);
            string username = await DefaultWorkflowLoader.GetDefaultUserForHost(gitLabInstance, null);
            Tuple<string, bool>[] collection = new Tuple<string, bool>[] { new Tuple<string, bool>(username, true) };
            users = new StringToBooleanCollection(collection);
         }
         setUsersForHost(hostname, users);
      }

      struct TokenHolder : IHostProperties
      {
         internal TokenHolder(string token) { Token = token; }
         public string GetAccessToken(string _) { return Token; }
         private readonly string Token;
      }

      private async Task<PersonalAccessToken> loadTokenExpirationDate(string hostname, string token)
      {
         TokenHolder holder = new TokenHolder(token);
         GitLabInstance gitLabInstance = new GitLabInstance(hostname, holder, this);
         PersonalAccessTokenAccessor accessor = new Shortcuts(gitLabInstance).GetPersonalAccessTokenAccessor();
         return await accessor.GetPersonalAccessTokenAsync();
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

         foreach (KeyValuePair<string, TokenInfo> kv in _hosts)
         {
            ListViewItem item = new ListViewItem(kv.Key);
            item.SubItems.Add(kv.Value.AccessToken);
            string asText = kv.Value.ExpiresAt.HasValue ? TimeUtils.DateTimeToString(
               kv.Value.ExpiresAt.Value, TimeUtils.DateOnlyTimeStampFormat) : "N/A";
            item.SubItems.Add(asText);
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
                     Trace.TraceInformation("[ConfigureHostsForm] Connection issue. Message = \"{0}\"", message);
                     return;
                  }

                  PersonalAccessToken tokenInfo = await loadTokenExpirationDate(hostname, accessToken);
                  if (tokenInfo == null)
                  {
                     MessageBox.Show("Bad access token", "Cannot connect to the host",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                     Trace.TraceInformation("[ConfigureHostsForm] Bad access token for host {0}", hostname);
                     return;
                  }

                  if (!addKnownHost(hostname, accessToken, tokenInfo.Expires_At))
                  {
                     MessageBox.Show("Such host is already in the list", "Host will not be added",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     Trace.TraceInformation("[ConfigureHostsForm] Cannot add duplicate host {0}", hostname);
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
         IEnumerable<string> tokens = _hosts.Values.Select(x => x.AccessToken);

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
         LoadingData,
         CheckingHost
      }

      private void onStartChecking(CheckingMode mode)
      {
         switch (mode)
         {
            case CheckingMode.LoadingData:
               labelChecking.Text = "Loading data...";
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
         return _hosts.ContainsKey(host) ? _hosts[host].AccessToken : String.Empty;
      }

      private struct TokenInfo
      {
         internal TokenInfo(string accessToken, DateTime? expiresAt)
         {
            AccessToken = accessToken;
            ExpiresAt = expiresAt;
         }

         internal readonly string AccessToken;
         internal readonly DateTime? ExpiresAt;
      };

      private readonly Dictionary<string, TokenInfo> _hosts =
         new Dictionary<string, TokenInfo>();
      private readonly Dictionary<string, StringToBooleanCollection> _projects =
         new Dictionary<string, StringToBooleanCollection>();
      private readonly Dictionary<string, StringToBooleanCollection> _usernames =
         new Dictionary<string, StringToBooleanCollection>();
   }
}

