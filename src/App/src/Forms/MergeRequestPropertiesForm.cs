using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.GitLabClient;
using mrHelper.Common.Tools;
using Markdig;

namespace mrHelper.App.src.Forms
{
   internal abstract partial class MergeRequestPropertiesForm : CustomFontForm
   {
      internal MergeRequestPropertiesForm(string hostname, ProjectAccessor projectAccessor, User currentUser)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _hostname = hostname;
         _projectAccessor = projectAccessor;
         _currentUser = currentUser;
         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         string css = String.Format("{0}", mrHelper.App.Properties.Resources.Common_CSS);
         htmlPanelTitle.BaseStylesheet = css;
         htmlPanelDescription.BaseStylesheet = css;
      }

      internal string Project => getProjectName();
      internal string SourceBranch => getSourceBranch()?.Name;
      internal string TargetBranch => getTargetBranch();
      internal string AssigneeUsername => getUserName();
      internal bool DeleteSourceBranch => checkBoxDeleteSourceBranch.Checked;
      internal bool Squash => checkBoxSquash.Checked;
      internal string Title => getTitle();
      internal string Description => getDescription();

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);

         applyInitialState();
      }

      private void buttonToggleWIP_Click(object sender, EventArgs e)
      {
         toggleWIP();
      }

      private void buttonEditTitle_Click(object sender, EventArgs e)
      {
         string title = mrHelper.Common.Tools.StringUtils.ConvertNewlineUnixToWindows(getTitle());
         TextEditForm editTitleForm = new TextEditForm("Edit Merge Request title", title, true);
         if (editTitleForm.ShowDialog() == DialogResult.OK)
         {
            setTitle(Common.Tools.StringUtils.ConvertNewlineWindowsToUnix(editTitleForm.Body));
         }
      }

      private void buttonEditDescription_Click(object sender, EventArgs e)
      {
         string description = mrHelper.Common.Tools.StringUtils.ConvertNewlineUnixToWindows(getDescription());
         TextEditForm editDescriptionForm = new TextEditForm("Edit Merge Request description", description, true);
         if (editDescriptionForm.ShowDialog() == DialogResult.OK)
         {
            setDescription(Common.Tools.StringUtils.ConvertNewlineWindowsToUnix(editDescriptionForm.Body));
         }
      }

      private void comboBoxSourceBranch_Format(object sender, ListControlConvertEventArgs e)
      {
         if (e.ListItem is Branch branch)
         {
            e.Value = branch.Name;
         }
         else if (e.ListItem is string branchName)
         {
            e.Value = branchName;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      protected Branch getSourceBranch()
      {
         return comboBoxSourceBranch.SelectedItem as Branch;
      }

      protected string getTargetBranch()
      {
         return comboBoxTargetBranch.Text;
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

         return _projectAccessor.GetSingleProjectAccessor(projectName).GetRepositoryAccessor();
      }

      protected void setTitle(string title)
      {
         _title = title;
         htmlPanelTitle.Text = convertTextToHtml(title);
      }

      protected void setDescription(string description)
      {
         _description = description;
         htmlPanelDescription.Text = convertTextToHtml(description);
      }

      protected string getTitle()
      {
         return _title;
      }

      protected string getDescription()
      {
         return _description;
      }

      string convertTextToHtml(string text)
      {
         string prefix = StringUtils.GetGitLabAttachmentPrefix(_hostname, getProjectName());
         string html = MarkDownUtils.ConvertToHtml(text, prefix, _mdPipeline);
         return String.Format(MarkDownUtils.HtmlPageTemplate, html);
      }

      private void toggleWIP()
      {
         string prefix = "WIP: ";
         string currentTitle = getTitle();
         string newTitle = currentTitle.StartsWith(prefix)
            ? currentTitle.Substring(prefix.Length, currentTitle.Length - prefix.Length)
            : prefix + currentTitle;
         setTitle(newTitle);
      }

      protected abstract void applyInitialState();

      private readonly string _hostname;
      protected readonly User _currentUser;
      private readonly MarkdownPipeline _mdPipeline;
      protected readonly ProjectAccessor _projectAccessor;
      private string _title;
      private string _description;

   }
}

