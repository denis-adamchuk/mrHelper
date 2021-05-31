using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class ConfigureNotificationsForm : CustomFontForm
   {
      public ConfigureNotificationsForm(IEnumerable<string> keywords)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         applyFont(Program.Settings.MainWindowFontSizeName);

         load();

         if (keywords == null)
         {
            checkBoxShowKeywords.Enabled = false;
         }
         else
         {
            checkBoxShowKeywords.Text = "Keywords: " + String.Join(", ", keywords);
         }
      }

      private void buttonOK_Click(object sender, EventArgs e)
      {
         save();
      }

      private void load()
      {
         checkBoxShowNewMergeRequests.Checked = Program.Settings.Notifications_NewMergeRequests;
         checkBoxShowMergedMergeRequests.Checked = Program.Settings.Notifications_MergedMergeRequests;
         checkBoxShowUpdatedMergeRequests.Checked = Program.Settings.Notifications_UpdatedMergeRequests;
         checkBoxShowResolvedAll.Checked = Program.Settings.Notifications_AllThreadsResolved;
         checkBoxShowOnMention.Checked = Program.Settings.Notifications_OnMention;
         checkBoxShowKeywords.Checked = Program.Settings.Notifications_Keywords;
         checkBoxShowMyActivity.Checked = Program.Settings.Notifications_MyActivity;
         checkBoxShowServiceNotifications.Checked = Program.Settings.Notifications_Service;
      }

      private void save()
      {
         Program.Settings.Notifications_NewMergeRequests = checkBoxShowNewMergeRequests.Checked;
         Program.Settings.Notifications_MergedMergeRequests = checkBoxShowMergedMergeRequests.Checked;
         Program.Settings.Notifications_UpdatedMergeRequests = checkBoxShowUpdatedMergeRequests.Checked;
         Program.Settings.Notifications_AllThreadsResolved = checkBoxShowResolvedAll.Checked;
         Program.Settings.Notifications_OnMention = checkBoxShowOnMention.Checked;
         Program.Settings.Notifications_Keywords = checkBoxShowKeywords.Checked;
         Program.Settings.Notifications_MyActivity = checkBoxShowMyActivity.Checked;
         Program.Settings.Notifications_Service = checkBoxShowServiceNotifications.Checked;
      }
   }
}

