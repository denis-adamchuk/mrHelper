using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : CustomFontForm, ICommandCallback
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      internal MainForm()
      {
         InitializeComponent();
         _trayIcon = new TrayIcon(notifyIcon);

         Markdig.Extensions.Tables.PipeTableOptions options = new Markdig.Extensions.Tables.PipeTableOptions
         {
            RequireHeaderSeparator = false
         };
         _mergeRequestDescriptionMarkdownPipeline = Markdig.MarkdownExtensions
            .UsePipeTables(new Markdig.MarkdownPipelineBuilder(), options)
            .Build();

         this.columnHeaderName.Width = this.listViewProjects.Width - SystemInformation.VerticalScrollBarWidth - 5;
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
         return getMergeRequestKey()?.ProjectKey.ProjectName ?? String.Empty;
      }

      public int GetCurrentMergeRequestIId()
      {
         return getMergeRequestKey()?.IId ?? 0;
      }

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

      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private GitClientFactory _gitClientFactory;
      private GitClientInteractiveUpdater _gitClientUpdater;
      private PersistentStorage _persistentStorage;
      private RevisionCacher _revisionCacher;
      private UserNotifier _userNotifier;

      private string _initialHostName = String.Empty;
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedCommits =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private Workflow _workflow;
      private ExpressionResolver _expressionResolver;
      private TimeTracker _timeTracker;

      private IEnumerable<ICommand> _customCommands;
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

      private MergeRequestManager _mergeRequestManager;

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

      private User? _currentUser;
   }
}

