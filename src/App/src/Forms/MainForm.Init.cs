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
         catch (WorkflowException ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void subscribeToUpdates()
      {
         _updateManager = new UpdateManager(_workflow, this, _settings);
         _updateManager.OnUpdate +=
            (updates) =>
         {
            BeginInvoke(new Action<List<UpdatedMergeRequest>>(
               async (updatesInternal) =>
               {
                  notifyOnMergeRequestUpdates(updatesInternal);

                  Action<UpdatedMergeRequest, Action<ListViewItem, int>> processUpdatedMergeRequest =
                     (mergeRequest, act) =>
                  {
                     for (int idx = listViewMergeRequests.Items.Count - 1; idx >= 0; --idx)
                     {
                        var item = listViewMergeRequests.Items[idx];
                        FullMergeRequestKey key = (FullMergeRequestKey)item.Tag;

                        if (key.HostName == mergeRequest.HostName
                         && key.Project.Id == mergeRequest.Project.Id
                         && key.MergeRequest.Id == mergeRequest.MergeRequest.Id)
                        {
                           act(item, idx);
                           break; // cannot have the same MR twice in the list
                        }
                     }
                  };

                  bool reloadCurrent = false;
                  bool mightChangeRowHeight = false;
                  bool invalidate = false;
                  foreach (UpdatedMergeRequest mergeRequest in updatesInternal)
                  {
                     switch (mergeRequest.UpdateKind)
                     {
                        case UpdateKind.New:
                           FullMergeRequestKey fmk = new FullMergeRequestKey(
                              mergeRequest.HostName, mergeRequest.Project, mergeRequest.MergeRequest);
                           _allMergeRequests.Add(fmk);
                           addListViewMergeRequestItem(fmk);
                           mightChangeRowHeight = true;
                           invalidate = true;
                           break;

                        case UpdateKind.Closed:
                           _allMergeRequests = _allMergeRequests.Where(
                              x =>
                                 x.HostName != mergeRequest.HostName
                              || x.MergeRequest.IId != mergeRequest.MergeRequest.IId
                              || x.Project.Path_With_Namespace != mergeRequest.Project.Path_With_Namespace).ToList();
                           processUpdatedMergeRequest(mergeRequest,
                              (item, index) => listViewMergeRequests.Items.RemoveAt(index));
                           invalidate = true;
                           break;

                        case UpdateKind.CommitsUpdated:
                           processUpdatedMergeRequest(mergeRequest,
                              (item, index) => reloadCurrent |= item.Selected);
                           break;

                        case UpdateKind.LabelsUpdated:
                           processUpdatedMergeRequest(mergeRequest,
                              (item, index) => setListViewItemTag(item,
                                 mergeRequest.HostName, mergeRequest.Project, mergeRequest.MergeRequest));
                           mightChangeRowHeight = true;
                           invalidate = true;
                           break;
                     }
                  }

                  if (mightChangeRowHeight)
                  {
                     recalcRowHeightForMergeRequestListView(listViewMergeRequests);
                  }

                  if (invalidate)
                  {
                     listViewMergeRequests.Invalidate();
                  }

                  if (reloadCurrent)
                  {
                     Trace.TraceInformation("[MainForm] Reloading current Merge Request");

                     FullMergeRequestKey key = (FullMergeRequestKey)(listViewMergeRequests.SelectedItems[0].Tag);
                     await switchMergeRequestByUserAsync(key.HostName, key.Project, key.MergeRequest.IId);
                  }
               }), updates);
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

