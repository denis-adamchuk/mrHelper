using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitClient;
using mrHelper.CustomActions;

namespace mrHelper.App.Forms
{
   internal partial class MainForm :
      CustomFontForm,
      ICommandCallback,
      ILocalGitRepositoryFactoryAccessor
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      internal MainForm()
      {
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

      public ILocalGitRepositoryFactory GetFactory()
      {
         return getLocalGitRepositoryFactory(Program.Settings.LocalGitFolder);
      }

      private readonly System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
      {
         Interval = timeTrackingTimerInterval
      };

      private bool _loadingConfiguration = false;
      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mergeRequestDescriptionMarkdownPipeline;
      private bool _canSwitchTab = true;
      private bool _notifyOnCommitChainCancelEnabled;
      private readonly bool _runningAsUwp = false;

      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private LocalGitRepositoryFactory _gitClientFactory;
      private GitInteractiveUpdater _gitClientUpdater;
      private GitDataUpdater _gitDataUpdater;
      private GitStatisticManager _gitStatManager;
      private PersistentStorage _persistentStorage;
      private UserNotifier _userNotifier;
      private EventFilter _eventFilter;
      private CommitChainCreator _commitChainCreator;

      private string _initialHostName = String.Empty;
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedCommits =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private WorkflowManager _workflowManager;
      private ExpressionResolver _expressionResolver;
      private TimeTracker _timeTracker;

      private SearchWorkflowManager _searchWorkflowManager;

      private IEnumerable<ICommand> _customCommands;
      private IEnumerable<string> _keywords;
      private ColorScheme _colorScheme;
      private Dictionary<string, string> _iconScheme;

      private string _newVersionFilePath;
      private string _newVersionNumber;
      private readonly System.Windows.Forms.Timer _checkForUpdatesTimer = new System.Windows.Forms.Timer
      {
         Interval = Constants.CheckForUpdatesTimerInterval
      };

      private struct HostComboBoxItem
      {
         internal string Host;
         internal string AccessToken;
      }

      private enum ECommitComboBoxItemStatus
      {
         Normal,
         Base,
         Latest
      }

      private struct CommitComboBoxItem
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

      private MergeRequestCache _mergeRequestCache;
      private MergeRequestFilter _mergeRequestFilter;

      private readonly Dictionary<string, User> _currentUser = new Dictionary<string, User>();
   }
}

