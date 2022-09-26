using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.StorageSupport;

namespace mrHelper.App.Forms
{
   internal partial class ConfigureStorageForm : CustomFontForm
   {
      public ConfigureStorageForm()
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         applyFont(Program.Settings.MainWindowFontSizeName);

         checkHelpAvailability();
         loadGitUsageMode();
         textBoxStorageFolder.Text = Program.Settings.LocalStorageFolder;
      }

      internal bool Changed { get; private set; }

      private void buttonOK_Click(object sender, EventArgs e)
      {
         Changed = false;
         Changed |= saveGitUsageMode();
         Changed |= saveStorageFolder();
      }

      private void buttonBrowseStorageFolder_Click(object sender, EventArgs e)
      {
         launchStorageFolderChangeDialog();
      }

      private void linkLabelCommitStorageDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         showHelp();
      }

      private void checkHelpAvailability()
      {
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         bool isHelpAvailable = helpUrl != String.Empty;
         if (isHelpAvailable)
         {
            toolTip.SetToolTip(linkLabelCommitStorageDescription, helpUrl);
         }
         linkLabelCommitStorageDescription.Visible = isHelpAvailable;
      }

      private static void showHelp()
      {
         Trace.TraceInformation("[ConfigureStorageForm] Clicked on link label for commit storage selection");
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         if (helpUrl != String.Empty)
         {
            Common.Tools.UrlHelper.OpenBrowser(helpUrl);
         }
      }

      private void loadGitUsageMode()
      {
         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         switch (type)
         {
            case LocalCommitStorageType.FileStorage:
               radioButtonDontUseGit.Checked = true;
               break;

            case LocalCommitStorageType.FullGitRepository:
               radioButtonUseGitFullClone.Checked = true;
               break;

            case LocalCommitStorageType.ShallowGitRepository:
               radioButtonUseGitShallowClone.Checked = true;
               break;
         }
      }

      private bool saveGitUsageMode()
      {
         LocalCommitStorageType type = radioButtonDontUseGit.Checked
            ? LocalCommitStorageType.FileStorage
            : (radioButtonUseGitFullClone.Checked
               ? LocalCommitStorageType.FullGitRepository
               : LocalCommitStorageType.ShallowGitRepository);
         if (type != ConfigurationHelper.GetPreferredStorageType(Program.Settings))
         {
            ConfigurationHelper.SelectPreferredStorageType(Program.Settings, type);
            return true;
         }
         return false;
      }

      private bool saveStorageFolder()
      {
         if (textBoxStorageFolder.Text != Program.Settings.LocalStorageFolder)
         {
            Program.Settings.LocalStorageFolder = textBoxStorageFolder.Text;
            return true;
         }
         return false;
      }

      private void launchStorageFolderChangeDialog()
      {
         storageFolderBrowser.SelectedPath = textBoxStorageFolder.Text;
         if (storageFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            textBoxStorageFolder.Text = storageFolderBrowser.SelectedPath;
         }
      }
   }
}
