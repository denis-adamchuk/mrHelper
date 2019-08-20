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
using mrHelper.Client.TimeTracking;

namespace mrHelper.App.Forms
{
   delegate void UpdateTextCallback(object sender, string text);

   internal partial class mrHelperForm : Form, ICommandCallback
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

      internal mrHelperForm()
      {
         InitializeComponent();
      }

      public string GetCurrentHostName()
      {
         return _workflow.State.HostName;
      }

      public string GetCurrentAccessToken()
      {
         return Tools.GetAccessToken(_workflow.State.HostName, _settings);
      }

      public string GetCurrentProjectName()
      {
         return _workflow.State.Project.Path_With_Namespace;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _workflow.State.MergeRequest.IId;
      }

      private System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
         {
            Interval = timeTrackingTimerInterval
         };

      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private UserDefinedSettings _settings;

      private WorkflowManager _workflowManager;
      private UpdateManager _updateManager;
      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private GitClientFactory _gitClientFactory;
      private GitClientInteractiveUpdater _gitClientUpdater;

      private Workflow _workflow;
      private WorkflowUpdateChecker _workflowUpdateChecker;
      private TimeTracker _timeTracker;

      private ColorScheme _colorScheme = new ColorScheme();

      private struct HostComboBoxItem
      {
         internal string Host;
         internal string AccessToken;
      }

      private struct VersionComboBoxItem
      {
         internal string SHA;
         internal string Text;
         internal bool IsLatest;
         internal DateTime? TimeStamp;

         public override string ToString()
         {
            return Text;
         }

         internal VersionComboBoxItem(string sha, string text, DateTime? timeStamp)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
            IsLatest = false;
         }

         internal VersionComboBoxItem(GitLabSharp.Entities.Version ver)
            : this(ver.Head_Commit_SHA, ver.Head_Commit_SHA.Substring(0, 10), ver.Created_At)
         {
         }
      }
   }
}

