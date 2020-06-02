using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitClient;
using mrHelper.CustomActions;
using mrHelper.Client;
using mrHelper.Client.Common;

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

         _trayIcon = new TrayIcon(notifyIcon);
         _mergeRequestDescriptionMarkdownPipeline = MarkDownUtils.CreatePipeline();

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

      bool _startMinimized;
      bool _forceMaximizeOnNextRestore;
      FormWindowState _prevWindowState;
      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mergeRequestDescriptionMarkdownPipeline;
      private bool _canSwitchTab = true;
      private readonly bool _runningAsUwp = false;

      private LocalGitRepositoryFactory _gitClientFactory;
      private GitInteractiveUpdater _gitClientUpdater;
      private GitDataUpdater _gitDataUpdater;
      private GitStatisticManager _gitStatManager;
      private PersistentStorage _persistentStorage;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;

      private string _initialHostName = String.Empty;
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedCommits =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private ExpressionResolver _expressionResolver;

      private ISession _liveSession;
      private ISession _searchSession;
      private ISession getSessionByName(string name) => name == "Live" ? _liveSession : _searchSession;
      private string getSessionName(ISession session) => session == _liveSession ? "Live" : "Search";

      private ITimeTracker _timeTracker;
      private GitLabClientManager _gitlabClientManager;

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

      private enum ECommitComboBoxItemStatus
      {
         Normal,
         Base,
         Latest
      }

      public enum EComparableEntityType
      {
         Commit,
         Version
      }

      private class CommitComboBoxItem
      {
         internal string SHA { get; }
         internal string Text { get; }
         internal ECommitComboBoxItemStatus Status { get; }
         internal EComparableEntityType Type { get; }
         internal DateTime? TimeStamp { get; }
         internal string Message { get; }

         internal CommitComboBoxItem(string sha, string text, DateTime? timeStamp, string message,
            ECommitComboBoxItemStatus status, EComparableEntityType type)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
            Message = message;
            Status = status;
            Type = type;
         }
      }

      private MergeRequestFilter _mergeRequestFilter;

      private readonly Dictionary<string, User> _currentUser = new Dictionary<string, User>();
   }
}

