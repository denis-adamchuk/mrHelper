using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using Markdig;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.GitLabClient;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Forms
{
   internal abstract partial class MergeRequestPropertiesForm : CustomFontForm
   {
      internal MergeRequestPropertiesForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         bool isAllowedToChangeSource, IEnumerable<User> users, AvatarImageCache avatarImageCache)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _hostname = hostname;
         _projectAccessor = projectAccessor;
         _currentUser = currentUser;
         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
         _isAllowedToChangeSource = isAllowedToChangeSource;
         _avatarImageCache = avatarImageCache;

         htmlPanelTitle.BaseStylesheet = ResourceHelper.SetControlFontSizeToCommonCss(htmlPanelTitle);
         htmlPanelDescription.BaseStylesheet = ResourceHelper.SetControlFontSizeToCommonCss(htmlPanelDescription);

         labelSpecialNotePrefix.Text = Program.ServiceManager.GetSpecialNotePrefix();

         buttonCancel.ConfirmationCondition = () => true;

         textBoxSpecialNote.Size = new System.Drawing.Size(textBoxSpecialNote.Size.Width, textBoxAssigneeUsername.Height);
         textBoxSpecialNote.Init(false, String.Empty, false, false, true);
         if (users == null || !users.Any())
         {
            _fullUserList = new User[] { };

            // This may happen when new MR creation is requested by URL and mrHelper is not launched.
            // In this case User Cache is still not filled. Let's load project users.
            comboBoxProject.SelectedIndexChanged += new System.EventHandler(loadProjectUsersForAutoCompletion);
         }
         else
         {
            _fullUserList = users;
            setAutocompletionEntities();
         }

         comboBoxProject.SelectedIndexChanged += new System.EventHandler(loadProjectId);
      }

      private void MergeRequestPropertiesForm_Deactivate(object sender, EventArgs e)
      {
         textBoxSpecialNote.HideAutoCompleteBox(SmartTextBox.HidingReason.FormDeactivation);
      }

      private void MergeRequestPropertiesForm_LocationChanged(object sender, EventArgs e)
      {
         textBoxSpecialNote.HideAutoCompleteBox(SmartTextBox.HidingReason.FormMovedOrResized);
      }

      private void MergeRequestPropertiesForm_SizeChanged(object sender, EventArgs e)
      {
         textBoxSpecialNote.HideAutoCompleteBox(SmartTextBox.HidingReason.FormMovedOrResized);
      }

      private void loadProjectUsersForAutoCompletion(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () =>
         {
            string projectName = getProjectName();
            _fullUserList = await _projectAccessor.GetSingleProjectAccessor(projectName).GetUsersAsync();
            setAutocompletionEntities();
         }));
      }

      private void loadProjectId(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () =>
         {
            Project project = await _projectAccessor.SearchProjectAsync(getProjectName());
            _projectId = project?.Id ?? 0;
         }));
      }

      private void setAutocompletionEntities()
      {
         if (_fullUserList != null && _avatarImageCache != null)
         {
            textBoxSpecialNote.SetAutoCompletionEntities(_fullUserList
               .Select(user => new SmartTextBox.AutoCompletionEntity(
                  user.Name, user.Username, SmartTextBox.AutoCompletionEntity.EntityType.User,
                  () => _avatarImageCache.GetAvatar(user))));
         }
      }

      internal string ProjectName => getProjectName();
      internal string SourceBranch => getSourceBranchName();
      internal string TargetBranch => getTargetBranchName();
      internal string AssigneeUsername => getAssigneeUserName();
      internal bool DeleteSourceBranch => checkBoxDeleteSourceBranch.Checked;
      internal bool Squash => checkBoxSquash.Checked;
      internal string Title => getTitle();
      internal string Description => getDescription();
      internal string SpecialNote => getSpecialNote();
      internal bool IsHighPriority => checkBoxHighPriority.Checked;

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);

         applyInitialState();
      }

      private void buttonToggleDraft_Click(object sender, EventArgs e)
      {
         toggleDraft();
      }

      private void buttonEditTitle_Click(object sender, EventArgs e)
      {
         string title = mrHelper.Common.Tools.StringUtils.ConvertNewlineUnixToWindows(getTitle());
         string formCaption = "Edit Merge Request title";
         using (TextEditBaseForm editTitleForm = new SimpleTextEditForm(formCaption, title, false, String.Empty, null, null))
         {
            if (WinFormsHelpers.ShowDialogOnControl(editTitleForm, this) == DialogResult.OK)
            {
               setTitle(Common.Tools.StringUtils.ConvertNewlineWindowsToUnix(editTitleForm.Body));
            }
         }
      }

      private void buttonEditDescription_Click(object sender, EventArgs e)
      {
         string description = mrHelper.Common.Tools.StringUtils.ConvertNewlineUnixToWindows(getDescription());
         string formCaption = "Edit Merge Request description";
         string uploadsPrefix = StringUtils.GetUploadsPrefix(_hostname, _projectId ?? 0);
         using (TextEditBaseForm editDescriptionForm = new SimpleTextEditForm(
            formCaption, description, true, uploadsPrefix, _fullUserList, _avatarImageCache))
         {
            if (WinFormsHelpers.ShowDialogOnControl(editDescriptionForm, this) == DialogResult.OK)
            {
               setDescription(Common.Tools.StringUtils.ConvertNewlineWindowsToUnix(editDescriptionForm.Body));
            }
         }
      }

      private void comboBoxSourceBranch_Format(object sender, ListControlConvertEventArgs e)
      {
         if (e.ListItem is Branch branch)
         {
            e.Value = branch.Name;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private void textBoxAssigneeUsername_KeyDown(object sender, KeyEventArgs e)
      {
         submitOnKeyDown(e.KeyCode, e.Modifiers);
      }

      private void textBoxSpecialNote_KeyDown(object sender, KeyEventArgs e)
      {
         submitOnKeyDown(e.KeyCode, e.Modifiers);
      }

      private void submitOnKeyDown(Keys keyCode, Keys modifiers)
      {
         if (buttonSubmit.Enabled && keyCode == Keys.Enter && modifiers.HasFlag(Keys.Control))
         {
            buttonSubmit.PerformClick();
         }
      }

      private void comboBoxTargetBranch_TextChanged(object sender, EventArgs e)
      {
         updateControls();
      }

      async private void buttonSubmit_Click(object sender, EventArgs e)
      {
         buttonSubmit.Enabled = false;
         try
         {
            if (await verifyInputData())
            {
               Close();
               DialogResult = DialogResult.OK;
               return;
            }
         }
         finally
         {
            buttonSubmit.Enabled = true;
         }
      }

      async private Task<bool> verifyInputData()
      {
         if (!await verifyTargetBranch())
         {
            MessageBox.Show("Cannot submit changes due to invalid target branch", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }
         return true;
      }

      async private Task<bool> verifyTargetBranch()
      {
         if (!isValidBranchName(getTargetBranchName()))
         {
            return false;
         }

         using (RepositoryAccessor repositoryAccessor = createRepositoryAccessor())
         {
            labelCheckingTargetBranch.Visible = true;
            try
            {
               string targetBranch = getTargetBranchName();
               IEnumerable<Branch> branches = await repositoryAccessor.GetBranches(targetBranch);
               return branches != null && branches.Any(branch => branch.Name.ToLower() == targetBranch.ToLower());
            }
            finally
            {
               labelCheckingTargetBranch.Visible = false;
            }
         }
      }

      protected RepositoryAccessor createRepositoryAccessor()
      {
         string projectName = getProjectName();
         if (String.IsNullOrEmpty(projectName))
         {
            Debug.Assert(false);
            return null;
         }

         return _projectAccessor.GetSingleProjectAccessor(projectName).GetRepositoryAccessor();
      }

      protected string getProjectName()
      {
         return (comboBoxProject.SelectedItem as string) ?? String.Empty;
      }

      protected Branch getSourceBranch()
      {
         return comboBoxSourceBranch.SelectedItem as Branch;
      }

      protected string getSourceBranchName()
      {
         return getSourceBranch()?.Name ?? String.Empty;
      }

      protected string getTargetBranchName()
      {
         return comboBoxTargetBranch.Text.Trim();
      }

      protected string getTitle()
      {
         return _title;
      }

      protected void setTitle(string title)
      {
         _title = title;
         htmlPanelTitle.Text = convertTextToHtml(title, htmlPanelTitle);
         updateControls();
      }

      protected string getDescription()
      {
         return _description;
      }

      protected void setDescription(string description)
      {
         _description = description;
         htmlPanelDescription.Text = convertTextToHtml(description, htmlPanelDescription);
         updateControls();
      }

      protected string getAssigneeUserName()
      {
         return convertLabelToWord(textBoxAssigneeUsername.Text);
      }

      protected void setAssigneeUsername(string username)
      {
         textBoxAssigneeUsername.Text = username.Trim();
      }

      protected string getSpecialNote()
      {
         if (String.IsNullOrWhiteSpace(textBoxSpecialNote.Text))
         {
            return String.Empty;
         }
         return Program.ServiceManager.GetSpecialNotePrefix() + formatSpecialNote(textBoxSpecialNote.Text);
      }

      protected void setSpecialNote(string specialNote)
      {
         if (String.IsNullOrWhiteSpace(specialNote))
         {
            textBoxSpecialNote.Text = String.Empty;
         }
         else if (specialNote.StartsWith(labelSpecialNotePrefix.Text))
         {
            textBoxSpecialNote.Text = specialNote.Substring(labelSpecialNotePrefix.Text.Length);
         }
         else
         {
            textBoxSpecialNote.Text = specialNote;
         }
      }

      protected void toggleDraft()
      {
         setTitle(StringUtils.ToggleDraftTitle(getTitle()));
      }

      protected void fillProjectListAndSelect(
         IEnumerable<string> projects, IEnumerable<string> favoriteProjects, string defaultProjectName)
      {
         if (favoriteProjects.Any())
         {
            comboBoxProject.Items.Add(FavoriteProjectsItemText);
            comboBoxProject.Items.AddRange(favoriteProjects.ToArray());
            comboBoxProject.Items.Add(AllProjectsItemText);
         }
         comboBoxProject.Items.AddRange(projects.OrderBy(x => x).ToArray());
         WinFormsHelpers.SelectComboBoxItem(comboBoxProject, String.IsNullOrWhiteSpace(defaultProjectName)
            ? null : new Func<object, bool>(o => (o as string) == defaultProjectName));
      }

      protected void fillProjectListAndSelect(string project)
      {
         fillProjectListAndSelect(new string[] { project }, Array.Empty<string>(), null);
      }

      protected void fillSourceBranchListAndSelect(IEnumerable<Branch> branches)
      {
         comboBoxSourceBranch.Items.AddRange(branches.ToArray());
         WinFormsHelpers.SelectComboBoxItem(comboBoxSourceBranch, null);
      }

      protected void fillTargetBranchListAndSelect(IEnumerable<string> branchNames)
      {
         branchNames = branchNames.Count() > 1 ?
            StringUtils.InsertString(branchNames, 0, UndefinedBranchName) : branchNames;
         comboBoxTargetBranch.Items.AddRange(branchNames.ToArray());
         WinFormsHelpers.SelectComboBoxItem(comboBoxTargetBranch, null);
      }

      protected abstract void applyInitialState();
      protected abstract bool isLoadingCommit();

      protected void updateControls()
      {
         bool isProjectSelected = !String.IsNullOrEmpty(getProjectName());
         comboBoxProject.Enabled = _isAllowedToChangeSource;

         bool areSourceBranches = comboBoxSourceBranch.Items.Count > 0;
         comboBoxSourceBranch.Enabled = areSourceBranches && _isAllowedToChangeSource;

         bool isSourceBranchSelected = !String.IsNullOrEmpty(getSourceBranchName());
         comboBoxTargetBranch.Enabled = isSourceBranchSelected;

         bool isTargetBranchSelected = isValidBranchName(getTargetBranchName());
         bool allDetailsLoaded = isProjectSelected && isSourceBranchSelected && isTargetBranchSelected && !isLoadingCommit();
         buttonEditDescription.Enabled = allDetailsLoaded;
         buttonEditTitle.Enabled = allDetailsLoaded;
         buttonToggleDraft.Enabled = allDetailsLoaded;
         checkBoxDeleteSourceBranch.Enabled = allDetailsLoaded;
         if (getSourceBranchName() == "master")
         {
            checkBoxDeleteSourceBranch.Enabled = false;
            checkBoxDeleteSourceBranch.Checked = false;
         }
         checkBoxSquash.Enabled = allDetailsLoaded;
         textBoxAssigneeUsername.Enabled = allDetailsLoaded;
         checkBoxHighPriority.Enabled = allDetailsLoaded;

         buttonSubmit.Enabled = allDetailsLoaded && !String.IsNullOrEmpty(getTitle());
      }

      private string convertTextToHtml(string text, Control control)
      {
         string prefix = StringUtils.GetGitLabAttachmentPrefix(_hostname, getProjectName());
         string html = MarkDownUtils.ConvertToHtml(text, prefix, _mdPipeline, control);
         return String.Format(MarkDownUtils.HtmlPageTemplate, html);
      }

      private static string formatSpecialNote(string text)
      {
         // 0. Prerequisite check
         if (String.IsNullOrEmpty(text))
         {
            return String.Empty;
         }

         // 1. Replace commas with spaces and trim extra spaces
         string trimmed = text.Replace(" ,", " ").Replace(", ", " ").Replace(",", " ").Trim();
         if (String.IsNullOrEmpty(trimmed))
         {
            return String.Empty;
         }

         // 2. Trim prefix
         trimmed = trimmed.StartsWith(Program.ServiceManager.GetSpecialNotePrefix())
            ? trimmed.Substring(Program.ServiceManager.GetSpecialNotePrefix().Length)
            : trimmed;

         // 3. Split in words and convert them to names
         var names = trimmed.Split(' ').Select(word => StringUtils.AddAtSignToLetterSubstring(word));

         // 4. Combine names into a string
         return String.Join(" ", names);
      }

      private static string convertLabelToWord(string text)
      {
         string trimmed = text.Trim();
         if (String.IsNullOrEmpty(trimmed))
         {
            return String.Empty;
         }
         return trimmed.StartsWith("@") ? trimmed.Substring(1) : trimmed;
      }

      private static bool isValidBranchName(string name)
      {
         return !String.IsNullOrWhiteSpace(name) && name != UndefinedBranchName;
      }

      protected static string FavoriteProjectsItemText = "--- Recently used ---";
      protected static string AllProjectsItemText      = "--- All ---";
      protected static string UndefinedBranchName = "Select branch manually!";

      protected readonly User _currentUser;
      protected readonly ProjectAccessor _projectAccessor;
      protected readonly string _hostname;

      private readonly MarkdownPipeline _mdPipeline;
      private readonly bool _isAllowedToChangeSource;
      private IEnumerable<User> _fullUserList;
      private int? _projectId;
      private readonly AvatarImageCache _avatarImageCache;
      private string _title;
      private string _description;
   }
}

