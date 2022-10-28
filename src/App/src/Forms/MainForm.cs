using System;
using System.Collections.Generic;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : CustomFontForm
   {
      // TODO Combine multiple timers into a single one
      private static readonly int ConnectionLossBlinkingTimerInterval = 750 * 1; // 0.75 second
      private static readonly int TimeTrackingTimerInterval = 1000 * 1; // 1 second
      private static readonly int NewVersionReminderTimerInterval = 1000 * 60 * 60 * 24; // 24 hours
      private static readonly int SessionLockCheckTimerInterval = 1000 * 30; // 30 seconds

      private static readonly string DefaultTimeTrackingTextBoxText = "00:00:00";

      private static readonly string RefreshButtonTooltip = "Refresh Live merge request list in the background";

      private static readonly int OperationRecordHistoryDepth = 10;

      private readonly string _startUrl;

      private bool _restoreSizeOnNextRestore;
      private FormWindowState _prevWindowState;
      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _applicationUpdateNotificationPostponedTillTimerStop;
      private bool _applicationUpdateReminderPostponedTillTimerStop;

      private readonly PeriodicUpdateChecker _applicationUpdateChecker;
      private readonly bool _startMinimized;
      private readonly TrayIcon _trayIcon;
      private readonly bool _allowAutoStartApplication = false;
      private readonly bool _integratedInGitExtensions;
      private readonly bool _integratedInSourceTree;
      private readonly Timer _connectionLossBlinkingTimer = new Timer
      {
         Interval = ConnectionLossBlinkingTimerInterval
      };
      private readonly Timer _newVersionReminderTimer = new Timer
      {
         Interval = NewVersionReminderTimerInterval
      };
      private readonly Timer _timeTrackingTimer = new Timer
      {
         Interval = TimeTrackingTimerInterval
      };
      private readonly Timer _sessionLockCheckTimer = new Timer
      {
         Interval = SessionLockCheckTimerInterval
      };

      private string _defaultHostName;
      private DictionaryWrapper<MergeRequestKey, DateTime> _recentMergeRequests;
      private DictionaryWrapper<MergeRequestKey, HashSet<string>> _reviewedRevisions;
      private DictionaryWrapper<string, MergeRequestKey> _lastMergeRequestsByHosts;
      private DictionaryWrapper<string, NewMergeRequestProperties> _newMergeRequestDialogStatesByHosts;
      private HashSetWrapper<ProjectKey> _collapsedProjectsLive;
      private HashSetWrapper<ProjectKey> _collapsedProjectsRecent;
      private HashSetWrapper<ProjectKey> _collapsedProjectsSearch;
      private DictionaryWrapper<MergeRequestKey, DateTime> _mutedMergeRequests;
      private DictionaryWrapper<string, MergeRequestFilterState> _filtersByHostsLive;
      private DictionaryWrapper<string, MergeRequestFilterState> _filtersByHostsRecent;

      private readonly List<string> _operationRecordHistory = new List<string>();

      private string _timeTrackingHost;
      private ITimeTracker _timeTracker;

      private IEnumerable<string> _keywords;
      private readonly ColorScheme _colorScheme;

      private PersistentStorage _persistentStorage;

      private enum BlinkingPhase
      {
         First,
         Second
      }
      private BlinkingPhase _connectionLossBlinkingPhase = BlinkingPhase.First;
   }
}

