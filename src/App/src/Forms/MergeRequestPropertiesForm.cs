﻿using System;
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
      internal MergeRequestPropertiesForm(string hostname, ProjectAccessor projectAccessor, User currentUser,
         bool allowChangeSource)
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
         _allowChangeSource = allowChangeSource;

         string css = String.Format("{0}", mrHelper.App.Properties.Resources.Common_CSS);
         htmlPanelTitle.BaseStylesheet = css;
         htmlPanelDescription.BaseStylesheet = css;

         labelSpecialNotePrefix.Text = Program.ServiceManager.GetSpecialNotePrefix();

         buttonCancel.ConfirmationCondition = () => true;
      }

      internal string ProjectName => getProjectName() ?? String.Empty;
      internal string SourceBranch => getSourceBranch()?.Name ?? String.Empty;
      internal string TargetBranch => getTargetBranch();
      internal string AssigneeUsername => getAssigneeUserName();
      internal bool DeleteSourceBranch => checkBoxDeleteSourceBranch.Checked;
      internal bool Squash => checkBoxSquash.Checked;
      internal string Title => getTitle();
      internal string Description => getDescription();
      internal string SpecialNote => getSpecialNote();

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

      private void textBoxAssigneeUsername_KeyDown(object sender, KeyEventArgs e)
      {
         submitOnKeyDown(e.KeyCode);
      }

      private void textBoxSpecialNote_KeyDown(object sender, KeyEventArgs e)
      {
         submitOnKeyDown(e.KeyCode);
      }

      private void submitOnKeyDown(Keys keyCode)
      {
         if (buttonSubmit.Enabled && keyCode == Keys.Enter)
         {
            buttonSubmit.PerformClick();
         }
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

      protected string getProjectName()
      {
         return comboBoxProject.SelectedItem as string;
      }

      protected Branch getSourceBranch()
      {
         return comboBoxSourceBranch.SelectedItem as Branch;
      }

      protected string getTargetBranch()
      {
         return comboBoxTargetBranch.Text.Trim();
      }

      protected string getTitle()
      {
         return _title;
      }

      protected void setTitle(string title)
      {
         _title = title;
         htmlPanelTitle.Text = convertTextToHtml(title);
         updateControls();
      }

      protected string getDescription()
      {
         return _description;
      }

      protected void setDescription(string description)
      {
         _description = description;
         htmlPanelDescription.Text = convertTextToHtml(description);
         updateControls();
      }

      protected string getAssigneeUserName()
      {
         return convertLabelToWord(textBoxAssigneeUsername.Text);
      }

      protected void setAssigneeUsername(string username)
      {
         textBoxAssigneeUsername.Text = username.Trim();
      }

      protected string getSpecialNote()
      {
         if (String.IsNullOrWhiteSpace(textBoxSpecialNote.Text))
         {
            return String.Empty;
         }
         return labelSpecialNotePrefix.Text + formatSpecialNote(textBoxSpecialNote.Text);
      }

      protected void setSpecialNote(string specialNote)
      {
         if (String.IsNullOrWhiteSpace(specialNote))
         {
            textBoxSpecialNote.Text = String.Empty;
         }
         else if (specialNote.StartsWith(labelSpecialNotePrefix.Text))
         {
            textBoxSpecialNote.Text = specialNote.Substring(labelSpecialNotePrefix.Text.Length);
         }
         else
         {
            textBoxSpecialNote.Text = specialNote;
         }
      }

      protected void toggleWIP()
      {
         string prefix = "WIP: ";
         string title = getTitle();
         string newTitle = title.StartsWith(prefix) ? title.Substring(prefix.Length) : prefix + title;
         setTitle(newTitle);
      }

      protected abstract void applyInitialState();
      protected abstract bool isLoadingCommit();

      protected void updateControls()
      {
         bool isProjectSelected = comboBoxProject.SelectedItem != null;
         comboBoxProject.Enabled = _allowChangeSource;

         bool areSourceBranches = comboBoxSourceBranch.Items.Count > 0;
         comboBoxSourceBranch.Enabled = areSourceBranches && _allowChangeSource;

         bool isSourceBranchSelected = comboBoxSourceBranch.SelectedItem != null;
         comboBoxTargetBranch.Enabled = isSourceBranchSelected;

         bool isTargetBranchSelected = !String.IsNullOrEmpty(comboBoxTargetBranch.Text);
         bool allDetailsLoaded = isProjectSelected && isSourceBranchSelected && isTargetBranchSelected && !isLoadingCommit();
         buttonEditDescription.Enabled = allDetailsLoaded;
         buttonEditTitle.Enabled = allDetailsLoaded;
         buttonToggleWIP.Enabled = allDetailsLoaded;
         checkBoxDeleteSourceBranch.Enabled = allDetailsLoaded;
         checkBoxSquash.Enabled = allDetailsLoaded;
         textBoxAssigneeUsername.Enabled = allDetailsLoaded;

         buttonSubmit.Enabled = allDetailsLoaded && !String.IsNullOrEmpty(getTitle());
      }

      private string convertTextToHtml(string text)
      {
         string prefix = StringUtils.GetGitLabAttachmentPrefix(_hostname, getProjectName());
         string html = MarkDownUtils.ConvertToHtml(text, prefix, _mdPipeline);
         return String.Format(MarkDownUtils.HtmlPageTemplate, html);
      }

      private static string formatSpecialNote(string text)
      {
         // 1. Trim spaces
         string trimmed = text.Trim();
         if (String.IsNullOrEmpty(trimmed))
         {
            return String.Empty;
         }

         // 2. Make sure that all names are preceded with '@'
         trimmed = String.Join(" ", trimmed
            .Replace("@", "")
            .Split(' ')
            .Select(x => Char.IsLetter(x[0]) ? "@" + x : x[0] + "@" + x.Substring(1)));

         // 3. Replace commas with spaces
         return trimmed.Replace(", ", " ").Replace(",", " ").Trim();
      }

      private static string convertLabelToWord(string text)
      {
         string trimmed = text.Trim();
         if (String.IsNullOrEmpty(trimmed))
         {
            return String.Empty;
         }
         return trimmed.StartsWith("@") ? trimmed.Substring(1) : trimmed;
      }

      protected readonly User _currentUser;
      protected readonly ProjectAccessor _projectAccessor;

      private readonly string _hostname;
      private readonly MarkdownPipeline _mdPipeline;
      private readonly bool _allowChangeSource;
      private string _title;
      private string _description;
   }
}

