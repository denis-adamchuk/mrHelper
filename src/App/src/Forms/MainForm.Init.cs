using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.DiffTool;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void addCustomActions()
      {
         List<ICommand> commands = Tools.LoadCustomActions(this);
         if (commands == null)
         {
            return;
         }

         int id = 0;
         System.Drawing.Point offSetFromGroupBoxTopLeft = new System.Drawing.Point
         {
            X = 10,
            Y = 17
         };
         System.Drawing.Size typicalSize = new System.Drawing.Size(83, 27);
         foreach (var command in commands)
         {
            string name = command.GetName();
            var button = new System.Windows.Forms.Button
            {
               Name = "customAction" + id,
               Location = offSetFromGroupBoxTopLeft,
               Size = typicalSize,
               Text = name,
               UseVisualStyleBackColor = true,
               Enabled = false,
               TabStop = false
            };
            button.Click += async (x, y) =>
            {
               labelWorkflowStatus.Text = "Command " + name + " is in progress";
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  ExceptionHandlers.Handle(ex, "Custom action failed");
                  MessageBox.Show("Custom action failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  labelWorkflowStatus.Text = "Command " + name + " failed";
                  return;
               }
               labelWorkflowStatus.Text = "Command " + name + " completed";
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      private void loadConfiguration()
      {
         Debug.Assert(_settings.KnownHosts.Count == _settings.KnownAccessTokens.Count);
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }
         List<string> newKnownHosts = new List<string>();
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            string host = _settings.KnownHosts[iKnownHost];

            // Old versions did not have prefix
            if (!host.StartsWith("http://") && !host.StartsWith("https://"))
            {
               host = "https://" + host;
            }
            newKnownHosts.Add(host);

            string accessToken = _settings.KnownAccessTokens[iKnownHost];
            addKnownHost(host, accessToken);
         }
         _settings.KnownHosts = newKnownHosts;
         textBoxLocalGitFolder.Text = _settings.LocalGitFolder;
         checkBoxLabels.Checked = _settings.CheckedLabelsFilter;
         textBoxLabels.Text = _settings.LastUsedLabels;
         checkBoxShowPublicOnly.Checked = _settings.ShowPublicOnly;
         if (comboBoxDCDepth.Items.Contains(_settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = _settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }
         checkBoxMinimizeOnClose.Checked = _settings.MinimizeOnClose;
         fillColorSchemesList();
      }

      private void integrateInTools()
      {
         IIntegratedDiffTool diffTool = new BC3Tool();
         DiffToolIntegration integration = new DiffToolIntegration(new GlobalGitConfiguration());

         try
         {
            integration.Integrate(diffTool);
         }
         catch (Exception ex)
         {
            MessageBox.Show("Diff tool integration failed. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (ex is DiffToolIntegrationException || ex is GitOperationException)
            {
               ExceptionHandlers.Handle(ex,
                  String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()));
               return;
            }
            throw;
         }
      }

      private void loadSettings()
      {
         _settings = new UserDefinedSettings(true);
         loadConfiguration();
         _settings.PropertyChanged += onSettingsPropertyChanged;

         labelTimeTrackingTrackedTime.Text = labelSpentTimeDefaultText;
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         labelWorkflowStatus.Text = String.Empty;
         labelGitStatus.Text = String.Empty;
         this.Text += " (" + Application.ProductVersion + ")";

         bool configured = listViewKnownHosts.Items.Count > 0
                        && textBoxLocalGitFolder.Text.Length > 0;
         if (configured)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else
         {
            tabControl.SelectedTab = tabPageSettings;
         }
      }

      async private Task onApplicationStarted()
      {
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         _workflowManager = new WorkflowManager(_settings);
         _updateManager = new UpdateManager(_settings);
         _discussionManager = new DiscussionManager(_settings);
         _gitClientUpdater = new GitClientInteractiveUpdater();
         _gitClientUpdater.InitializationStatusChange +=
            (status) =>
         {
            labelWorkflowStatus.Text = status;
            labelWorkflowStatus.Update();
         };

         updateHostsDropdownList();

         try
         {
            await initializeWorkflow();
         }
         catch (WorkflowException)
         {
            MessageBox.Show("Cannot initialize the workflow. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}
