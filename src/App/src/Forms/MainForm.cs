using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.Persistence;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Services;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : Form, ICommandCallback
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms
      private static readonly int checkForUpdatesTimerInterval = 1000 * 60 * 60 * 4; // 4 hours

      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      internal MainForm()
      {
         InitializeComponent();
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

      private UpdateManager _updateManager;
      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private GitClientFactory _gitClientFactory;
      private GitClientInteractiveUpdater _gitClientUpdater;
      private PersistentStorage _persistentStorage;
      private RevisionCacher _revisionCacher;

      private string _initialHostName = String.Empty;
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedCommits =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Dictionary<string, MergeRequestKey> _lastMergeRequestsByHosts =
         new Dictionary<string, MergeRequestKey>();
      private Workflow _workflow;
      private ExpressionResolver _expressionResolver;
      private TimeTracker _timeTracker;

      private ColorScheme _colorScheme;

      private string _newVersionFilePath;
      private readonly System.Windows.Forms.Timer _checkForUpdatesTimer = new System.Windows.Forms.Timer
      {
         Interval = checkForUpdatesTimerInterval
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

         public override string ToString()
         {
            return Text;
         }

         internal CommitComboBoxItem(string sha, string text, DateTime? timeStamp)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
            IsLatest = false;
            IsBase = false;
         }

         internal CommitComboBoxItem(Commit commit)
            : this(commit.Id, commit.Title, commit.Created_At)
         {
         }
      }

      private struct FullMergeRequestKey
      {
         public string HostName;
         public Project Project;
         public MergeRequest MergeRequest;

         public FullMergeRequestKey(string hostname, Project project, MergeRequest mergeRequest)
         {
            HostName = hostname;
            Project = project;
            MergeRequest = mergeRequest;
         }

         public static bool SameMergeRequest(FullMergeRequestKey fmk1, FullMergeRequestKey fmk2)
         {
            return fmk1.HostName == fmk2.HostName
                && fmk1.Project.Path_With_Namespace == fmk2.Project.Path_With_Namespace
                && fmk1.MergeRequest.IId == fmk2.MergeRequest.IId;
         }
      }

      private readonly List<FullMergeRequestKey> _allMergeRequests = new List<FullMergeRequestKey>();

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

