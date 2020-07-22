using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.Client.Projects;
using mrHelper.Client.Repository;

namespace mrHelper.App.src.Forms
{
   public partial class CreateNewMergeRequestForm : CustomFontForm
   {
      public CreateNewMergeRequestForm(IProjectAccessor projectAccessor)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _projectAccessor = projectAccessor;
      }

      async private void CreateNewMergeRequestForm_Load(object sender, EventArgs e)
      {
         onProjectListLoadStart();
         IEnumerable<Project> projects = await loadProjectListAsync();
         onProjectListLoadFinish(projects);
      }

      async private void comboBoxProject_SelectedIndexChanged(object sender, EventArgs e)
      {
         onBranchListLoadStart();
         IEnumerable<Branch> branchList = await loadBranchListAsync();
         onBranchListLoadFinish(branchList);
      }

      async private void comboBoxSourceBranch_SelectedIndexChanged(object sender, System.EventArgs e)
      {
         _repositoryAccessor.Cancel();

         onTargetBranchSearchStart();
         Branch targetBranch = await searchTargetBranchAsync();
         onTargetBranchSearchFinish(targetBranch?.Name);
      }

      private Task<IEnumerable<Project>> loadProjectListAsync()
      {
         return _projectAccessor.GetProjects();
      }

      async private Task<IEnumerable<Branch>> loadBranchListAsync()
      {
         Debug.Assert(_repositoryAccessor != null);
         return await _repositoryAccessor.GetBranches();
      }

      async private Task<Branch> searchTargetBranchAsync()
      {
         Debug.Assert(_repositoryAccessor != null);
         return await _repositoryAccessor.FindPreferredTargetBranch(getSourceBranchName());
      }

      private void onProjectListLoadStart()
      {
         comboBoxProject.Enabled = false;
         comboBoxSourceBranch.Enabled = false;
         comboBoxTargetBranch.Enabled = false;
      }

      private void onProjectListLoadFinish(IEnumerable<Project> projectList)
      {
         comboBoxProject.Enabled = true;
         comboBoxSourceBranch.Enabled = true;
         comboBoxTargetBranch.Enabled = true;

         string[] projectArray = projectList.Select(x => x.Path_With_Namespace).ToArray();
         comboBoxProject.Items.AddRange(projectArray);

         selectTargetBranch(DefaultTargetBranchName);
      }

      private void onBranchListLoadStart()
      {
         comboBoxProject.Enabled = false;
         comboBoxSourceBranch.Enabled = false;
         comboBoxTargetBranch.Enabled = false;

         createRepositoryAccessor();
      }

      private void onBranchListLoadFinish(IEnumerable<Branch> branchList)
      {
         comboBoxProject.Enabled = true;
         comboBoxSourceBranch.Enabled = true;
         comboBoxTargetBranch.Enabled = true;

         string[] branchArray = branchList.Select(x => x.Name).ToArray();
         comboBoxSourceBranch.Items.AddRange(branchArray);
         comboBoxTargetBranch.Items.AddRange(branchArray);

         selectTargetBranch(DefaultTargetBranchName);
      }

      private void onTargetBranchSearchStart()
      {
         comboBoxProject.Enabled = false;
         comboBoxSourceBranch.Enabled = false;
         comboBoxTargetBranch.Enabled = false;
      }

      private void onTargetBranchSearchFinish(string targetBranchName)
      {
         comboBoxProject.Enabled = true;
         comboBoxSourceBranch.Enabled = true;
         comboBoxTargetBranch.Enabled = true;

         selectTargetBranch(targetBranchName);
      }

      private void selectTargetBranch(string name)
      {
         int index = comboBoxTargetBranch.Items.IndexOf(name);
         int masterIndex = comboBoxTargetBranch.Items.IndexOf(DefaultTargetBranchName);
         comboBoxTargetBranch.SelectedIndex = index == -1 ? masterIndex : index;
      }

      private string getSourceBranchName()
      {
         return comboBoxSourceBranch.SelectedIndex == -1 ? String.Empty : comboBoxSourceBranch.Text;
      }

      private string getProjectName()
      {
         return comboBoxProject.SelectedIndex == -1 ? String.Empty : comboBoxProject.Text;
      }

      private void createRepositoryAccessor()
      {
         _repositoryAccessor = _projectAccessor.GetSingleProjectAccessor(getProjectName()).RepositoryAccessor;
      }

      private readonly string DefaultTargetBranchName = "master";
      private readonly IProjectAccessor _projectAccessor;
      private IRepositoryAccessor _repositoryAccessor;
   }
}

