using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.Client.Repository;
using mrHelper.Client.Session;

namespace mrHelper.App.src.Forms
{
   public partial class CreateNewMergeRequestForm : CustomFontForm
   {
      public CreateNewMergeRequestForm(ISession session)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _repositoryAccessor = session.GetRepositoryAccessor();
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

      async private Task<IEnumerable<Project>> loadProjectListAsync()
      {
         throw new NotImplementedException();
      }

      async private Task<IEnumerable<Branch>> loadBranchListAsync()
      {
         return await _repositoryAccessor.GetBranches(getProjectName());
      }

      async private Task<Branch> searchTargetBranchAsync()
      {
         return await _repositoryAccessor.FindPreferredTargetBranch(getProjectName(), getSourceBranchName());
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
         if (comboBoxSourceBranch.SelectedIndex == -1)
         {
            return String.Empty;
         }

         return comboBoxSourceBranch.Text;
      }

      private string getProjectName()
      {
         return "";
      }

      private readonly string DefaultTargetBranchName = "master";
      private readonly IRepositoryAccessor _repositoryAccessor;
   }
}

