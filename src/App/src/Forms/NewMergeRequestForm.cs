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
      internal NewMergeRequestForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         NewMergeRequestProperties initialState, IEnumerable<ProjectKey> projects, string sourceBranchTemplate)
         : base(hostname, projectAccessor, currentUser)
      {
         _initialState = initialState;
         _projects = projects;
         _sourceBranchTemplate = sourceBranchTemplate;

         comboBoxProject.SelectedIndexChanged += new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
         comboBoxSourceBranch.SelectedIndexChanged += new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         comboBoxTargetBranch.TextChanged += new System.EventHandler(this.comboBoxTargetBranch_TextChanged);
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

      private void comboBoxTargetBranch_TextChanged(object sender, EventArgs e)
      {
         updateControls();
      }

      async private Task loadBranchListAsync()
      {
         onSourceBranchListLoadStart();

         Debug.Assert(_repositoryAccessor != null);
         IEnumerable<Branch> branchList = await _repositoryAccessor.GetBranches(_sourceBranchTemplate);

         onSourceBranchListLoadFinish(branchList);
      }

      private void onSourceBranchListLoadStart()
      {
         comboBoxSourceBranch.Items.Clear();
         comboBoxTargetBranch.Items.Clear();

         updateControls();
         groupBoxSource.Text = "Source Branch (Loading...)";
      }

      private void onSourceBranchListLoadFinish(IEnumerable<Branch> branchList)
      {
         if (branchList != null && branchList.Any())
         {
            comboBoxSourceBranch.Items.AddRange(branchList.ToArray());
            comboBoxSourceBranch.SelectedIndex = 0;
         }

         updateControls();
         groupBoxSource.Text = "Source Branch";
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
         setTitle("Loading...");
         setDescription("Loading...");
         _isLoadingCommit = true;
         updateControls();
      }

      private void onCommitLoaded(Commit commit)
      {
         setTitle(commit?.Title ?? String.Empty);
         setDescription(trimTitleFromCommitMessage(commit));
         _isLoadingCommit = false;
         updateControls();
      }

      private static string trimTitleFromCommitMessage(Commit commit)
      {
         string message = commit?.Message ?? String.Empty;
         if (message.StartsWith(commit.Title))
         {
            message = commit.Message.Substring(commit.Title.Length, commit.Message.Length - commit.Title.Length);
            message = message.TrimStart(new char[] { '\n' });
         }
         return message;
      }

      async private Task searchTargetBranchNameAsync(Commit sourceBranchCommit)
      {
         onTargetBranchSearchStart();

         Debug.Assert(_repositoryAccessor != null);
         Branch sourceBranch = getSourceBranch();
         IEnumerable<string> targetBranchNames = await _repositoryAccessor.FindPreferredTargetBranchNames(
            sourceBranch, sourceBranchCommit);

         onTargetBranchSearchFinish(targetBranchNames);
      }

      private void onTargetBranchSearchStart()
      {
         comboBoxTargetBranch.Items.Clear();

         updateControls();
         groupBoxTarget.Text = "Target Branch (Loading...)";
      }

      private void onTargetBranchSearchFinish(IEnumerable<string> targetBranchNames)
      {
         if (targetBranchNames != null && targetBranchNames.Any())
         {
            comboBoxTargetBranch.Items.AddRange(targetBranchNames.ToArray());
            comboBoxTargetBranch.SelectedIndex = 0;
         }

         updateControls();
         groupBoxTarget.Text = "Target Branch";
      }

      protected override void applyInitialState()
      {
         checkBoxSquash.Checked = _initialState.IsSquashNeeded;
         checkBoxDeleteSourceBranch.Checked = _initialState.IsBranchDeletionNeeded;
         textBoxAssigneeUsername.Text = _initialState.AssigneeUsername;
         setTitle(String.Empty);
         setDescription(String.Empty);

         comboBoxProject.Items.AddRange(_projects
            .OrderBy(x => x.ProjectName)
            .Select(x => x.ProjectName)
            .ToArray());
         if (comboBoxProject.Items.Count > 0)
         {
            int defaultProjectIndex = comboBoxProject.Items.IndexOf(_initialState.DefaultProject ?? String.Empty);
            comboBoxProject.SelectedIndex = defaultProjectIndex == -1 ? 0 : defaultProjectIndex;
         }
      }

      private void updateControls()
      {
         bool areSourceBranches = comboBoxSourceBranch.Items.Count > 0;
         comboBoxSourceBranch.Enabled = areSourceBranches;

         bool isSourceBranchSelected = comboBoxSourceBranch.SelectedItem != null;
         comboBoxTargetBranch.Enabled = isSourceBranchSelected;

         bool isTargetBranchSelected = !String.IsNullOrEmpty(comboBoxTargetBranch.Text);
         bool allDetailsLoaded = isSourceBranchSelected && isTargetBranchSelected && !_isLoadingCommit;
         buttonEditDescription.Enabled = allDetailsLoaded;
         buttonEditTitle.Enabled = allDetailsLoaded;
         buttonToggleWIP.Enabled = allDetailsLoaded;
         checkBoxDeleteSourceBranch.Enabled = allDetailsLoaded;
         checkBoxSquash.Enabled = allDetailsLoaded;
         textBoxAssigneeUsername.Enabled = allDetailsLoaded;

         buttonSubmit.Enabled = allDetailsLoaded && !String.IsNullOrEmpty(getTitle());
      }

      protected RepositoryAccessor _repositoryAccessor;
      private bool _isLoadingCommit;
      private readonly NewMergeRequestProperties _initialState;
      private readonly IEnumerable<ProjectKey> _projects;
      private readonly string _sourceBranchTemplate;
   }
}

