﻿using System;
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
         : base(hostname, projectAccessor, currentUser)
      {
         _projectKey = projectKey;
         _initialMergeRequest = mergeRequest;
         _specialNote = specialNote;

         buttonSubmit.Text = "Apply";

         buttonCancel.ConfirmationText = "Do you want to discard editing a merge request? Changes will be lost.";
      }

      protected override void applyInitialState()
      {
         comboBoxProject.Enabled = false;
         if (!String.IsNullOrEmpty(_projectKey.ProjectName))
         {
            comboBoxProject.Items.Add(_projectKey.ProjectName);
            comboBoxProject.SelectedIndex = 0;
         }

         comboBoxSourceBranch.Enabled = false;
         if (!String.IsNullOrEmpty(_initialMergeRequest.Source_Branch))
         {
            comboBoxSourceBranch.Items.Add(_initialMergeRequest.Source_Branch);
            comboBoxSourceBranch.SelectedIndex = 0;
         }

         comboBoxTargetBranch.Enabled = false;
         if (!String.IsNullOrEmpty(_initialMergeRequest.Target_Branch))
         {
            comboBoxTargetBranch.Items.Add(_initialMergeRequest.Target_Branch);
            comboBoxTargetBranch.SelectedIndex = 0;
         }

         htmlPanelTitle.Text = String.Empty;
         if (!String.IsNullOrEmpty(_initialMergeRequest.Title))
         {
            setTitle(_initialMergeRequest.Title);
         }

         htmlPanelDescription.Text = String.Empty;
         if (!String.IsNullOrEmpty(_initialMergeRequest.Description))
         {
            setDescription(_initialMergeRequest.Description);
         }

         textBoxAssigneeUsername.Text = String.Empty;
         if (!String.IsNullOrEmpty(_initialMergeRequest.Assignee?.Username))
         {
            textBoxAssigneeUsername.Text = _initialMergeRequest.Assignee.Username;
         }

         textBoxSpecialNote.Text = String.Empty;
         if (!String.IsNullOrEmpty(_specialNote))
         {
            setFirstNote(_specialNote);
         }

         checkBoxSquash.Checked = _initialMergeRequest.Squash;
         checkBoxDeleteSourceBranch.Checked = _initialMergeRequest.Force_Remove_Source_Branch;
      }

      protected override bool isLoadingCommit() => false;

      private readonly ProjectKey _projectKey;
      private readonly MergeRequest _initialMergeRequest;
      private readonly string _specialNote;
   }
}

