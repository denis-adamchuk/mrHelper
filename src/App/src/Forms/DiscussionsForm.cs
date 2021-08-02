using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.StorageSupport;
using mrHelper.App.Helpers.GitLab;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.CustomActions;
using mrHelper.Core.Context;

namespace mrHelper.App.Forms
{
   public delegate bool IsCommandEnabledFn(ICommand command, out bool isVisible);

   internal partial class DiscussionsForm : CustomFontForm, ICommandCallback
   {
      public DiscussionsForm(
         IGitCommandService git, User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         string mergeRequestTitle, User mergeRequestAuthor,
         ColorScheme colorScheme, AsyncDiscussionLoader discussionLoader, AsyncDiscussionHelper discussionHelper,
         string webUrl, Shortcuts shortcuts,
         IEnumerable<ICommand> commands, IsCommandEnabledFn isCommandEnabled)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mergeRequestTitle;

         _discussionLoader = discussionLoader;
         _discussionLoader.StatusChanged += onDiscussionLoaderStatusChanged;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();

         applyFont(Program.Settings.MainWindowFontSizeName);

         var discussionLayout = new DiscussionLayout(
            ConfigurationHelper.GetDiffContextPosition(Program.Settings),
            ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings),
            Program.Settings.NeedShiftReplies,
            new ContextDepth(0, Program.Settings.DiffContextDepth));
         _discussionLayout = discussionLayout;
         _discussionLayout.DiffContextPositionChanged += updateSaveDefaultLayoutState;
         _discussionLayout.DiscussionColumnWidthChanged += updateSaveDefaultLayoutState;
         _discussionLayout.NeedShiftRepliesChanged += updateSaveDefaultLayoutState;
         updateSaveDefaultLayoutState();

         Program.Settings.DiffContextPositionChanged += updateSaveDefaultLayoutState;
         Program.Settings.DiscussionColumnWidthChanged += updateSaveDefaultLayoutState;
         Program.Settings.NeedShiftRepliesChanged += updateSaveDefaultLayoutState;

         var displayFilter = new DiscussionFilter(currentUser, mergeRequestAuthor, DiscussionFilterState.Default);
         var discussionSort = new DiscussionSort(DiscussionSortState.Default);

         // Includes making some boxes visible. This does not paint them because their parent (Form) is hidden so far.
         discussionPanel.Initialize(discussionSort, displayFilter, discussionLoader, discussions,
            shortcuts, git, colorScheme, mrk, mergeRequestAuthor, currentUser, discussionLayout);
         if (discussionPanel.DiscussionCount < 1)
         {
            throw new NoDiscussionsToShow();
         }

         searchPanel.Initialize(discussionPanel);

         discussionMenu.Initialize(discussionSort, displayFilter, discussionLayout,
            discussionLoader, discussionHelper, commands, this, applyFont, colorScheme, isCommandEnabled);

         linkLabelGitLabURL.Text = webUrl;
         toolTip.SetToolTip(linkLabelGitLabURL, webUrl);
         linkLabelGitLabURL.SetLinkLabelClicked(UrlHelper.OpenBrowser);

         Text = DefaultCaption;
         MainMenuStrip = discussionMenu.MenuStrip;
      }

      internal void Restore()
      {
         this.WindowState = _previousState;
         Activate();
         _discussionLoader.LoadDiscussions();
      }

      // Logging stub
      protected override void OnHandleCreated(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnHandleCreated()...");
         base.OnHandleCreated(e);
      }

      // Logging stub
      protected override void OnBindingContextChanged(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnBindingContextChanged()...");
         base.OnBindingContextChanged(e);
      }

      // Logging stub
      protected override void OnLoad(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnLoad()...");
         base.OnLoad(e);
      }

      // Logging stub
      protected override void OnVisibleChanged(EventArgs e)
      {
         Trace.TraceInformation(String.Format("[DiscussionsForm] Processing OnVisibleChanged({0})...", Visible.ToString()));
         base.OnVisibleChanged(e);
      }

      // Logging stub
      protected override void OnActivated(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnActivated()...");
         base.OnActivated(e);
      }

      // Logging stub
      protected override void OnShown(EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsForm] Processing OnShown()...");
         base.OnShown(e);
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
         if (ActiveControl != searchPanel)
         {
            discussionPanel.ProcessKeyDown(e);
            if (e.Handled)
            {
               return;
            }
         }

         searchPanel.ProcessKeyDown(e);
         if (e.Handled)
         {
            return;
         }

         base.OnKeyDown(e); // menu shortcuts
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
         if (this.WindowState != FormWindowState.Minimized)
         {
            _previousState = this.WindowState;
         }
      }

      private void onSaveAsDefaultClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         ConfigurationHelper.SetDiffContextPosition(Program.Settings, _discussionLayout.DiffContextPosition);
         ConfigurationHelper.SetDiscussionColumnWidth(Program.Settings, _discussionLayout.DiscussionColumnWidth);
         Program.Settings.NeedShiftReplies = _discussionLayout.NeedShiftReplies;
         updateSaveDefaultLayoutState();
      }

      private void onDiscussionLoaderStatusChanged(string status)
      {
         Text = String.IsNullOrEmpty(status)
            ? DefaultCaption
            : String.Format("{0}   ({1})", DefaultCaption, status);
      }

      private void updateSaveDefaultLayoutState()
      {
         var currentDiffContextPosition = _discussionLayout.DiffContextPosition;
         var diffContextPositionDefault = ConfigurationHelper.GetDiffContextPosition(Program.Settings);

         var currentDiscussionColumnWidth = _discussionLayout.DiscussionColumnWidth;
         var discussionColumnWidthDefault = ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings);

         var currentNeedShiftReplies = _discussionLayout.NeedShiftReplies;
         var needShiftRepliesDefault = Program.Settings.NeedShiftReplies;

         bool needEnableControl = diffContextPositionDefault   != currentDiffContextPosition
                               || discussionColumnWidthDefault != currentDiscussionColumnWidth
                               || needShiftRepliesDefault      != currentNeedShiftReplies;
         linkLabelSaveAsDefaultLayout.Visible = needEnableControl;
      }

      private string DefaultCaption
      {
         get => String.Format(
            "Discussions for merge request !{0} in {1} -- \"{2}\"",
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestTitle);
      }

      // ICommandCallback
      public string GetCurrentHostName()
      {
         return _mergeRequestKey.ProjectKey.HostName;
      }

      // ICommandCallback
      public string GetCurrentAccessToken()
      {
         return Program.Settings.GetAccessToken(_mergeRequestKey.ProjectKey.HostName);
      }

      // ICommandCallback
      public string GetCurrentProjectName()
      {
         return _mergeRequestKey.ProjectKey.ProjectName;
      }

      // ICommandCallback
      public int GetCurrentMergeRequestIId()
      {
         return _mergeRequestKey.IId;
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly AsyncDiscussionLoader _discussionLoader;
      private readonly DiscussionLayout _discussionLayout;
      private FormWindowState _previousState;
   }

   internal class NoDiscussionsToShow : Exception { };
}

