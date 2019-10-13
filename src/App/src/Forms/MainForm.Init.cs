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
using mrHelper.Core.Git;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void addCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         List<ICommand> commands = null;
         try
         {
            string CustomActionsFileName = "CustomActions.xml";
            commands = loader.LoadCommands(CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle(ex, "Cannot load custom actions");
         }

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
         System.Drawing.Size typicalSize = new System.Drawing.Size(96, 32);
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
         Text = Common.Constants.Constants.MainWindowCaption + " (" + Application.ProductVersion + ")";

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
         if (!System.IO.File.Exists(Common.Constants.Constants.ProjectListFileName))
         {
            MessageBox.Show(String.Format("Cannot find {0} file. Current version cannot run without it.",
               Common.Constants.Constants.ProjectListFileName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
         }

         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         _serviceManager = new Client.Services.ServiceManager();
         _persistentStorage = new PersistentStorage();
         _persistentStorage.OnSerialize += (writer) => onPersistentStorageSerialize(writer);
         _persistentStorage.OnDeserialize += (reader) => onPersistentStorageDeserialize(reader);

         _discussionManager = new DiscussionManager(_settings);
         _gitClientUpdater = new GitClientInteractiveUpdater();
         _gitClientUpdater.InitializationStatusChange +=
            (status) =>
         {
            labelWorkflowStatus.Text = status;
            labelWorkflowStatus.Update();
         };

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

         try
         {
            _persistentStorage.Deserialize();
         }
         catch (PersistenceStateDeserializationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot deserialize the state");
         }

         updateHostsDropdownList();

         try
         {
            string[] arguments = Environment.GetCommandLineArgs();
            string url = arguments.Length > 1 ? arguments[1] : String.Empty;

            if (url != String.Empty)
            {
               await connectToUrlAsync(url);
            }
            else
            {
               selectHost(PreferredSelection.Initial);
               await switchHostToSelected();
            }
         }
         catch (WorkflowException ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void subscribeToUpdates()
      {
         _updateManager = new UpdateManager(_workflow, this, _settings);
         _updateManager.OnUpdate += (updates) => processUpdatesAsync(updates);
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

