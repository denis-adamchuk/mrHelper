using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.App.Forms.Helpers;
using mrHelper.GitLabClient;

namespace mrHelper.App.src.Forms
{
   internal partial class CreateNewMergeRequestForm : CustomFontForm
   {
      internal CreateNewMergeRequestForm(
         ProjectAccessor projectAccessor,
         User currentUser,
         CreateNewMergeRequestState initialState)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _projectAccessor = projectAccessor;
         _currentUser = currentUser;
         _initialState = initialState;

         textBoxFirstNote.Text = DefaultFirstNote;
      }

      internal string Project => getProjectName();
      internal string SourceBranch => getSourceBranch()?.Name;
      internal string TargetBranch => getTargetBranch()?.Name;
      internal string AssigneeUsername => getUserName();
      internal bool DeleteSourceBranch => checkBoxDeleteSourceBranch.Checked;
      internal bool Squash => checkBoxSquash.Checked;
      internal string Title => htmlPanelTitle.Text;
      internal string Description => htmlPanelDescription.Text;

      async private void CreateNewMergeRequestForm_Load(object sender, EventArgs e)
      {
         applyInitialState();

         await loadProjectListAsync();
      }

      async private void comboBoxProject_SelectedIndexChanged(object sender, EventArgs e)
      {
         _repositoryAccessor?.Cancel();
         createRepositoryAccessor();

         await loadBranchListAsync();
      }

      async private void comboBoxSourceBranch_SelectedIndexChanged(object sender, System.EventArgs e)
      {
         _repositoryAccessor?.Cancel();

         await loadCommitAsync();
         await searchTargetBranchNameAsync();
      }

      private void buttonToggleWIP_Click(object sender, EventArgs e)
      {
         string prefix = "WIP: ";
         if (htmlPanelTitle.Text.StartsWith(prefix))
         {
            htmlPanelTitle.Text = htmlPanelTitle.Text.Substring(
               prefix.Length, htmlPanelTitle.Text.Length - prefix.Length);
         }
         else
         {
            htmlPanelTitle.Text = prefix + htmlPanelTitle.Text;
         }
      }

      private void buttonEditTitle_Click(object sender, EventArgs e)
      {
         ViewDiscussionItemForm editTitleForm = new ViewDiscussionItemForm(
            "Edit MR title", htmlPanelTitle.Text, true);
         if (editTitleForm.ShowDialog() == DialogResult.OK)
         {
            htmlPanelTitle.Text = editTitleForm.Body;
         }
      }

      private void buttonEditDescription_Click(object sender, EventArgs e)
      {
         ViewDiscussionItemForm editDescriptionForm = new ViewDiscussionItemForm(
            "Edit MR description", htmlPanelDescription.Text, true);
         if (editDescriptionForm.ShowDialog() == DialogResult.OK)
         {
            htmlPanelDescription.Text = editDescriptionForm.Body;
         }
      }

      async private Task loadProjectListAsync()
      {
         onProjectListLoadStart();

         IEnumerable<Project> projects = await _projectAccessor.LoadProjects();

         onProjectListLoadFinish(projects);
      }

      async private Task loadBranchListAsync()
      {
         onBranchListLoadStart();

         Debug.Assert(_repositoryAccessor != null);
         IEnumerable<Branch> branchList = await _repositoryAccessor.GetBranches();

         onBranchListLoadFinish(branchList);
      }

      async private Task loadCommitAsync()
      {
         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         Commit commit = await _repositoryAccessor.LoadCommit(sourceBranch?.Name);

         onCommitLoaded(commit);
      }

      async private Task searchTargetBranchNameAsync()
      {
         onTargetBranchSearchStart();

         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         string targetBranchName = await _repositoryAccessor.FindPreferredTargetBranchName(
            sourceBranch?.Name, sourceBranch?.Commit.Parent_Ids.FirstOrDefault());

         onTargetBranchSearchFinish(targetBranchName);
      }

      private void onProjectListLoadStart()
      {
         updateRepositoryActionsState(false);
      }

      private void onProjectListLoadFinish(IEnumerable<Project> projectList)
      {
         updateRepositoryActionsState(true);

         string[] projectArray = projectList.Select(x => x.Path_With_Namespace).ToArray();
         comboBoxProject.Items.AddRange(projectArray);

         if (projectArray.Any())
         {
            int defaultProjectIndex = comboBoxProject.Items.IndexOf(_initialState.DefaultProject);
            comboBoxProject.SelectedIndex = defaultProjectIndex == -1 ? 0 : defaultProjectIndex;
         }
      }

      private void onBranchListLoadStart()
      {
         updateRepositoryActionsState(false);
      }

      private void onBranchListLoadFinish(IEnumerable<Branch> branchList)
      {
         updateRepositoryActionsState(true);

         Branch[] branchArray = branchList.ToArray();
         comboBoxSourceBranch.Items.AddRange(branchArray);
         comboBoxTargetBranch.Items.AddRange(branchArray);

         selectBranch(comboBoxSourceBranch, x => x.Name.Contains(String.Format("/{0}/", _currentUser.Username)));
      }

      private void onTargetBranchSearchStart()
      {
         updateRepositoryActionsState(false);
      }

      private void onTargetBranchSearchFinish(string targetBranchName)
      {
         updateRepositoryActionsState(true);

         selectBranch(comboBoxTargetBranch, x => x.Name == targetBranchName);
      }

      private void onCommitLoaded(Commit commit)
      {
         htmlPanelTitle.Text = commit.Title;
         htmlPanelDescription.Text = commit.Message;
      }

      private void selectBranch(ComboBox comboBox, Func<Branch, bool> predicate)
      {
         if (comboBox.Items.Count == 0)
         {
            return;
         }

         Branch preferredBranch = comboBox.Items.Cast<Branch>().FirstOrDefault(predicate);
         Branch defaultBranch = comboBox.Items.Cast<Branch>().FirstOrDefault(x => x.Name == DefaultBranchName);
         Branch selectedBranch = preferredBranch ?? defaultBranch;
         int selectedBranchIndex = comboBox.Items.IndexOf(selectedBranch);
         if (selectedBranchIndex != -1)
         {
            comboBox.SelectedIndex = selectedBranchIndex;
         }
      }

      private Branch getSourceBranch()
      {
         return comboBoxSourceBranch.SelectedItem as Branch;
      }

      private Branch getTargetBranch()
      {
         return comboBoxTargetBranch.SelectedItem as Branch;
      }

      private string getProjectName()
      {
         return comboBoxProject.SelectedItem as string;
      }

      private string getUserName()
      {
         return textBoxAssigneeUsername.Text;
      }

      private void createRepositoryAccessor()
      {
         string projectName = getProjectName();
         if (projectName == null)
         {
            return;
         }

         _repositoryAccessor = _projectAccessor.GetSingleProjectAccessor(projectName).RepositoryAccessor;
      }

      private void updateRepositoryActionsState(bool enabled)
      {
         comboBoxProject.Enabled = enabled;
         comboBoxSourceBranch.Enabled = enabled;
         comboBoxTargetBranch.Enabled = enabled;
      }

      private void applyInitialState()
      {
         checkBoxSquash.Checked = _initialState.IsSquashNeeded;
         checkBoxDeleteSourceBranch.Checked = _initialState.IsBranchDeletionNeeded;
      }

      private readonly string DefaultBranchName = "master";
      private readonly string DefaultFirstNote = "/insp ";
      private readonly ProjectAccessor _projectAccessor;
      private readonly User _currentUser;
      private readonly CreateNewMergeRequestState _initialState;
      private RepositoryAccessor _repositoryAccessor;
   }
}

