using System;
using GitLabSharp.Entities;
using mrHelper.App.src.Forms;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal class EditMergeRequestPropertiesForm : MergeRequestPropertiesForm
   {
      internal EditMergeRequestPropertiesForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         ProjectKey projectKey, MergeRequest mergeRequest, string specialNote)
         : base(hostname, projectAccessor, currentUser, false)
      {
         if (mergeRequest == null)
         {
            throw new ArgumentException("mergeRequest argument cannot be null");
         }

         _projectKey = projectKey;
         _initialMergeRequest = mergeRequest;
         _specialNote = specialNote;

         buttonSubmit.Text = "Apply";

         buttonCancel.ConfirmationText = "Do you want to discard editing a merge request? Changes will be lost.";
      }

      protected override void applyInitialState()
      {
         if (!String.IsNullOrEmpty(_projectKey.ProjectName))
         {
            addProject(_projectKey.ProjectName);
         }

         if (!String.IsNullOrEmpty(_initialMergeRequest.Source_Branch))
         {
            addSourceBranch(_initialMergeRequest.Source_Branch);
         }

         if (!String.IsNullOrEmpty(_initialMergeRequest.Target_Branch))
         {
            addTargetBranch(_initialMergeRequest.Target_Branch);
         }

         setTitle(String.Empty);
         if (!String.IsNullOrEmpty(_initialMergeRequest.Title))
         {
            setTitle(_initialMergeRequest.Title);
         }

         setDescription(String.Empty);
         if (!String.IsNullOrEmpty(_initialMergeRequest.Description))
         {
            setDescription(_initialMergeRequest.Description);
         }

         setAssigneeUsername(String.Empty);
         if (!String.IsNullOrEmpty(_initialMergeRequest.Assignee?.Username))
         {
            setAssigneeUsername(_initialMergeRequest.Assignee.Username);
         }

         setSpecialNote(String.Empty);
         if (!String.IsNullOrEmpty(_specialNote))
         {
            setSpecialNote(_specialNote);
         }

         checkBoxSquash.Checked = _initialMergeRequest.Squash;
         checkBoxDeleteSourceBranch.Checked = _initialMergeRequest.Force_Remove_Source_Branch;
         updateControls();
      }

      private void addProject(string projectname)
      {
         comboBoxProject.Items.Add(_projectKey.ProjectName);
         comboBoxProject.SelectedIndex = 0;
      }

      private void addSourceBranch(string branchname)
      {
         Branch dummyBranch = new Branch(_initialMergeRequest.Source_Branch, null /* no Commit */);
         comboBoxSourceBranch.Items.Add(dummyBranch);
         comboBoxSourceBranch.SelectedIndex = 0;
      }

      private void addTargetBranch(string branchname)
      {
         comboBoxTargetBranch.Items.Add(_initialMergeRequest.Target_Branch);
         comboBoxTargetBranch.SelectedIndex = 0;
      }

      protected override bool isLoadingCommit() => false;

      private readonly ProjectKey _projectKey;
      private readonly MergeRequest _initialMergeRequest;
      private readonly string _specialNote;
   }
}

