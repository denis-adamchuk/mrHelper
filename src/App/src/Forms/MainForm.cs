﻿using System;
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
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : Form, ICommandCallback
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms
      private static readonly int checkForUpdatesTimerInterval = 1000 * 60 * 60 * 4; // 4 hours

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      internal MainForm()
      {
         InitializeComponent();
         _trayIcon = new TrayIcon(notifyIcon);
      }

      public string GetCurrentHostName()
      {
         return getHostName();
      }

      public string GetCurrentAccessToken()
      {
         return ConfigurationHelper.GetAccessToken(getHostName(), Program.Settings);
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
      private bool _userIsMovingSplitter = false;
      private TrayIcon _trayIcon;

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

      private List<ICommand> _customCommands;
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

