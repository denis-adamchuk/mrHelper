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
using mrHelper.Client.Persistence;

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

               Trace.TraceInformation(String.Format("Custom action {0} completed", name));

               // TODO This may be unneeded in general case but so far it is ok for current list of custom actions
               await onStopTimer(true);
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      private void loadConfiguration()
      {
         Trace.TraceInformation("[MainForm] Loading configuration");

         Debug.Assert(_settings.KnownHosts.Count == _settings.KnownAccessTokens.Count);
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = getHostWithPrefix(_settings.KnownHosts[iKnownHost]);
            string accessToken = _settings.KnownAccessTokens[iKnownHost];
            addKnownHost(host, accessToken);
            newKnownHosts.Add(host);
         }
         _settings.KnownHosts = newKnownHosts;

         if (_settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            _settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         textBoxLocalGitFolder.Text = _settings.LocalGitFolder;
         checkBoxLabels.Checked = _settings.CheckedLabelsFilter;
         textBoxLabels.Text = _settings.LastUsedLabels;
         checkBoxShowPublicOnly.Checked = _settings.ShowPublicOnly;
         checkBoxMinimizeOnClose.Checked = _settings.MinimizeOnClose;

         if (comboBoxDCDepth.Items.Contains(_settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = _settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }

         Trace.TraceInformation("[MainForm] Configuration loaded");
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
         _settings.PropertyChanged += onSettingsPropertyChanged;
         loadConfiguration();

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

         _persistentStorage = new PersistentStorage();
         _persistentStorage.OnSerialize += (writer) => onPersistentStorageSerialize(writer);
         _persistentStorage.OnDeserialize += (reader) => onPersistentStorageDeserialize(reader);

         _workflowFactory = new WorkflowFactory(_settings, _persistentStorage);
         _discussionManager = new DiscussionManager(_settings);
         _gitClientUpdater = new GitClientInteractiveUpdater();
         _gitClientUpdater.InitializationStatusChange +=
            (status) =>
         {
            labelWorkflowStatus.Text = status;
            labelWorkflowStatus.Update();
         };

         updateHostsDropdownList();

         createWorkflow();

         // Expression resolver requires Workflow 
         _expressionResolver = new ExpressionResolver(_workflow);

         // Color Scheme requires Expression Resolver
         fillColorSchemesList();
         initializeColorScheme();

         // Update manager indirectly subscribes to Workflow
         subscribeToUpdates();

         // Time Tracking Manager requires Workflow
         createTimeTrackingManager();

         // Now we can de-serialize the persistence state, Workflow subscribed to Storage callbacks
         try
         {
            _persistentStorage.Deserialize();
         }
         catch (PersistenceStateDeserializationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot deserialize the state");
         }

         try
         {
            // Connect
            await initializeWorkflow();
         }
         catch (WorkflowException)
         {
            MessageBox.Show("Cannot initialize the workflow. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
         }

         if (!await _updateManager.InitializeAsync())
         {
            MessageBox.Show("Cannot initialize Update Manager. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
         }
      }

      private void subscribeToUpdates()
      {
         _updateManager = new UpdateManager(_workflow, this, _settings);
         _updateManager.OnUpdate += async (updates) =>
         {
            notifyOnMergeRequestUpdates(updates);

            if (_workflow.State.Project.Id == default(Project).Id)
            {
               // state changed 
               return;
            }

            // TODO This should use ProjectKey instead of ProjectId
            // check if currently selected project is affected by update

            // Below conditions are commented out to reload lists on each update
            // This is needed to update merge request colors due to changed labels.
            // This works around imperfect comparison logic inside WorkflowDetialsChecker.
            // TODO Change WorkflowDetailsChecker comparison logic to have merge requests
            // with changed labels among UpdatedMergeRequests

            //if (updates.NewMergeRequests.Any(x => x.Project_Id == _workflow.State.Project.Id)
            // || updates.UpdatedMergeRequests.Any(x => x.Project_Id == _workflow.State.Project.Id)
            // || updates.ClosedMergeRequests.Any(x => x.Project_Id == _workflow.State.Project.Id))
            {
               // emulate project change to reload merge request list
               // This will automatically update commit list (if there are new ones).
               // This will also remove closed merge requests from the list.
               Trace.TraceInformation("[MainForm] Emulating project change to reload merge request list");

               try
               {
                  await _workflow.SwitchProjectAsync(_workflow.State.Project.Path_With_Namespace);
               }
               catch (WorkflowException ex)
               {
                  ExceptionHandlers.Handle(ex, "Workflow error occurred during auto-update");
               }
            }
         };
      }

      private void createTimeTrackingManager()
      {
         _timeTrackingManager = new TimeTrackingManager(_settings, _workflow);
         _timeTrackingManager.PreLoadTotalTime += () => onLoadTotalTime();
         _timeTrackingManager.PostLoadTotalTime += (e) => onTotalTimeLoaded(e);
         _timeTrackingManager.FailedLoadTotalTime += () => onFailedLoadTotalTime();
      }
   }
}

