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
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      internal MainForm(bool startMinimized)
      {
         _startMinimized = startMinimized;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this, (float)Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         _runningAsUwp = new DesktopBridge.Helpers().IsRunningAsUwp();
         Trace.TraceInformation(String.Format("[MainForm] Running as UWP = {0}", _runningAsUwp ? "Yes" : "No"));
         checkBoxRunWhenWindowsStarts.Enabled = !_runningAsUwp;

         _trayIcon = new TrayIcon(notifyIcon);
         _mdPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         this.columnHeaderName.Width = this.listViewProjects.Width - SystemInformation.VerticalScrollBarWidth - 5;
         this.linkLabelConnectedTo.Text = String.Empty;

         foreach (Control control in CommonControls.Tools.WinFormsHelpers.GetAllSubControls(this))
         {
            if (control.Anchor.HasFlag(AnchorStyles.Right)
               && (control.MinimumSize.Width != 0 || control.MinimumSize.Height != 0))
            {
               Debug.Assert(false);
            }
         }

         buttonTimeTrackingCancel.ConfirmationCondition = () => true;
         buttonTimeTrackingCancel.ConfirmationText = "Tracked time will be lost, are you sure?";

         listViewMergeRequests.Deselected += ListViewMergeRequests_Deselected;
         listViewFoundMergeRequests.Deselected += ListViewMergeRequests_Deselected;

         listViewMergeRequests.ContextMenuStrip = new ContextMenuStrip();
         listViewMergeRequests.ContextMenuStrip.Items.Add("&Refresh", null, ListViewMergeRequests_Refresh);
         listViewMergeRequests.ContextMenuStrip.Items.Add("&Edit", null, ListViewMergeRequests_Edit);
      }

      public string GetCurrentHostName()
      {
         return getHostName();
      }

      public string GetCurrentAccessToken()
      {
         return Program.Settings.GetAccessToken(getHostName());
      }

      public string GetCurrentProjectName()
      {
         return getMergeRequestKey(null)?.ProjectKey.ProjectName ?? String.Empty;
      }

      public int GetCurrentMergeRequestIId()
      {
         return getMergeRequestKey(null)?.IId ?? 0;
      }

      private readonly System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
      {
         Interval = timeTrackingTimerInterval
      };
      readonly bool _startMinimized;
      bool _forceMaximizeOnNextRestore;
      bool _applySplitterDistanceOnNextRestore;
      FormWindowState _prevWindowState;
      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mdPipeline;
      private readonly bool _canSwitchTab = true;
      private readonly bool _runningAsUwp = false;

      private LocalCommitStorageFactory _storageFactory;
      private GitDataUpdater _gitDataUpdater;
      private IDiffStatisticProvider _diffStatProvider;
      private PersistentStorage _persistentStorage;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;

      private string _initialHostName = String.Empty;

      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedRevisions =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private Dictionary<string, NewMergeRequestProperties> _newMergeRequestDialogStatesByHosts =
         new Dictionary<string, NewMergeRequestProperties>();
      private ExpressionResolver _expressionResolver;

      private GitLabInstance _gitLabInstance;
      private DataCache _liveDataCache;
      private DataCache _searchDataCache;
      private DataCache getDataCacheByName(string name) =>
         name == "Live" ? _liveDataCache : _searchDataCache;
      private string getDataCacheName(DataCache dataCache) =>
         dataCache == _liveDataCache ? "Live" : "Search";

      private TabPage _timeTrackingTabPage;
      private ITimeTracker _timeTracker;

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

      private readonly List<MergeRequestKey> _mergeRequestsUpdatingByUserRequest = new List<MergeRequestKey>();
      private readonly Dictionary<MergeRequestKey, string> _latestStorageUpdateStatus =
         new Dictionary<MergeRequestKey, string>();

      private MergeRequestFilter _mergeRequestFilter;

      private readonly Dictionary<string, User> _currentUser = new Dictionary<string, User>();
   }
}

