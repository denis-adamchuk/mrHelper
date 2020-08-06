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
         : base(hostname, projectAccessor, currentUser, true)
      {
         _initialState = initialState;
         _projects = projects;
         _sourceBranchTemplate = sourceBranchTemplate ?? String.Empty;

         comboBoxProject.SelectedIndexChanged += new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
         comboBoxSourceBranch.SelectedIndexChanged += new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         comboBoxTargetBranch.TextChanged += new System.EventHandler(this.comboBoxTargetBranch_TextChanged);

         buttonCancel.ConfirmationText = "Do you want to discard creating a new merge request?";
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
         Debug.Assert(_sourceBranchTemplate != null);
         onSourceBranchListLoadStart();
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
            fillSourceBranchListAndSelect(branchList.ToArray());
         }

         updateControls();
         groupBoxSource.Text = "Source Branch";
      }

      async private Task<Commit> loadCommitAsync()
      {
         Branch sourceBranch = getSourceBranch();
         Debug.Assert(sourceBranch != null);

         onCommitLoading();
         Commit commit = await _repositoryAccessor.LoadCommit(sourceBranch.Name);
         onCommitLoaded(commit);
         return commit;
      }

      private void onCommitLoading()
      {
         setTitle(String.Empty);
         setDescription(String.Empty);

         _isLoadingCommit = true;
         updateControls();
      }

      private void onCommitLoaded(Commit commit)
      {
         if (commit != null)
         {
            setTitle(commit.Title);
            setDescription(trimTitleFromCommitMessage(commit));
            toggleWIP(); // switch on WIP by default
         }

         _isLoadingCommit = false;
         updateControls();
      }

      private static string trimTitleFromCommitMessage(Commit commit)
      {
         Debug.Assert(commit != null);
         string message = commit.Message;
         if (message.StartsWith(commit.Title))
         {
            message = commit.Message.Substring(commit.Title.Length);
            message = message.TrimStart(new char[] { '\n' });
         }
         return message;
      }

      async private Task searchTargetBranchNameAsync(Commit sourceBranchCommit)
      {
         Branch sourceBranch = getSourceBranch();
         Debug.Assert(sourceBranch != null);
         if (sourceBranchCommit == null)
         {
            return;
         }

         onTargetBranchSearchStart();
         IEnumerable<string> targetBranchNames =
            await _repositoryAccessor.FindPreferredTargetBranchNames( sourceBranch, sourceBranchCommit);
         onTargetBranchSearchFinish(targetBranchNames);
      }

      private void onTargetBranchSearchStart()
      {
         updateControls();
         groupBoxTarget.Text = "Target Branch (Loading...)";
      }

      private void onTargetBranchSearchFinish(IEnumerable<string> targetBranchNames)
      {
         if (targetBranchNames != null && targetBranchNames.Any())
         {
            fillTargetBranchListAndSelect(targetBranchNames.ToArray());
         }

         updateControls();
         groupBoxTarget.Text = "Target Branch";
      }

      protected override void applyInitialState()
      {
         checkBoxSquash.Checked = _initialState.IsSquashNeeded;
         checkBoxDeleteSourceBranch.Checked = _initialState.IsBranchDeletionNeeded;
         setTitle(String.Empty);
         setDescription(String.Empty);
         setAssigneeUsername(_initialState.AssigneeUsername);
         fillProjectListAndSelect();
         updateControls();
      }

      private void fillProjectListAndSelect()
      {
         comboBoxProject.Items.AddRange(_projects
            .OrderBy(x => x.ProjectName)
            .Select(x => x.ProjectName)
            .ToArray());
         if (comboBoxProject.Items.Count > 0)
         {
            int selectedProjectIndex = 0;
            if (_initialState.DefaultProject != null)
            {
               int defaultProjectIndex = comboBoxProject.Items.IndexOf(_initialState.DefaultProject);
               if (defaultProjectIndex != -1)
               {
                  selectedProjectIndex = defaultProjectIndex;
               }
            }
            comboBoxProject.SelectedIndex = selectedProjectIndex;
         }
      }

      private void fillSourceBranchListAndSelect(Branch[] branches)
      {
         comboBoxSourceBranch.Items.AddRange(branches);
         comboBoxSourceBranch.SelectedIndex = 0;
      }

      private void fillTargetBranchListAndSelect(string[] branchNames)
      {
         comboBoxTargetBranch.Items.AddRange(branchNames.ToArray());
         comboBoxTargetBranch.SelectedIndex = 0;
      }

      protected override bool isLoadingCommit() => _isLoadingCommit;

      protected RepositoryAccessor _repositoryAccessor;
      private bool _isLoadingCommit;
      private readonly NewMergeRequestProperties _initialState;
      private readonly IEnumerable<ProjectKey> _projects;
      private readonly string _sourceBranchTemplate;
   }
}

