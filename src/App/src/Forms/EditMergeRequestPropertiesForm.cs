using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.src.Forms;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal class EditMergeRequestPropertiesForm : MergeRequestPropertiesForm
   {
      internal EditMergeRequestPropertiesForm(ProjectAccessor projectAccessor, User currentUser,
         ProjectKey projectKey, MergeRequest mergeRequest)
         : base(projectAccessor, currentUser)
      {
         _projectKey = projectKey;
         _initialMergeRequest = mergeRequest;
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         _repositoryAccessor?.Dispose();
      }

      protected override void applyInitialState()
      {
         comboBoxProject.Text = _projectKey.ProjectName ?? String.Empty;
         comboBoxProject.Enabled = false;

         comboBoxSourceBranch.Text = _initialMergeRequest.Source_Branch ?? String.Empty;
         comboBoxSourceBranch.Enabled = false;

         // target branch is selected after the list is loaded (async)

         htmlPanelTitle.Text = _initialMergeRequest.Title ?? String.Empty;
         htmlPanelDescription.Text = _initialMergeRequest.Description ?? String.Empty;

         textBoxAssigneeUsername.Text = _initialMergeRequest.Assignee?.Username ?? String.Empty;

         checkBoxSquash.Checked = _initialMergeRequest.Squash;
         checkBoxDeleteSourceBranch.Checked = _initialMergeRequest.Should_Remove_Source_Branch;

         BeginInvoke(new Action(async () => await loadBranchListAsync()));
      }

      async private Task loadBranchListAsync()
      {
         _repositoryAccessor = createRepositoryAccessor();
         IEnumerable<Branch> branchList = await _repositoryAccessor.GetBranches();

         Branch[] branchArray = branchList.ToArray();
         comboBoxTargetBranch.Items.AddRange(branchArray);

         selectBranch(comboBoxTargetBranch, x => x.Name == _initialMergeRequest.Target_Branch);
         if (comboBoxTargetBranch.SelectedIndex == -1)
         {
            Debug.Assert(false); // cannot find even "master"
            //MessageBox.Show("Invalid Target Branch",
            //   String.Format("Cannot find target branch {0} at GitLab", _initialMergeRequest.Target_Branch),
            //      MessageBoxButtons.OK, MessageBoxIcon.Warning);
         }
      }

      private readonly ProjectKey _projectKey;
      private readonly MergeRequest _initialMergeRequest;
      private RepositoryAccessor _repositoryAccessor;
   }
}

