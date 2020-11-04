using System;
using System.Windows.Forms;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   public partial class CheckForUpdatesForm : Form
   {
      public CheckForUpdatesForm()
      {
         InitializeComponent();
         this.Text = "mrHelper - Checking for updates";
      }

      internal string NewVersionFilePath => StaticUpdateChecker.NewVersionInformation?.InstallerFilePath;

      async private void CheckForUpdatesForm_Load(object sender, EventArgs e)
      {
         labelStatus.Text = "Checking for updates...";
         buttonUpgradeNow.Enabled = false;

         await StaticUpdateChecker.CheckForUpdatesAsync(Program.ServiceManager);
         if (StaticUpdateChecker.NewVersionInformation != null)
         {
            labelStatus.Text = String.Format("New version {0} is available",
               StaticUpdateChecker.NewVersionInformation.VersionNumber);
            buttonUpgradeNow.Enabled = true;
            buttonUpgradeNow.Focus();
         }
         else
         {
            labelStatus.Text = "New version is not found";
            buttonRemindLater.PerformClick();
         }
      }
   }

   public class RemindAboutUpdateForm : CheckForUpdatesForm
   {
      public RemindAboutUpdateForm()
         : base()
      {
         Text = "mrHelper - Reminder about available update";
      }
   }
}

