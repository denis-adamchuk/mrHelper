using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using mrHelper.App.Controls;
using System.Drawing;

namespace mrHelper.App.Forms
{
   delegate void UpdateTextCallback(string text);

   internal partial class MainForm : Form, ICommandCallback
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;

      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      private void SetHeight(ListView listView, int height)
      {
         ImageList imgList = new ImageList();
         imgList.ImageSize = new Size(1, height);
         listView.SmallImageList = imgList;
      }

      internal MainForm()
      {
         InitializeComponent();

         SetHeight(listViewMergeRequests, listViewMergeRequests.Font.Height * 2 + 2);
      }

      public string GetCurrentHostName()
      {
         return getHostName();
      }

      public string GetCurrentAccessToken()
      {
         return Tools.GetAccessToken(getHostName(), _settings);
      }

      public string GetCurrentProjectName()
      {
         return getMergeRequestKey().Value.ProjectKey.ProjectName;
      }

      public int GetCurrentMergeRequestIId()
      {
         return getMergeRequestKey().Value.IId;
      }

      private readonly System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
         {
            Interval = timeTrackingTimerInterval
         };

      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private UserDefinedSettings _settings;

      private WorkflowFactory _workflowFactory;
      private UpdateManager _updateManager;
      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private GitClientFactory _gitClientFactory;
      private GitClientInteractiveUpdater _gitClientUpdater;
      private PersistentStorage _persistentStorage;

      private string _initialHostName = String.Empty;
      private Dictionary<MergeRequestKey, HashSet<string>> _reviewedCommits =
         new Dictionary<MergeRequestKey, HashSet<string>>();
      private Workflow _workflow;
      private ExpressionResolver _expressionResolver;
      private TimeTracker _timeTracker;

      private ColorScheme _colorScheme;

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
      }

      private User? _currentUser;
   }
}

