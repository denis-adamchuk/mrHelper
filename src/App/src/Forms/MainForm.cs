﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Client.Repository;
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
      ILocalGitRepositoryFactoryAccessor,
      IWorkflowEventNotifier
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

      public Task<ILocalGitRepositoryFactory> GetFactory()
      {
         return getLocalGitRepositoryFactory(Program.Settings.LocalGitFolder);
      }

      public event Action<string, User, IEnumerable<Project>> Connected;
      public event Action<string, Project, IEnumerable<MergeRequest>> LoadedMergeRequests;
      public event Action<string, string, MergeRequest, GitLabSharp.Entities.Version> LoadedMergeRequestVersion;

      private readonly System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
      {
         Interval = timeTrackingTimerInterval
      };

      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private bool _userIsMovingSplitter1 = false;
      private bool _userIsMovingSplitter2 = false;
      private readonly TrayIcon _trayIcon;
      private readonly Markdig.MarkdownPipeline _mergeRequestDescriptionMarkdownPipeline;
      private bool _canSwitchTab = true;
      private bool _notifyOnCommitChainCancelEnabled;

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

      private WorkflowManager _searchWorkflowManager;

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

      private struct CommitComboBoxItem
      {
         internal string SHA;
         internal string Text;
         internal bool IsLatest;
         internal bool IsBase;
         internal DateTime? TimeStamp;
         internal string Message;

         public override string ToString()
         {
            return Text;
         }

         internal CommitComboBoxItem(string sha, string text, DateTime? timeStamp, string message)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
            IsLatest = false;
            IsBase = false;
            Message = message;
         }

         internal CommitComboBoxItem(Commit commit)
            : this(commit.Id, commit.Title, commit.Created_At, commit.Message)
         {
         }
      }

      private MergeRequestCache _mergeRequestCache;

      private struct ListViewSubItemInfo
      {
         public ListViewSubItemInfo(Func<string> getText, Func<string> getUrl)
         {
            _getText = getText;
            _getUrl = getUrl;
         }

         public bool Clickable => _getUrl() != String.Empty;
         public string Text => _getText();
         public string Url => _getUrl();

         private readonly Func<string> _getText;
         private readonly Func<string> _getUrl;
      }

      private readonly Dictionary<string, User> _currentUser = new Dictionary<string, User>();
   }
}

