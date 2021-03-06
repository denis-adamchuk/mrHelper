﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers.GitLab;

namespace mrHelper.App.Forms
{
   internal partial class MainForm :
      CustomFontForm,
      ICommandCallback
   {
      // TODO Combine multiple timers into a single one
      private static readonly int LostConnectionIndicationTimerInterval = 750 * 1; // 0.75 second
      private static readonly int TimeTrackingTimerInterval = 1000 * 1; // 1 second
      private static readonly int ClipboardCheckingTimerInterval = 1000 * 1; // 1 second
      private static readonly int ProjectAndUserCacheCheckTimerInterval = 1000 * 1; // 1 second
      private static readonly int NewVersionReminderTimerInterval = 1000 * 60 * 60 * 24; // 24 hours
      private static readonly int RedrawTimerInterval = 1000 * 30; // 0.5 minute

      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";

      private static readonly string DefaultColorSchemeName = "Default";
      private static readonly string ColorSchemeFileNamePrefix = "colors.json";
      private static readonly string ProjectListFileName = "projects.json";

      private static readonly string openFromClipboardEnabledText = "Open from Clipboard";
      private static readonly string openFromClipboardDisabledText = "Open from Clipboard (Copy GitLab MR URL to activate)";

      private static readonly string RefreshButtonTooltip = "Refresh merge request list in the background";

      private static readonly string ConnectionLostText = "connection is lost (trying to reconnect)";

      private static readonly int NewOrClosedMergeRequestRefreshListTimerInterval = 1000 * 3; // 3 seconds
      private static readonly int PseudoTimerInterval = 100 * 1; // 0.1 second

      private static readonly int OperationRecordHistoryDepth = 10;

      private bool _forceMaximizeOnNextRestore;
      private bool _applySplitterDistanceOnNextRestore;
      private FormWindowState _prevWindowState;
      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;
      private bool _applicationUpdateNotificationPostponedTillTimerStop;
      private bool _applicationUpdateReminderPostponedTillTimerStop;

      private readonly Dictionary<Color, IconGroup> _iconCache = new Dictionary<Color, IconGroup>();
      private readonly PeriodicUpdateChecker _applicationUpdateChecker;
      private readonly bool _startMinimized;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mdPipeline;
      private readonly bool _canSwitchTab = true;
      private readonly bool _allowAutoStartApplication = false;
      private readonly string _startUrl;
      private readonly bool _integratedInGitExtensions;
      private readonly bool _integratedInSourceTree;
      private readonly Timer _timeTrackingTimer = new Timer
      {
         Interval = TimeTrackingTimerInterval
      };
      private readonly Timer _clipboardCheckingTimer = new Timer
      {
         Interval = ClipboardCheckingTimerInterval
      };
      private readonly Timer _newVersionReminderTimer = new Timer
      {
         Interval = NewVersionReminderTimerInterval
      };
      private readonly Timer _redrawTimer = new Timer
      {
         // This timer is needed to update "ago" timestamps
         Interval = RedrawTimerInterval
      };

      private LocalCommitStorageFactory _storageFactory;
      private GitDataUpdater _gitDataUpdater;
      private IDiffStatisticProvider _diffStatProvider;
      private PersistentStorage _persistentStorage;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;
      private ExpressionResolver _expressionResolver;
      private MergeRequestFilter _mergeRequestFilter;
      private Shortcuts _shortcuts;
      private GitLabInstance _gitLabInstance;

      private Dictionary<MergeRequestKey, DateTime> _recentMergeRequests = new Dictionary<MergeRequestKey, DateTime>();
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedRevisions =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private Dictionary<string, NewMergeRequestProperties> _newMergeRequestDialogStatesByHosts =
         new Dictionary<string, NewMergeRequestProperties>();
      private readonly List<MergeRequestKey> _mergeRequestsUpdatingByUserRequest = new List<MergeRequestKey>();
      private readonly Dictionary<MergeRequestKey, string> _latestStorageUpdateStatus =
         new Dictionary<MergeRequestKey, string>();
      private readonly Dictionary<string, User> _currentUser = new Dictionary<string, User>();
      private readonly List<string> _operationRecordHistory = new List<string>();

      // TODO Data caches should be hidden into a holder and accessed via getDataCache() only
      private DataCache _liveDataCache;
      private DataCache _searchDataCache;
      private DataCache _recentDataCache;

      private EDataCacheType? _timeTrackingMode;
      private ITimeTracker _timeTracker;

      private string _initialHostName;
      private IEnumerable<string> _keywords;
      private ColorScheme _colorScheme;

      private class HostComboBoxItem
      {
         public HostComboBoxItem(string host, string accessToken)
         {
            Host = host;
            AccessToken = accessToken;
         }

         internal string Host { get; }
         internal string AccessToken { get; }
      }

      private enum EConnectionState
      {
         ConnectingLive,
         ConnectingRecent,
         Connected
      }
      private EConnectionState? _connectionStatus;

      private struct LostConnectionInfo
      {
         public LostConnectionInfo(Timer indicatorTimer, DateTime timeStamp)
         {
            IndicatorTimer = indicatorTimer;
            TimeStamp = timeStamp;
         }

         internal Timer IndicatorTimer { get; }
         internal DateTime TimeStamp { get; }
      }
      private LostConnectionInfo? _lostConnectionInfo;

      private class ColorSelectorComboBoxItem
      {
         internal ColorSelectorComboBoxItem(string humanFriendlyName, Color color)
         {
            HumanFriendlyName = humanFriendlyName;
            Color = color;
         }

         /// <summary>
         /// ToString() override for ComboBox item sorting purpose
         /// </summary>
         public override string ToString()
         {
            return HumanFriendlyName;
         }

         internal string HumanFriendlyName { get; }

         internal Color Color { get; }
      }

      private struct IconGroup
      {
         internal IconGroup(Icon iconWithoutBorder, Icon iconWithBorder)
         {
            IconWithoutBorder = iconWithoutBorder;
            IconWithBorder = iconWithBorder;
         }

         internal Icon IconWithoutBorder { get; }
         internal Icon IconWithBorder { get; }
      }
   }
}

