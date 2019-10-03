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
         return _workflow.State.HostName;
      }

      public string GetCurrentAccessToken()
      {
         return Tools.GetAccessToken(_workflow.State.HostName, _settings);
      }

      public string GetCurrentProjectName()
      {
         return _workflow.State.MergeRequestKey.ProjectKey.ProjectName;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _workflow.State.MergeRequestKey.IId;
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

      private void fillRectangle2(DrawListViewSubItemEventArgs e, Color backColor, bool isSelected)
      {
         if (isSelected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
         }
         else
         {
            using (Brush brush = new SolidBrush(backColor))
            {
               e.Graphics.FillRectangle(brush, e.Bounds);
            }
         }
      }

      private void listViewMergeRequests_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         Tuple<string, MergeRequest> projectAndMergeRequest = (Tuple<string, MergeRequest>)(e.Item.Tag);
         string projectname = projectAndMergeRequest.Item1;
         MergeRequest mergeRequest = projectAndMergeRequest.Item2;

         e.DrawBackground();

         bool isSelected = e.Item.Selected;
         fillRectangle2(e, getMergeRequestColor(mergeRequest), isSelected);

         e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

         switch (e.ColumnIndex)
         {
            case 0:
               e.Graphics.DrawString(mergeRequest.IId.ToString(), e.Item.ListView.Font, textBrush, new PointF(e.Bounds.X, e.Bounds.Y));
               break;
            case 1:
               e.Graphics.DrawString(mergeRequest.Author.Name, e.Item.ListView.Font, textBrush, new PointF(e.Bounds.X, e.Bounds.Y));
               break;
            case 2:
               e.Graphics.DrawString(projectname, e.Item.ListView.Font, textBrush, new PointF(e.Bounds.X, e.Bounds.Y));
               break;
            case 3:
               {
                  string labels = String.Join(", ", mergeRequest.Labels.ToArray());

                  // first row
                  e.Graphics.DrawString(mergeRequest.Title, e.Item.ListView.Font, textBrush, new PointF(e.Bounds.X, e.Bounds.Y));

                  // second row
                  e.Graphics.DrawString(" [" + labels + "]", e.Item.ListView.Font, textBrush,
                     new PointF(e.Bounds.X, e.Bounds.Y + e.Bounds.Height / (float)2));
               }
               break;
         }


         e.DrawFocusRectangle(e.Bounds);
      }

      private void listViewMergeRequests_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
      {
         e.DrawDefault = true;
      }

      private void listViewMergeRequests_DrawItem(object sender, DrawListViewItemEventArgs e)
      {
         //e.DrawDefault = true;
      }

      private void listViewMergeRequests_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         e.ToString();
      }

      async private void listViewMergeRequests_MouseClick(object sender, MouseEventArgs e)
      {
         ListView listView = (sender as ListView);

         if (listView.SelectedItems.Count > 0)
         {
            Tuple<string, MergeRequest> projectAndMergeRequest =
               (Tuple<string, MergeRequest>)(listView.SelectedItems[0].Tag);
            await changeMergeRequestAsync(projectAndMergeRequest.Item2.Id);
         }
      }
   }
}

