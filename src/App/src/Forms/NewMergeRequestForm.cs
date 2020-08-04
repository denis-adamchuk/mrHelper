using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.src.Forms;
using mrHelper.GitLabClient;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Forms
{
   internal class NewMergeRequestForm : MergeRequestPropertiesForm
   {
      internal NewMergeRequestForm(ProjectAccessor projectAccessor, User currentUser,
         NewMergeRequestProperties initialState, IEnumerable<ProjectKey> projects)
         : base(projectAccessor, currentUser)
      {
         _initialState = initialState;
         _projects = projects;

         comboBoxSourceBranch.SelectedIndexChanged += new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         comboBoxProject.SelectedIndexChanged += new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         _repositoryAccessor?.Dispose();
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

         Commit commit = await loadCommitAsync();
         await searchTargetBranchNameAsync(commit);
      }

      async private Task loadBranchListAsync()
      {
         onSourceBranchListLoadStart();

         string search = "^task/" + _currentUser.Username + "/";
         Debug.Assert(_repositoryAccessor != null);
         IEnumerable<Branch> branchList = await _repositoryAccessor.GetBranches(search);

         onSourceBranchListLoadFinish(branchList);
      }

      private void onSourceBranchListLoadStart()
      {
         updateRepositoryActionsState(false);
         groupBoxSource.Text = "Source Branch (Loading...)";
      }

      private void onSourceBranchListLoadFinish(IEnumerable<Branch> branchList)
      {
         updateRepositoryActionsState(true);

         groupBoxSource.Text = "Source Branch";

         Branch[] branchArray = branchList.ToArray();
         comboBoxSourceBranch.Items.AddRange(branchArray);

         selectBranch(comboBoxSourceBranch, x => true /* to select the first item */);
      }

      async private Task<Commit> loadCommitAsync()
      {
         onCommitLoading();

         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         Commit commit = await _repositoryAccessor.LoadCommit(sourceBranch?.Name);

         onCommitLoaded(commit);
         return commit;
      }

      private void onCommitLoading()
      {
         htmlPanelTitle.Text = "Loading...";
         htmlPanelDescription.Text = "Loading...";
      }

      private void onCommitLoaded(Commit commit)
      {
         htmlPanelTitle.Text = commit.Title;
         htmlPanelDescription.Text = commit.Message;
      }

      async private Task searchTargetBranchNameAsync(Commit commit)
      {
         onTargetBranchSearchStart();
         groupBoxTarget.Text = "Target Branch (Loading...)";

         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         string targetBranchName = await _repositoryAccessor.FindPreferredTargetBranchName(
            sourceBranch?.Name, commit?.Parent_Ids.FirstOrDefault());

         onTargetBranchSearchFinish(targetBranchName);
      }

      private void onTargetBranchSearchStart()
      {
         updateRepositoryActionsState(false);
         groupBoxTarget.Text = "Target Branch";
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

         comboBoxProject.Items.AddRange(_projects
            .OrderBy(x => x.ProjectName)
            .Select(x => x.ProjectName)
            .ToArray());
         if (comboBoxProject.Items.Count > 0)
         {
            int defaultProjectIndex = comboBoxProject.Items.IndexOf(_initialState.DefaultProject);
            comboBoxProject.SelectedIndex = defaultProjectIndex == -1 ? 0 : defaultProjectIndex;
         }
      }

      private void updateRepositoryActionsState(bool enabled)
      {
         bool areSourceBranchesAvailable = comboBoxSourceBranch.Items.Count > 0;
         comboBoxSourceBranch.Enabled = enabled && areSourceBranchesAvailable;
         comboBoxTargetBranch.Enabled = enabled && areSourceBranchesAvailable;
      }

      private readonly NewMergeRequestProperties _initialState;
      protected RepositoryAccessor _repositoryAccessor;
      private readonly IEnumerable<ProjectKey> _projects;
   }
}

