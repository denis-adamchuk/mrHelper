using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.src.Forms;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal class NewMergeRequestForm : MergeRequestPropertiesForm
   {
      internal NewMergeRequestForm(ProjectAccessor projectAccessor, User currentUser,
         NewMergeRequestProperties initialState)
         : base(projectAccessor, currentUser)
      {
         _initialState = initialState;

         comboBoxSourceBranch.SelectedIndexChanged += new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         comboBoxProject.SelectedIndexChanged += new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         _repositoryAccessor?.Dispose();
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);

         BeginInvoke(new Action(async () => await loadProjectListAsync()));
      }

      async private void comboBoxProject_SelectedIndexChanged(object sender, EventArgs e)
      {
         _repositoryAccessor?.Cancel();
         _repositoryAccessor = createRepositoryAccessor();

         await loadBranchListAsync();
      }

      async private void comboBoxSourceBranch_SelectedIndexChanged(object sender, System.EventArgs e)
      {
         _repositoryAccessor?.Cancel();

         await loadCommitAsync();
         await searchTargetBranchNameAsync();
      }

      async private Task loadProjectListAsync()
      {
         onProjectListLoadStart();

         IEnumerable<Project> projects = await _projectAccessor.LoadProjects();

         onProjectListLoadFinish(projects);
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

      async private Task loadBranchListAsync()
      {
         onBranchListLoadStart();

         Debug.Assert(_repositoryAccessor != null);
         IEnumerable<Branch> branchList = await _repositoryAccessor.GetBranches();

         onBranchListLoadFinish(branchList);
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

      async private Task loadCommitAsync()
      {
         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         Commit commit = await _repositoryAccessor.LoadCommit(sourceBranch?.Name);

         onCommitLoaded(commit);
      }

      private void onCommitLoaded(Commit commit)
      {
         htmlPanelTitle.Text = commit.Title;
         htmlPanelDescription.Text = commit.Message;
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

      private void onTargetBranchSearchStart()
      {
         updateRepositoryActionsState(false);
      }

      private void onTargetBranchSearchFinish(string targetBranchName)
      {
         updateRepositoryActionsState(true);

         selectBranch(comboBoxTargetBranch, x => x.Name == targetBranchName);
      }

      protected override void applyInitialState()
      {
         checkBoxSquash.Checked = _initialState.IsSquashNeeded;
         checkBoxDeleteSourceBranch.Checked = _initialState.IsBranchDeletionNeeded;
      }

      private void updateRepositoryActionsState(bool enabled)
      {
         comboBoxProject.Enabled = enabled;
         comboBoxSourceBranch.Enabled = enabled;
         comboBoxTargetBranch.Enabled = enabled;
      }

      private readonly NewMergeRequestProperties _initialState;
      protected RepositoryAccessor _repositoryAccessor;
   }
}

