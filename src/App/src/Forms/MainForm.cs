using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;

namespace mrHelper.App.Forms
{
   internal partial class MainForm :
      CustomFontForm,
      ICommandCallback
   {
      // TODO Combine multiple timers into a single one
      private static readonly int TimeTrackingTimerInterval = 1000 * 1; // 1 second
      private static readonly int ClipboardCheckingTimerInterval = 1000 * 1; // 1 second
      private static readonly int ProjectAndUserCacheCheckTimerInterval = 1000 * 1; // 1 second
      private static readonly int ListViewRefreshTimerInterval = 1000 * 20; // 20 seconds

      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";

      private static readonly string DefaultColorSchemeName = "Default";
      private static readonly string ColorSchemeFileNamePrefix = "colors.json";
      private static readonly string IconSchemeFileName = "icons.json";
      private static readonly string BadgeSchemeFileName = "badges.json";
      private static readonly string ProjectListFileName = "projects.json";

      private static readonly string openFromClipboardEnabledText = "Open from Clipboard";
      private static readonly string openFromClipboardDisabledText = "Open from Clipboard (Copy GitLab MR URL to activate)";

      private static readonly string RefreshButtonTooltip = "Refresh merge request list in the background";

      private static readonly int NewOrClosedMergeRequestRefreshListTimerInterval = 1000 * 3; // 3 seconds
      private static readonly int PseudoTimerInterval = 100 * 1; // 0.1 second

      private bool _forceMaximizeOnNextRestore;
      private bool _applySplitterDistanceOnNextRestore;
      private FormWindowState _prevWindowState;
      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;

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
      private readonly Timer _listViewRefreshTimer = new Timer
      {
         Interval = ListViewRefreshTimerInterval
      };

      private LocalCommitStorageFactory _storageFactory;
      private GitDataUpdater _gitDataUpdater;
      private IDiffStatisticProvider _diffStatProvider;
      private PersistentStorage _persistentStorage;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;
      private ExpressionResolver _expressionResolver;
      private MergeRequestFilter _mergeRequestFilter;
      private readonly GitLabClient.Accessors.ModificationNotifier _modificationNotifier =
         new GitLabClient.Accessors.ModificationNotifier();

      private HashSet<MergeRequestKey> _recentMergeRequests = new HashSet<MergeRequestKey>();
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

      // TODO Data caches should be hidden into a holder and accessed via getDataCache() only
      private DataCache _liveDataCache;
      private DataCache _searchDataCache;
      private DataCache _recentDataCache;

      private TabPage _timeTrackingTabPage;
      private ITimeTracker _timeTracker;

      private string _initialHostName;
      private IEnumerable<ICommand> _customCommands;
      private IEnumerable<string> _keywords;
      private ColorScheme _colorScheme;
      private Dictionary<string, string> _iconScheme;
      private Dictionary<string, string> _badgeScheme;

      private string _newVersionFilePath;
      private string _newVersionNumber;
      private readonly System.Windows.Forms.Timer _checkForUpdatesTimer = new System.Windows.Forms.Timer
      {
         Interval = Constants.CheckForUpdatesTimerInterval
      };

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
   }
}

