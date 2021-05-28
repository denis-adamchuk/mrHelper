using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Win32;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;
using mrHelper.CommonControls.Tools;
using System.Drawing;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      internal MainForm(bool startMinimized, bool runningAsUwp, string startUrl, bool integratedInGitExtensions,
         bool integratedInSourceTree)
      {
         _startUrl = startUrl;
         _allowAutoStartApplication = !runningAsUwp;
         _startMinimized = startMinimized;
         _integratedInGitExtensions = integratedInGitExtensions;
         _integratedInSourceTree = integratedInSourceTree;
         createSharedCollections();

         WinFormsHelpers.FixNonStandardDPIIssue(this, (float)Constants.FontSizeChoices["Design"]);
         _loadingConfiguration = true;
         InitializeComponent();
         _loadingConfiguration = false;
         WinFormsHelpers.LogScaleDimensions(this);
         WinFormsHelpers.LogScreenResolution(this);

         _trayIcon = new TrayIcon(notifyIcon);
         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
         SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
         _applicationUpdateChecker = new PeriodicUpdateChecker(this);

         _colorScheme = new ColorScheme();
         _colorScheme.Changed += onColorSchemeChanged;
      }

      private void createSharedCollections()
      {
         _recentMergeRequests =
            new DictionaryWrapper<MergeRequestKey, DateTime>(new Dictionary<MergeRequestKey, DateTime>(), saveState);
         _reviewedRevisions =
            new DictionaryWrapper<MergeRequestKey, HashSet<string>>(new Dictionary<MergeRequestKey, HashSet<string>>(), saveState);
         _lastMergeRequestsByHosts =
            new DictionaryWrapper<string, MergeRequestKey>(new Dictionary<string, MergeRequestKey>(), saveState);
         _newMergeRequestDialogStatesByHosts =
            new DictionaryWrapper<string, Helpers.NewMergeRequestProperties>(new Dictionary<string, Helpers.NewMergeRequestProperties>(), saveState);
      }

      private void initializeWork()
      {
         restoreState();
         prepareFormToStart();

         reconnect(_startUrl);
      }

      private void finalizeWork()
      {
         _requestedDiff.Clear();
         _requestedUrl.Clear();

         saveState();
         Interprocess.SnapshotSerializer.CleanUpSnapshots();

         Trace.TraceInformation("[MainForm] Work finalized.");
      }

      private void restoreState()
      {
         _persistentStorage = new PersistentStorage();
         _persistentStorage.OnSerialize += onPersistentStorageSerialize;
         _persistentStorage.OnDeserialize += onPersistentStorageDeserialize;

         try
         {
            _persistentStorage.Deserialize();
         }
         catch (PersistenceStateDeserializationException ex)
         {
            ExceptionHandlers.Handle("Cannot deserialize the state", ex);
         }
      }

      private void saveState()
      {
         try
         {
            _persistentStorage?.Serialize();
         }
         catch (PersistenceStateSerializationException ex)
         {
            ExceptionHandlers.Handle("Cannot serialize the state", ex);
         }
      }

      private void prepareFormToStart()
      {
         subscribeToApplicationUpdatesAndRequestThem();
         initializeKeywords();
         setControlStateFromConfiguration();
         applyAutostartSetting(Program.Settings.RunWhenWindowsStarts);
         disableSSLVerification();
         updateCaption();
         upgradeHostList();
         prepareControlsToStart();
         prepareSizeToStart();
      }

      private void prepareControlsToStart()
      {
         prepareToolBar();
         prepareStatusBarControls();
         startClipboardCheckTimer();
         startNewVersionReminderTimer();
         subscribeToNewVersionReminderTimer();
         subscribeToConnectionLossBlinkingTimer();
         updateNewVersionStatus();

         _timeTrackingTimer.Tick += new System.EventHandler(onTimeTrackingTimer);
      }

      private void prepareToolBar()
      {
         // This positions menu strip at the top and toolbars
         // at the next row in the following order: Hosts - Actions - Custom Actions
         toolStripCustomActions.Location = new System.Drawing.Point(0, 0);
         toolStripActions.Location = new System.Drawing.Point(0, 0);
         toolStripHosts.Location = new System.Drawing.Point(0, 0);
         menuStrip1.Location = new System.Drawing.Point(0, 0);

         // Minimize gaps between controls
         toolStripActions.Location = new Point(
            toolStripHosts.Location.X + toolStripHosts.Width + 5,
            toolStripActions.Location.Y);
         toolStripCustomActions.Location = new Point(
            toolStripActions.Location.X + toolStripActions.Width + 5,
            toolStripCustomActions.Location.Y);
      }

      private void prepareStatusBarControls()
      {
         labelStorageStatus.Text = String.Empty;
         labelOperationStatus.Text = String.Empty;
         labelConnectionStatus.Text = String.Empty;
         linkLabelAbortGitClone.Visible = false;
      }

      private void prepareSizeToStart()
      {
         Trace.TraceInformation("[MainForm] prepareSizeToStart() _startMinimized={0}", _startMinimized);

         if (_startMinimized)
         {
            WindowState = FormWindowState.Minimized;
            _restoreSizeOnNextRestore = true;
         }
         else
         {
            restoreSize();
         }
      }

      private void restoreSize()
      {
         if (Program.Settings.WidthBeforeClose != 0 && Program.Settings.HeightBeforeClose != 0)
         {
            Size = new Size(Program.Settings.WidthBeforeClose, Program.Settings.HeightBeforeClose);
            Size = new Size(Program.Settings.WidthBeforeClose, Program.Settings.HeightBeforeClose);
         }

         if (Program.Settings.LeftBeforeClose != 0 && Program.Settings.TopBeforeClose != 0)
         {
            int screenWidth = Screen.GetWorkingArea(this).Width;
            int screenHeight = Screen.GetWorkingArea(this).Height;
            double minAllowedVisibleAreaPx = 0.20; // 20%
            int maxX = screenWidth - Convert.ToInt32(screenWidth * minAllowedVisibleAreaPx);
            int maxY = screenHeight - Convert.ToInt32(screenHeight * minAllowedVisibleAreaPx);
            int newX = Math.Max(0, Math.Min(maxX, Program.Settings.LeftBeforeClose));
            int newY = Math.Max(0, Math.Min(maxY, Program.Settings.TopBeforeClose));
            Location = new Point(newX, newY);
         }

         if (Program.Settings.WasMaximizedBeforeClose)
         {
            WindowState = FormWindowState.Maximized;
         }
         else if (Program.Settings.WasMinimizedBeforeClose)
         {
            WindowState = FormWindowState.Minimized;
         }

         Trace.TraceInformation(
            "[MainForm] restoreSize(), New Size = {0}x{1}, New Location = {2}x{3}, WindowState={4}",
            Size.Width, Size.Height, Location.X, Location.Y, WindowState.ToString());
      }

      private static void disableSSLVerification()
      {
         if (Program.Settings.DisableSSLVerification)
         {
            try
            {
               GitTools.DisableSSLVerification();
               Program.Settings.DisableSSLVerification = false;
            }
            catch (GitTools.SSLVerificationDisableException ex)
            {
               ExceptionHandlers.Handle("Cannot disable SSL verification", ex);
            }
         }
      }

      private void addFontSizes()
      {
         foreach (string fontSizeChoice in Constants.MainWindowFontSizeChoices)
         {
            ToolStripMenuItem item = new ToolStripMenuItem
            {
               Name = "fontSize" + fontSizeChoice,
               Text = fontSizeChoice,
               CheckOnClick = true,
               Checked = false
            };
            item.CheckedChanged += onFontMenuItemCheckedChanged;
            fontSizeToolStripMenuItem.DropDownItems.Add(item);
         }

         var fontItems = fontSizeToolStripMenuItem.DropDownItems.Cast<ToolStripMenuItem>();
         if (!fontItems.Any())
         {
            return;
         }

         var preferredItem = fontItems
            .FirstOrDefault(item => item.Text == Program.Settings.MainWindowFontSizeName);
         if (preferredItem == null)
         {
            preferredItem = fontItems
               .FirstOrDefault(item => item.Text == Constants.DefaultMainWindowFontSizeChoice);
         }

         if (preferredItem == null)
         {
            preferredItem = fontItems.First();
         }

         preferredItem.Checked = true;
      }

      private void onFontMenuItemCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(fontSizeToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyFontChange();
      }

      private void initializeKeywords()
      {
         string filepath = Path.Combine(
            Directory.GetCurrentDirectory(), Constants.KeywordsFileName);
         if (System.IO.File.Exists(filepath))
         {
            try
            {
               _keywords = JsonUtils.LoadFromFile<string[]>(filepath);
            }
            catch (Exception ex) // Any exception from JsonUtils.LoadFromFile()
            {
               ExceptionHandlers.Handle("Cannot load keywords from file", ex);
            }
         }
      }

      private void startClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Tick += new EventHandler(onClipboardCheckingTimer);
         _clipboardCheckingTimer.Start();
      }

      private void stopClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Stop();
      }

      private void subscribeToConnectionLossBlinkingTimer()
      {
         _connectionLossBlinkingTimer.Tick += new EventHandler(onConnectionLossBlinkingTimer);
      }

      private void subscribeToNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Tick += (s, e) => remindAboutNewVersion();
      }

      private void startNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Start();
      }

      private void stopNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Stop();
      }

      private void subscribeToApplicationUpdatesAndRequestThem()
      {
         _applicationUpdateChecker.NewVersionAvailable += onNewVersionAvailable;
      }

      private void unsubscribeFromApplicationUpdates()
      {
         _applicationUpdateChecker.NewVersionAvailable -= onNewVersionAvailable;
      }

      private void onColorSchemeChanged()
      {
         getConnectionPages()?
            .ToList()
            .ForEach(connectionPage => onSummaryColorChanged(connectionPage));
      }
   }
}

