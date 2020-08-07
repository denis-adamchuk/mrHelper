using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.GitLabClient;
using mrHelper.Common.Interfaces;
using mrHelper.CommonControls.Tools;
using System.Windows.Forms;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Forms
{
   internal class NewMergeRequestForm : MergeRequestPropertiesForm
   {
      internal NewMergeRequestForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         NewMergeRequestProperties initialState, IEnumerable<ProjectKey> projects, string sourceBranchTemplate)
         : base(hostname, projectAccessor, currentUser, allowChangeSource(initialState))
      {
         _initialState = initialState;
         _projects = projects ?? throw new ArgumentException("projects argument cannot be null");
         _sourceBranchTemplate = sourceBranchTemplate ?? String.Empty;

         if (allowChangeSource(_initialState))
         {
            comboBoxProject.SelectedIndexChanged +=
               new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
            comboBoxSourceBranch.SelectedIndexChanged +=
               new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         }

         buttonCancel.ConfirmationText = "Do you want to discard creating a new merge request?";
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         _repositoryAccessor?.Dispose();
      }

      protected override void OnLoad(EventArgs e)
      {
         checkSourceBranchTemplate();
         base.OnLoad(e);
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

         Commit commit = null;
         try
         {
            commit = await loadCommitAsync();
         }
         catch (RepositoryAccessorException ex)
         {
            string message = String.Format("Cannot find a commit for branch {0}", getSourceBranchName());
            ExceptionHandlers.Handle(message, ex);
         }
         await searchTargetBranchNameAsync(commit);
      }

      async private Task loadBranchListAsync()
      {
         Debug.Assert(_sourceBranchTemplate != null);
         onSourceBranchListLoadStart();

         IEnumerable<Branch> branchList = null;;
         try
         {
            branchList = await _repositoryAccessor.GetBranches(_sourceBranchTemplate);
         }
         catch (RepositoryAccessorException ex)
         {
            string message = String.Format("Cannot load a list of branches by template {0}", _sourceBranchTemplate);
            ExceptionHandlers.Handle(message, ex);
         }
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
            fillSourceBranchListAndSelect(branchList.ToArray(), null);
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
         IEnumerable<string> targetBranchNames = null;
         try
         {
            targetBranchNames =
               await _repositoryAccessor.FindPreferredTargetBranchNames(sourceBranch, sourceBranchCommit);
         }
         catch (RepositoryAccessorException ex)
         {
            string message = String.Format("Cannot find a target branch for {0}", sourceBranch.Name);
            ExceptionHandlers.Handle(message, ex);
         }
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
            fillTargetBranchListAndSelect(targetBranchNames.ToArray(), null);
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

         if (allowChangeSource(_initialState))
         {
            fillProjectListAndSelect(_projects, _initialState.DefaultProject);
         }
         else
         {
            fillProjectListAndSelect(new ProjectKey[] { new ProjectKey(_hostname, _initialState.DefaultProject) }, null);
            fillSourceBranchListAndSelect(new Branch[] { new Branch(_initialState.SourceBranch, null) }, null);
            fillTargetBranchListAndSelect(new string[] { _initialState.TargetBranch }, null);

            BeginInvoke(new Action(
               async () =>
               {
                  Debug.Assert(_repositoryAccessor == null);
                  _repositoryAccessor = createRepositoryAccessor();
                  try
                  {
                     Commit commit = await loadCommitAsync();
                  }
                  catch (RepositoryAccessorException ex)
                  {
                     string message = String.Format("Cannot find branch {0} in project {1}",
                        _initialState.SourceBranch, _initialState.DefaultProject);
                     ExceptionHandlers.Handle(message, ex);
                     MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     DialogResult = DialogResult.Cancel;
                     Close();
                  }
               }));
         }

         updateControls();
      }

      private static bool allowChangeSource(NewMergeRequestProperties initialState)
      {
         return String.IsNullOrEmpty(initialState.DefaultProject)
             || String.IsNullOrEmpty(initialState.SourceBranch)
             || String.IsNullOrEmpty(initialState.TargetBranch);
      }

      private void checkSourceBranchTemplate()
      {
         if (String.IsNullOrWhiteSpace(_initialState.SourceBranch) || String.IsNullOrEmpty(_sourceBranchTemplate))
         {
            return;
         }

         Regex re = new Regex(_sourceBranchTemplate, RegexOptions.Compiled | RegexOptions.IgnoreCase);
         if (!re.Match(_initialState.SourceBranch).Success)
         {
            string message = String.Format("Source branch {0} does not match template {1}. Do you want to continue?",
               _initialState.SourceBranch, _sourceBranchTemplate);
            if (MessageBox.Show(message, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
               DialogResult = DialogResult.Cancel;
               Close();
            }
            else
            {
               _sourceBranchTemplate = _initialState.SourceBranch;
            }
         }
      }

      protected override bool isLoadingCommit() => _isLoadingCommit;

      protected RepositoryAccessor _repositoryAccessor;
      private bool _isLoadingCommit;
      private readonly NewMergeRequestProperties _initialState;
      private readonly IEnumerable<ProjectKey> _projects;
      private string _sourceBranchTemplate;
   }
}

