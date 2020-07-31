using System;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.GitLabClient;

namespace mrHelper.App.src.Forms
{
   internal abstract partial class MergeRequestPropertiesForm : CustomFontForm
   {
      internal MergeRequestPropertiesForm(ProjectAccessor projectAccessor, User currentUser)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _projectAccessor = projectAccessor;
         _currentUser = currentUser;
      }

      internal string Project => getProjectName();
      internal string SourceBranch => getSourceBranch()?.Name;
      internal string TargetBranch => getTargetBranch()?.Name;
      internal string AssigneeUsername => getUserName();
      internal bool DeleteSourceBranch => checkBoxDeleteSourceBranch.Checked;
      internal bool Squash => checkBoxSquash.Checked;
      internal string Title => htmlPanelTitle.Text;
      internal string Description => htmlPanelDescription.Text;

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);

         applyInitialState();
      }

      private void buttonToggleWIP_Click(object sender, EventArgs e)
      {
         string prefix = "WIP: ";
         if (htmlPanelTitle.Text.StartsWith(prefix))
         {
            htmlPanelTitle.Text = htmlPanelTitle.Text.Substring(
               prefix.Length, htmlPanelTitle.Text.Length - prefix.Length);
         }
         else
         {
            htmlPanelTitle.Text = prefix + htmlPanelTitle.Text;
         }
      }

      private void buttonEditTitle_Click(object sender, EventArgs e)
      {
         TextEditForm editTitleForm = new TextEditForm(
            "Edit MR title", htmlPanelTitle.Text, true);
         if (editTitleForm.ShowDialog() == DialogResult.OK)
         {
            htmlPanelTitle.Text = editTitleForm.Body;
         }
      }

      private void buttonEditDescription_Click(object sender, EventArgs e)
      {
         TextEditForm editDescriptionForm = new TextEditForm(
            "Edit MR description", htmlPanelDescription.Text, true);
         if (editDescriptionForm.ShowDialog() == DialogResult.OK)
         {
            htmlPanelDescription.Text = editDescriptionForm.Body;
         }
      }

      protected Branch getSourceBranch()
      {
         return comboBoxSourceBranch.SelectedItem as Branch;
      }

      protected Branch getTargetBranch()
      {
         return comboBoxTargetBranch.SelectedItem as Branch;
      }

      protected string getProjectName()
      {
         return comboBoxProject.SelectedItem as string;
      }

      protected string getUserName()
      {
         return textBoxAssigneeUsername.Text;
      }

      protected RepositoryAccessor createRepositoryAccessor()
      {
         string projectName = getProjectName();
         if (projectName == null)
         {
            return null;
         }

         return _projectAccessor.GetSingleProjectAccessor(projectName).RepositoryAccessor;
      }

      protected void selectBranch(ComboBox comboBox, Func<Branch, bool> predicate)
      {
         if (comboBox.Items.Count == 0)
         {
            return;
         }

         Branch preferredBranch = comboBox.Items.Cast<Branch>().FirstOrDefault(predicate);
         Branch defaultBranch = comboBox.Items.Cast<Branch>().FirstOrDefault(x => x.Name == DefaultBranchName);
         Branch selectedBranch = preferredBranch ?? defaultBranch;
         int selectedBranchIndex = comboBox.Items.IndexOf(selectedBranch);
         if (selectedBranchIndex != -1)
         {
            comboBox.SelectedIndex = selectedBranchIndex;
         }
      }

      protected abstract void applyInitialState();

      private readonly string DefaultBranchName = "master";
      protected readonly User _currentUser;
      protected readonly ProjectAccessor _projectAccessor;
   }
}

