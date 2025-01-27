using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.App.Controls;
using mrHelper.CustomActions;
using mrHelper.CommonControls.Tools;
using mrHelper.App.Interprocess;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      // Time Tracking

      private bool isTrackingTime()
      {
         return _timeTracker != null;
      }

      private void startTimeTrackingTimer()
      {
         Debug.Assert(!isTrackingTime());

         // Start timer
         _timeTrackingTimer.Start();

         // Reset and start stopwatch
         _timeTracker = createTimeTracker();
         if (_timeTracker == null)
         {
            return;
         }

         _timeTracker.Start();

         onTimerStarted();
      }

      private ITimeTracker createTimeTracker()
      {
         return getCurrentConnectionPage()?.CreateTimeTracker();
      }

      private void onTimeTrackingTimer(object sender, EventArgs e)
      {
         if (isTrackingTime())
         {
            toolStripTextBoxTrackedTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
            toolStripTextBoxTrackedTime.Invalidate();
         }
      }

      private void pauseTimeTrackingTimer()
      {
         if (isTrackingTime())
         {
            _timeTracker.Pause();
         }
      }

      private void resumeTimeTrackingTimer()
      {
         if (isTrackingTime())
         {
            _timeTracker.Resume();
         }
      }

      private void stopTimeTrackingTimer()
      {
         BeginInvoke(new Action(async () => await stopTimeTrackingTimerAsync()));
      }

      async private Task stopTimeTrackingTimerAsync()
      {
         if (!isTrackingTime())
         {
            return;
         }

         // Stop timer
         _timeTrackingTimer.Stop();

         // Reset member right now to not send tracked time again on re-entrance
         ITimeTracker timeTracker = _timeTracker;
         _timeTracker = null;

         string convertSpanToText(TimeSpan span) => String.Format("{0}h {1}m {2}s",
               span.ToString("hh"), span.ToString("mm"), span.ToString("ss"));

         addOperationRecord("Sending tracked time has started");
         try
         {
            TimeSpan span = await timeTracker.Stop();
            string duration = convertSpanToText(span);
            addOperationRecord(String.Format("Tracked time {0} sent successfully", duration));
         }
         catch (ForbiddenTimeTrackerException ex)
         {
            TimeSpan span = ex.TrackedTime;
            string duration = convertSpanToText(span);
            string status = String.Format(
               "Cannot report tracked time ({0}).\r\n"
             + "You don't have permissions to track time in {1} project.\r\n"
             + "Please contact {2} administrator or SCM team.",
               duration, timeTracker.MergeRequest.ProjectKey.ProjectName, timeTracker.MergeRequest.ProjectKey.HostName);
            MessageBox.Show(status, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            addOperationRecord(String.Format("Tracked time is not set. Set up permissions and report {0} manually",
               duration));
         }
         catch (TooSmallSpanTimeTrackerException)
         {
            addOperationRecord("Tracked time less than 1 second is ignored");
         }
         catch (TimeTrackerException ex)
         {
            TimeSpan span = ex.TrackedTime;
            string duration = convertSpanToText(span);
            string status = String.Format("Error occurred. Tracked time {0} is not sent", duration);
            ExceptionHandlers.Handle(status, ex);
            MessageBox.Show(status, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         if (!isTrackingTime())
         {
            onTimerStopped();
         }
      }

      private void cancelTimeTrackingTimer()
      {
         if (!isTrackingTime())
         {
            return;
         }

         // Stop timer
         _timeTrackingTimer.Stop();

         _timeTracker.Cancel();
         _timeTracker = null;
         addOperationRecord("Time tracking cancelled");

         onTimerStopped();
      }

      private void onTimerStarted()
      {
         _timeTrackingHost = getCurrentConnectionPage()?.GetCurrentHostName();
         toolStripButtonGoToTimeTracking.Enabled = true;
         toolStripButtonEditTrackedTime.Enabled = false;
         toolStripButtonCancelTimer.Enabled = true;
         toolStripTextBoxTrackedTime.Text = DefaultTimeTrackingTextBoxText;
         toolStripButtonStartStopTimer.Image = Properties.Resources.stop_100x100;
         pullCustomActionToolBar();

         addOperationRecord("Time tracking has started");
         onSummaryColorChanged(getCurrentConnectionPage());
         Invalidate(true);
      }

      private void onTimerStopped()
      {
         _timeTrackingHost = null;
         ConnectionPage connectionPage = getCurrentConnectionPage();
         toolStripButtonGoToTimeTracking.Enabled = false;
         toolStripButtonEditTrackedTime.Enabled = connectionPage != null && connectionPage.CanTrackTime();
         toolStripButtonCancelTimer.Enabled = false;
         toolStripTextBoxTrackedTime.Text = connectionPage?.GetTrackedTimeAsText() ?? DefaultTimeTrackingTextBoxText;
         toolStripButtonStartStopTimer.Image = Properties.Resources.play_100x100;
         toolStripButtonStartStopTimer.Enabled = connectionPage != null && connectionPage.CanTrackTime();
         pullCustomActionToolBar();

         onSummaryColorChanged(getCurrentConnectionPage());
         Invalidate(true);

         Debug.Assert(!_applicationUpdateNotificationPostponedTillTimerStop
                   || !_applicationUpdateReminderPostponedTillTimerStop); // cannot have both enabled
         if (_applicationUpdateNotificationPostponedTillTimerStop)
         {
            notifyAboutNewVersion();
         }
         else if (_applicationUpdateReminderPostponedTillTimerStop)
         {
            remindAboutNewVersion();
         }
      }

      private void gotoTimeTrackingMergeRequest()
      {
         if (_timeTracker == null || _timeTrackingHost == null)
         {
            return;
         }
         findGlobal(_timeTracker.MergeRequest);
      }

      private void findGlobal(MergeRequestKey mrk)
      {
         emulateClickOnHostToolbarButton(mrk.ProjectKey.HostName);
         getConnectionPage(mrk.ProjectKey.HostName).TrySelectMergeRequest(mrk);
      }

      // Diff Tool

      private void onDiffCommand(string argumentString)
      {
         string[] argumentsEx = argumentString.Split('|');
         int gitPID = int.Parse(argumentsEx[argumentsEx.Length - 1]);

         string[] arguments = new string[argumentsEx.Length - 1];
         Array.Copy(argumentsEx, 0, arguments, 0, argumentsEx.Length - 1);

         enqueueDiffRequest(new DiffRequest(gitPID, arguments));
      }

      struct DiffRequest
      {
         internal int GitPID { get; }
         internal string[] DiffArguments { get; }

         internal DiffRequest(int gitPID, string[] diffArguments)
         {
            GitPID = gitPID;
            DiffArguments = diffArguments;
         }
      }

      readonly Queue<DiffRequest> _requestedDiff = new Queue<DiffRequest>();
      private void enqueueDiffRequest(DiffRequest diffRequest)
      {
         _requestedDiff.Enqueue(diffRequest);
         if (_requestedDiff.Count == 1)
         {
            BeginInvoke(new Action(() => processDiffQueue()));
         }
      }

      private void processDiffQueue()
      {
         if (!_requestedDiff.Any())
         {
            return;
         }

         DiffRequest diffRequest = _requestedDiff.Peek();
         try
         {
            SnapshotSerializer serializer = new SnapshotSerializer();
            Snapshot snapshot;
            try
            {
               snapshot = serializer.DeserializeFromDisk(diffRequest.GitPID);
            }
            catch (Exception ex) // Any exception from de-serialization code
            {
               ExceptionHandlers.Handle("Cannot read serialized Snapshot object", ex);
               MessageBox.Show(
                  "Make sure that diff tool was launched from Merge Request Helper which is still running",
                  "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }

            getCurrentConnectionPage()?.ProcessDiffToolRequest(snapshot, diffRequest.DiffArguments);
         }
         finally
         {
            if (_requestedDiff.Any())
            {
               _requestedDiff.Dequeue();
               BeginInvoke(new Action(() => processDiffQueue()));
            }
         }
      }

      // New Version

      private void onNewVersionAvailable()
      {
         Debug.Assert(StaticUpdateChecker.NewVersionInformation != null);
         updateNewVersionStatus();

         // when a new version appears in the middle of work, re-schedule a reminder to trigger in 24 hours
         stopNewVersionReminderTimer();
         if (!notifyAboutNewVersion())
         {
            Trace.TraceInformation("[MainForm] Reminder timer restarted");
            startNewVersionReminderTimer();
         }
      }

      private bool notifyAboutNewVersion()
      {
         Debug.Assert(StaticUpdateChecker.NewVersionInformation != null);
         if (isTrackingTime())
         {
            _applicationUpdateReminderPostponedTillTimerStop = false;
            _applicationUpdateNotificationPostponedTillTimerStop = true;
            Trace.TraceInformation("[MainForm] New version appeared during time tracking");
         }
         else if (ApplicationUpdateHelper.ShowCheckForUpdatesDialog())
         {
            doCloseOnUpgrade();
            return true;
         }
         return false;
      }

      private bool remindAboutNewVersion()
      {
         Trace.TraceInformation("[MainForm] Reminder timer triggered (or re-triggered after timer stop)");
         if (StaticUpdateChecker.NewVersionInformation != null && Program.Settings.RemindAboutAvailableNewVersion)
         {
            if (isTrackingTime())
            {
               _applicationUpdateReminderPostponedTillTimerStop = true;
               _applicationUpdateNotificationPostponedTillTimerStop = false;
               Trace.TraceInformation("[MainForm] Reminder triggered during time tracking");
            }
            else if (ApplicationUpdateHelper.RemindAboutAvailableVersion())
            {
               doCloseOnUpgrade();
               return true;
            }
         }
         return false;
      }

      private void upgradeApplicationByUserRequest()
      {
         if (StaticUpdateChecker.NewVersionInformation == null)
         {
            Debug.Assert(false); // Should not UI control be disabled now?..
            return;
         }

         Trace.TraceInformation("[MainForm] User clicked at new version label in UI");
         if (ApplicationUpdateHelper.InstallUpdate(StaticUpdateChecker.NewVersionInformation.InstallerFilePath))
         {
            doCloseOnUpgrade();
         }
         else
         {
            Trace.TraceInformation("[MainForm] User discarded to install a new version");
         }
      }

      private void updateNewVersionStatus()
      {
         updateToolStripMenuItem.Enabled = StaticUpdateChecker.NewVersionInformation != null;
         updateCaption();
      }

      // Status

      private void addOperationRecord(string text)
      {
         string textWithTimestamp = String.Format("{0} {1}", TimeUtils.DateTimeToString(DateTime.Now), text);
         _operationRecordHistory.Add(textWithTimestamp);
         if (_operationRecordHistory.Count() > OperationRecordHistoryDepth)
         {
            _operationRecordHistory.RemoveAt(0);
         }

         labelOperationStatus.Text = text;
         Trace.TraceInformation("[MainForm] {0}", text);

         if (!_exiting)
         {
            StringBuilder builder = new StringBuilder(OperationRecordHistoryDepth);
            foreach (string record in _operationRecordHistory)
            {
               builder.AppendLine(record);
            }
            labelOperationStatus.ToolTipText = builder.ToString();
         }
      }

      private void updateStorageStatusLabel(string text)
      {
         labelStorageStatus.Text = text;
      }

      private void processConnectionStatusChange(ConnectionPage.EConnectionState state, string details)
      {
         Trace.TraceInformation("[MainForm] processConnectionStatusChange({0}, {1})",
            state.ToString(), details ?? "null");

         updateConnectionStatusLabel(state, details);
         if (state == ConnectionPage.EConnectionState.ConnectionLost)
         {
            startConnectionLossBlinkingTimer();
         }
         else
         {
            stopConnectionLossBlinkingTimer();
         }
      }

      private void updateConnectionStatusLabel(ConnectionPage.EConnectionState state, string details)
      {
         if (state == ConnectionPage.EConnectionState.ConnectionLost)
         {
            updateConnectionStatusLabelOnConnectionLoss();
            labelConnectionStatus.ToolTipText = details;
            return;
         }

         Color foreColor = ColorScheme.GetColor("MainFormStatusBar_Text").Color;
         string labelText = String.Empty;
         switch (state)
         {
            case ConnectionPage.EConnectionState.Connected:
               foreColor = ColorScheme.GetColor("MainFormStatusBar_Connected_Text").Color;
               labelText = "Connected";
               break;

            case ConnectionPage.EConnectionState.Connecting:
               labelText = "Connecting...";
               break;

            case ConnectionPage.EConnectionState.NotConnected:
               labelText = "Not connected";
               break;

            case ConnectionPage.EConnectionState.ConnectionLost:
               Debug.Assert(false); // handled above
               break;
         }

         labelConnectionStatus.ForeColor = foreColor;
         labelConnectionStatus.Text = labelText;
         labelConnectionStatus.ToolTipText = details;
      }

      private void updateConnectionStatusLabelOnConnectionLoss()
      {
         string labelText = "Connection is lost. Reconnecting...";
         if (_connectionLossBlinkingPhase == BlinkingPhase.Second)
         {
            labelText = labelText.ToUpper();
         }
         labelConnectionStatus.ForeColor = ColorScheme.GetColor("MainFormStatusBar_Reconnecting_Text").Color;
         labelConnectionStatus.Text = labelText;
      }

      private void updateCaption()
      {
         string mainCaption = Constants.MainWindowCaption;
         string currentVersion = " (" + Application.ProductVersion + ")";
         string newVersion = StaticUpdateChecker.NewVersionInformation != null
              ? String.Format("   New version {0} is available!", StaticUpdateChecker.NewVersionInformation.VersionNumber)
              : String.Empty;
         Text = String.Format("{0} {1} {2}", mainCaption, currentVersion, newVersion);
      }

      private void setNotifyIconByColor(Color? colorOpt)
      {
         if (colorOpt == null)
         {
            notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
            return;
         }

         Icon icon = IconCache.Get(colorOpt.Value);
         if (icon == null)
         {
            notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
            return;
         }

         notifyIcon.Icon = icon;
      }

      private void updateTrayAndTaskBar()
      {
         void applyColor(Color? colorOpt)
         {
            if (colorOpt != null)
            {
               setNotifyIconByColor(colorOpt.Value);
               WinFormsHelpers.SetOverlayEllipseIcon(colorOpt.Value);
            }
            else
            {
               setNotifyIconByColor(null);
               WinFormsHelpers.SetOverlayEllipseIcon(null);
            }
         }

         Color? consolidatedColor = getConsolidatedColor();
         if (isTrackingTime())
         {
            applyColor(ColorScheme.GetColor("Status_Tracking")?.Color);
         }
         else if (consolidatedColor.HasValue)
         {
            applyColor(consolidatedColor);
         }
         else if (isConnectionLost())
         {
            applyColor(ColorScheme.GetColor("Status_LostConnection")?.Color);
         }
         else
         {
            applyColor(null);
         }
      }

      private void updateToolbarHostIcon(string hostname)
      {
         HostToolbarItem toolbarButton = getHostToolbarButtons()
            .SingleOrDefault(item => item.HostName == hostname);
         if (toolbarButton == null)
         {
            return;
         }

         Color? summaryColor = getConnectionPage(hostname)?.GetSummaryColor();
         toolbarButton.UpdateIcon(summaryColor);
      }

      private bool isConnectionLost()
      {
         return getConnectionPages().Any(connectionPage =>
            connectionPage.GetConnectionState(out var _) == ConnectionPage.EConnectionState.ConnectionLost);
      }

      private Color? getConsolidatedColor()
      {
         IEnumerable<Color?> summaryColors = getConnectionPages()
            .Select(connectionPage => connectionPage.GetSummaryColor());
         ColorSchemeItem[] colorSchemeItems = ColorScheme.GetColors("MergeRequests");
         return colorSchemeItems?
            .FirstOrDefault(colorSchemeItem =>
               summaryColors.Any(color => color.HasValue && color.Value == colorSchemeItem.Color))?
            .Color;
      }

      private void startConnectionLossBlinkingTimer()
      {
         if (!_connectionLossBlinkingTimer.Enabled)
         {
            _connectionLossBlinkingTimer.Start();
         }
      }

      private void stopConnectionLossBlinkingTimer()
      {
         if (_connectionLossBlinkingTimer.Enabled)
         {
            _connectionLossBlinkingTimer.Stop();
         }
      }

      private void onConnectionLossBlinkingTimer(object sender, EventArgs e)
      {
         switch (_connectionLossBlinkingPhase)
         {
            case BlinkingPhase.First: _connectionLossBlinkingPhase = BlinkingPhase.Second; break;
            case BlinkingPhase.Second: _connectionLossBlinkingPhase = BlinkingPhase.First; break;
         }
         updateConnectionStatusLabelOnConnectionLoss();
      }

      private void onSessionLockCheckTimer(object sender, EventArgs e)
      {
         if (isTrackingTime())
         {
            Trace.TraceInformation("[MainForm] Time tracking will be stopped because workstation was locked");
            stopTimeTrackingTimer();
         }
      }

      private void startSessionLockCheckTimer()
      {
         if (!_sessionLockCheckTimer.Enabled)
         {
            _sessionLockCheckTimer.Start();
         }
      }

      private void stopSessionLockCheckTimer()
      {
         if (_sessionLockCheckTimer.Enabled)
         {
            _sessionLockCheckTimer.Stop();
         }
      }

      // Custom Actions

      private void clearCustomActionControls()
      {
         removeToolbarButtons(toolStripCustomActions);
      }

      private void createCustomActionControls(IEnumerable<ICommand> commands)
      {
         if (commands == null)
         {
            return;
         }

         int id = 0;
         foreach (ICommand command in commands)
         {
            string name = command.Name;
            var menuItem = new CustomActionToolbarItem
            {
               Name = "customAction" + id,
               Margin = toolStripButtonLive.Margin,
               Text = name,
               Tag = command,
               ToolTipText = command.Hint,
               Enabled = false,
               Visible = command.InitiallyVisible,
               // Image size automatically upscales to parent's ImageScalingSize but let's set it to some default value
               Image = new Bitmap(DefaultToolbarImageSize.Width, DefaultToolbarImageSize.Height),
               DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
               TextImageRelation = TextImageRelation.Overlay
            };
            menuItem.Click += (sender, e) =>
            {
               ConnectionPage connectionPage = getCurrentConnectionPage();
               if (connectionPage == null)
               {
                  return;
               }

               MergeRequestKey mergeRequestKey = new MergeRequestKey(
                  new ProjectKey(connectionPage.GetCurrentHostName(), connectionPage.GetCurrentProjectName()),
                  connectionPage.GetCurrentMergeRequestIId());

               WrongActionConfirmationForm.ActionType actionType = WrongActionConfirmationForm.ActionType.ExecuteAction;
               if (isTrackingTime() && !_timeTracker.MergeRequest.Equals(mergeRequestKey) &&
                   !WrongActionConfirmationForm.Show(this, actionType, () => findGlobal(_timeTracker.MergeRequest)))
               {
                  return;
               }

               BeginInvoke(new Action(async () => await onCommandAsync(command, mergeRequestKey, connectionPage)));
            };
            toolStripCustomActions.Items.Add(menuItem);
            id++;
         }
      }

      private struct CommandCallback : ICommandCallback
      {
         public CommandCallback(MergeRequestKey mergeRequestKey) => MergeRequestKey = mergeRequestKey;

         public string GetCurrentAccessToken() => Program.Settings.GetAccessToken(MergeRequestKey.ProjectKey.HostName);

         public string GetCurrentHostName() => MergeRequestKey.ProjectKey.HostName;

         public int GetCurrentMergeRequestIId() => MergeRequestKey.IId;

         public string GetCurrentProjectName() => MergeRequestKey.ProjectKey.ProjectName;

         private MergeRequestKey MergeRequestKey { get; }
      }

      private async Task onCommandAsync(ICommand command, MergeRequestKey mrk, ConnectionPage connectionPage)
      {
         addOperationRecord(String.Format("Command {0} execution has started", command.Name));
         try
         {
            await command.Run(new CommandCallback(mrk));
         }
         catch (Exception ex) // Exception type does not matter
         {
            string errorMessage = "Custom action failed";
            ExceptionHandlers.Handle(errorMessage, ex);
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            addOperationRecord(String.Format("Command {0} failed", command.Name));
            return;
         }

         string statusMessage = String.Format(
            "Command {0} execution has completed for merge request !{1} in project {2}",
            command.Name, mrk.IId, mrk.ProjectKey.ProjectName);
         addOperationRecord(statusMessage);
         Trace.TraceInformation("[MainForm] EnabledIf: {0}", command.EnabledIf);
         Trace.TraceInformation("[MainForm] VisibleIf: {0}", command.VisibleIf);

         if (command.StopTimer)
         {
            await stopTimeTrackingTimerAsync();
         }

         if (command.Pin)
         {
            connectionPage.TogglePinState(mrk);
         }

         if (command.Reload)
         {
            connectionPage.ReloadOne(mrk);
         }
      }

      private IEnumerable<CustomActionToolbarItem> getCustomActionMenuItems()
      {
         return toolStripCustomActions.Items
            .Cast<ToolStripItem>()
            .Where(item => item is CustomActionToolbarItem)
            .Select(item => item as CustomActionToolbarItem);
      }

      // Misc

      private static void showWarningOnReloadList()
      {
         if (Program.Settings.ShowWarningOnReloadList)
         {
            int autoUpdateMs = Program.Settings.AutoUpdatePeriodMs;
            double oneMinuteMs = 60000;
            double autoUpdateMinutes = autoUpdateMs / oneMinuteMs;

            string periodicity = autoUpdateMs > oneMinuteMs
               ? (autoUpdateMs % Convert.ToInt32(oneMinuteMs) == 0
                  ? String.Format("{0} minutes", autoUpdateMinutes)
                  : String.Format("{0:F1} minutes", autoUpdateMinutes))
               : String.Format("{0} seconds", autoUpdateMs / 1000);

            string message = String.Format(
               "Merge Request list updates each {0} and you don't usually need to update it manually", periodicity);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.Settings.ShowWarningOnReloadList = false;
         }
      }

      private static void removeToolbarButtons(ToolStrip toolbar)
      {
         toolbar.SuspendLayout();
         toolbar.Items.Cast<ToolStripItem>()
            .Where(item => item is HostToolbarItem)
            .Select(item => item as HostToolbarItem)
            .ToList()
            .ForEach(item => item.UnsubscribeFromDpiChange());
         toolbar.Items.Cast<ToolStripItem>().ToList().ForEach(item => item.Image?.Dispose());
         toolbar.Items.Cast<ToolStripItem>().ToList().ForEach(item => item.Dispose());
         toolbar.Items.Clear();
         toolbar.ResumeLayout();
      }

      private string getClipboardText()
      {
         try
         {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
               return Clipboard.GetText(TextDataFormat.Text);
            }
         }
         catch (System.Runtime.InteropServices.ExternalException) { }
         catch (Exception) // just in case
         {
            Debug.Assert(false);
         }
         return String.Empty;
      }

      private void doClose()
      {
         setExitingFlag();
         Close();
      }

      private void doCloseOnUpgrade()
      {
         Trace.TraceInformation("[MainForm] Application is exiting to install a new version...");
         doClose();
      }

      private void setExitingFlag()
      {
         Trace.TraceInformation("[MainForm] Set _exiting flag");
         _exiting = true;
         getConnectionPages()?.ToList().ForEach(connectionPage => connectionPage.SetExiting());
      }

      private void onHideToTray()
      {
         Trace.TraceInformation("[MainForm] onHideToTray()");
         if (Program.Settings.ShowWarningOnHideToTray)
         {
            _trayIcon.ShowTooltipBalloon(new TrayIcon.BalloonText("Information", "I will now live in your tray"));
            Program.Settings.ShowWarningOnHideToTray = false;
         }
         Hide();
      }

      private void onRestoreFromTray()
      {
         Trace.TraceInformation("[MainForm] onRestoreFromTray(), Visible = {0}", Visible.ToString());
         if (!Visible)
         {
            Show();
            updateTrayAndTaskBar();
         }
         CommonNative.Win32Tools.ForceWindowIntoForeground(this.Handle);
      }

      private static void sendFeedback()
      {
         try
         {
            if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
            {
               Program.FeedbackReporter.SendEMail("Merge Request Helper Feedback Report",
                  "Please provide your feedback here", Program.ServiceManager.GetBugReportEmail(),
                  Constants.BugReportLogArchiveName, Constants.BugReportDumpArchiveName);
            }
         }
         catch (FeedbackReporterException ex)
         {
            string message = "Cannot send feedback";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(ex.InnerException?.Message ?? "Unknown error", message,
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private static void showHelp()
      {
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         if (helpUrl != String.Empty)
         {
            UrlHelper.OpenBrowser(helpUrl);
         }
      }

      private IEnumerable<string> getHostList()
      {
         return Program.Settings.KnownHosts;
      }

      private void saveSizeAndLocation()
      {
         if (WindowState == FormWindowState.Maximized)
         {
            Program.Settings.WidthBeforeClose = RestoreBounds.Size.Width;
            Program.Settings.HeightBeforeClose = RestoreBounds.Size.Height;
            Program.Settings.LeftBeforeClose = RestoreBounds.Location.X;
            Program.Settings.TopBeforeClose = RestoreBounds.Location.Y;
            Program.Settings.WasMaximizedBeforeClose = true;
            Program.Settings.WasMinimizedBeforeClose = false;
         }
         else if (WindowState == FormWindowState.Normal)
         {
            Program.Settings.WidthBeforeClose = Size.Width;
            Program.Settings.HeightBeforeClose = Size.Height;
            Program.Settings.LeftBeforeClose = Location.X;
            Program.Settings.TopBeforeClose = Location.Y;
            Program.Settings.WasMaximizedBeforeClose = false;
            Program.Settings.WasMinimizedBeforeClose = false;
         }
         else
         {
            Program.Settings.WidthBeforeClose = RestoreBounds.Size.Width;
            Program.Settings.HeightBeforeClose = RestoreBounds.Size.Height;
            Program.Settings.LeftBeforeClose = RestoreBounds.Location.X;
            Program.Settings.TopBeforeClose = RestoreBounds.Location.Y;
            Program.Settings.WasMaximizedBeforeClose = false;
            Program.Settings.WasMinimizedBeforeClose = true;
         }
      }

      bool _inRestoringSize = false;
      private void onWindowStateChanged()
      {
         Trace.TraceInformation(
            "[MainForm] onWindowStateChanged(), _isRestoringSize={0}, " +
            "_prevWindowState={1}, WindowState={2}, _restoreSizeOnNextRestore={3}",
            _inRestoringSize, _prevWindowState.ToString(), WindowState.ToString(),
            _restoreSizeOnNextRestore);

         if (_inRestoringSize)
         {
            return;
         }

         Trace.TraceInformation("[MainForm] Window state changed from {0} to {1}",
            _prevWindowState.ToString(), WindowState.ToString());

         if (WindowState != FormWindowState.Minimized)
         {
            if (_restoreSizeOnNextRestore)
            {
               _restoreSizeOnNextRestore = false;
               _inRestoringSize = true;
               try
               {
                  restoreSize();
               }
               finally
               {
                  _inRestoringSize = false;
               }
            }

            if (_prevWindowState == FormWindowState.Minimized)
            {
               getCurrentConnectionPage()?.RestoreSplitterDistance();
            }
         }

         _prevWindowState = WindowState;
      }

      private void processDpiChange()
      {
         Trace.TraceInformation(String.Format("[MainForm] DPI changed, new DPI = {0}", DeviceDpi));
            CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         _trayIcon.ShowTooltipBalloon(new TrayIcon.BalloonText
         (
            "System DPI has changed",
            "It is recommended to restart application to update layout"
         ));

         resizeToolbarImages();

         DiffContextHelpers.EnableHtmlWidthEstimates = false;
      }

      private bool checkAutoScaleDimensions()
      {
         if (CurrentAutoScaleDimensions.Height == 0 || CurrentAutoScaleDimensions.Width == 0) //-V3024
         {
            MessageBox.Show("A problem with system fonts has been detected, mrHelper cannot start",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            doClose();
            return false;
         }
         return true;
      }

      void moveToolstrips(ToolStripPanel moveFrom, ToolStripPanel moveTo)
      {
         bool isMoveable(ToolStrip toolStrip)
         {
            return toolStrip == toolStripCustomActions || toolStrip == toolStripActions;
         }

         for (int iToolStrip = moveFrom.Controls.Count - 1; iToolStrip >= 0; --iToolStrip)
         {
            ToolStrip toolStrip = moveFrom.Controls[iToolStrip] as ToolStrip;
            if (isMoveable(toolStrip))
            {
               moveFrom.Controls.Remove(toolStrip);
               moveTo.Controls.Add(toolStrip);
            }
         }
      }

      private void onToolBarPositionChanged()
      {
         initToolBars();
      }

      private void pullToolBar(ToolStrip toolStrip, bool up)
      {
         int min = int.MaxValue;
         while (true)
         {
            int prev = up ? toolStrip.Location.Y : toolStrip.Location.X;
            if (prev < 2)
            {
               break;
            }

            toolStrip.Location = up
               ? new Point(toolStrip.Location.X, prev - 1)
               : new Point(prev - 1, toolStrip.Location.Y);
            int current = up ? toolStrip.Location.Y : toolStrip.Location.X;
            if (current > prev)
            {
               int newValue = min != int.MaxValue ? min : prev;
               toolStrip.Location = up
                  ? new Point(toolStrip.Location.X, newValue)
                  : new Point(newValue, toolStrip.Location.Y);
               break;
            }

            if (current < prev)
            {
               min = current;
               continue;
            }

            break; // current == prev
         }
      }

      private void pullCustomActionToolBar()
      {
         toolStripContainer1.SuspendLayout();

         if (toolStripContainer1.TopToolStripPanel.Controls.Contains(toolStripActions))
         {
            toolStripCustomActions.Location = new Point(
               toolStripActions.Location.X + toolStripActions.Width,
               toolStripCustomActions.Location.Y);
            pullToolBar(toolStripCustomActions, false);
         }
         else
         {
            toolStripCustomActions.Location = new Point(
               toolStripCustomActions.Location.X,
               toolStripActions.Location.Y + toolStripActions.Height);
            pullToolBar(toolStripCustomActions, true);
         }

         toolStripContainer1.ResumeLayout();
      }

      private void pullToolBars()
      {
         toolStripContainer1.SuspendLayout();

         // Pull first control closer to the border and minimize gaps between tool bars
         if (toolStripContainer1.TopToolStripPanel.Controls.Contains(toolStripActions))
         {
            if (isToolStripHostsVisible())
            {
               pullToolBar(toolStripHosts, false);
               toolStripActions.Location = new Point(
                  toolStripHosts.Location.X + toolStripHosts.Width,
                  toolStripActions.Location.Y);
            }
            pullToolBar(toolStripActions, false);
         }
         else
         {
            pullToolBar(toolStripActions, true);
         }
         pullCustomActionToolBar();

         toolStripContainer1.ResumeLayout();
      }

      private void initToolBars()
      {
         toolStripContainer1.SuspendLayout();

         // Attach tool bars to their parents
         var tbPosition = ConfigurationHelper.GetToolBarPosition(Program.Settings);
         switch (tbPosition)
         {
            case ConfigurationHelper.ToolBarPosition.Top:
               moveToolstrips(toolStripContainer1.LeftToolStripPanel, toolStripContainer1.TopToolStripPanel);
               moveToolstrips(toolStripContainer1.RightToolStripPanel, toolStripContainer1.TopToolStripPanel);
               moveToolstrips(toolStripContainer1.BottomToolStripPanel, toolStripContainer1.TopToolStripPanel);
               break;

            case ConfigurationHelper.ToolBarPosition.Left:
               moveToolstrips(toolStripContainer1.TopToolStripPanel, toolStripContainer1.LeftToolStripPanel);
               moveToolstrips(toolStripContainer1.RightToolStripPanel, toolStripContainer1.LeftToolStripPanel);
               moveToolstrips(toolStripContainer1.BottomToolStripPanel, toolStripContainer1.LeftToolStripPanel);
               break;

            case ConfigurationHelper.ToolBarPosition.Right:
               moveToolstrips(toolStripContainer1.TopToolStripPanel, toolStripContainer1.RightToolStripPanel);
               moveToolstrips(toolStripContainer1.LeftToolStripPanel, toolStripContainer1.RightToolStripPanel);
               moveToolstrips(toolStripContainer1.BottomToolStripPanel, toolStripContainer1.RightToolStripPanel);
               break;
         }

         // To guarantee the following order: Hosts - Actions - Custom Actions
         toolStripCustomActions.Location = new System.Drawing.Point(0, 0);
         toolStripActions.Location = new System.Drawing.Point(0, 0);
         toolStripHosts.Location = new System.Drawing.Point(0, 0);

         pullToolBars();

         // Place menu
         menuStrip1.Location = new System.Drawing.Point(0, 0);

         resizeToolbarImages();

         toolStripContainer1.ResumeLayout();
      }

      private void resizeToolbarImages()
      {
         int newToolbarImageWidth = (int)WinFormsHelpers.ScalePixelsToNewDpi(
            96, DeviceDpi, DefaultToolbarImageSize.Width);
         int newToolbarImageHeight = (int)WinFormsHelpers.ScalePixelsToNewDpi(
            96, DeviceDpi, DefaultToolbarImageSize.Height);
         Size newToolbarImageSize = new Size(newToolbarImageWidth, newToolbarImageHeight);
         foreach (ToolStrip toolStrip in new ToolStrip[]
            { toolStripCustomActions, toolStripActions, toolStripHosts })
         {
            toolStrip.ImageScalingSize = newToolbarImageSize;
         }
      }

      private bool isToolStripHostsVisible()
      {
         return toolStripHosts.Items.Count > 1;
      }
   }
}

