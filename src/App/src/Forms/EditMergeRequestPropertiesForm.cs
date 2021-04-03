using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal class EditMergeRequestPropertiesForm : MergeRequestPropertiesForm
   {
      internal EditMergeRequestPropertiesForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         ProjectKey projectKey, MergeRequest mergeRequest, string specialNote, IEnumerable<User> users)
         : base(hostname, projectAccessor, currentUser, false, users)
      {
         _projectKey = projectKey;
         _initialMergeRequest = mergeRequest ?? throw new ArgumentException("mergeRequest argument cannot be null");
         _specialNote = specialNote;

         buttonSubmit.Text = "Apply";

         buttonCancel.ConfirmationText = "Do you want to discard editing a merge request? Changes will be lost.";

         Text = "Edit Merge Request";
      }

      protected override void applyInitialState()
      {
         if (!String.IsNullOrEmpty(_projectKey.ProjectName))
         {
            fillProjectListAndSelect(new string[] { _projectKey.ProjectName }, null);
         }

         if (!String.IsNullOrEmpty(_initialMergeRequest.Source_Branch))
         {
            fillSourceBranchListAndSelect(new Branch[] { new Branch(_initialMergeRequest.Source_Branch, null) }, null);
         }

         if (!String.IsNullOrEmpty(_initialMergeRequest.Target_Branch))
         {
            fillTargetBranchListAndSelect(new string[] { _initialMergeRequest.Target_Branch }, null);
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
         checkBoxHighPriority.Checked = _initialMergeRequest.Labels
            .Any(label => label == Constants.HighPriorityLabel);
         updateControls();
      }

      protected override bool isLoadingCommit() => false;

      private readonly ProjectKey _projectKey;
      private readonly MergeRequest _initialMergeRequest;
      private readonly string _specialNote;
   }
}

