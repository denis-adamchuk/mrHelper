﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
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
   internal partial class DiscussionsForm : ThemedForm
   {
      internal DiscussionsForm(
         IGitCommandService git,
         User currentUser,
         MergeRequestKey mrk,
         IEnumerable<Discussion> discussions,
         string mergeRequestTitle,
         User mergeRequestAuthor,
         AsyncDiscussionLoader discussionLoader,
         AsyncDiscussionHelper discussionHelper,
         string webUrl,
         Shortcuts shortcuts,
         IEnumerable<ICommand> commands,
         Func<ICommand, CommandState> isCommandEnabled,
         Func<ICommand, Task> onCommand,
         Action onRefresh,
         AvatarImageCache avatarImageCache,
         Action<string> onSelectNoteByUrl,
         IEnumerable<User> fullUserList,
         IEnumerable<Project> fullProjectList)
      {
         _mergeRequestKey = mrk;
         _mergeRequestTitle = mergeRequestTitle;

         _onRefresh = onRefresh;
         _discussionLoader = discussionLoader;
         _discussionLoader.StatusChanged += onDiscussionLoaderStatusChanged;
         _discussionLoader.Loaded += _ => hideReapplyFilter();

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
         _pageFilter = new DiscussionFilter(currentUser, mergeRequestAuthor, DiscussionFilterState.Default);
         _searchFilter = new DiscussionFilter(currentUser, mergeRequestAuthor, DiscussionFilterState.Default);
         var discussionSort = new DiscussionSort(DiscussionSortState.Default);

         // Includes making some boxes visible. This does not paint them because their parent (Form) is hidden so far.
         discussionPanel.Initialize(discussionSort, displayFilter, _pageFilter, _searchFilter, discussionLoader, discussions,
            shortcuts, git, mrk, mergeRequestAuthor, currentUser, discussionLayout, avatarImageCache,
            webUrl, onSelectNoteByUrl, fullUserList, fullProjectList, contentChanged);
         discussionPanel.ContentMismatchesFilter += showReapplyFilter;
         discussionPanel.ContentMatchesFilter += hideReapplyFilter;
         discussionPanel.PageCountChanged += onPageCountChanged;
         discussionPanel.PageSizeChanged += onPageSizeChanged;
         discussionPanel.PageChangeRequest += onPageChangeRequest;
         if (discussionPanel.DiscussionCount < 1)
         {
            throw new NoDiscussionsToShow();
         }

         searchPanel.Initialize(discussionPanel, onSearchResult);

         discussionMenu.Initialize(discussionSort, displayFilter, discussionLayout,
            discussionHelper, commands, applyFont, isCommandEnabled, onCommand, onRefreshByUser);

         linkLabelGitLabURL.Text = webUrl;
         toolTip.SetToolTip(linkLabelGitLabURL, webUrl);
         linkLabelGitLabURL.SetLinkLabelClicked(Common.Tools.UrlHelper.OpenBrowser);

         Text = DefaultCaption;
         MainMenuStrip = discussionMenu.MenuStrip;

         updatePageNavigationButtonState();
         updatePageNavigationButtonTooltip();
      }

      internal void SelectNote(int noteId)
      {
         discussionPanel.SelectNoteById(noteId, null, App.Controls.DiscussionPanel.ESelectStyle.Flickering);
      }

      internal void OnMergeRequestEvent()
      {
         discussionMenu.OnMergeRequestEvent();
      }

      internal void Restore()
      {
         if (this.WindowState != _previousState)
         {
            this.WindowState = _previousState;
         }
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

      private void contentChanged(bool needRestartSearch)
      {
         if (needRestartSearch)
         {
            searchPanel.RestartSearch();
         }
         else
         {
            searchPanel.RefreshSearch();
         }
      }

      private void onSearchResult(IEnumerable<TextSearchResult> results)
      {
         if (results == null || !searchPanel.NeedShowFoundOnly())
         {
            _searchFilter.FilterState = new DiscussionFilterState(null);
            return;
         }

         IEnumerable<Controls.ITextControl> controls = results.Select(r => r.Control);
         IEnumerable<Discussion> discussions = discussionPanel.CollectDiscussionsForControls(controls);
         _searchFilter.FilterState = new DiscussionFilterState(discussions.ToArray());
      }

      private void onRefreshByUser()
      {
         _discussionLoader.LoadDiscussions();
         _onRefresh?.Invoke();
      }

      private void onSaveAsDefaultClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         ConfigurationHelper.SetDiffContextPosition(Program.Settings, _discussionLayout.DiffContextPosition);
         ConfigurationHelper.SetDiscussionColumnWidth(Program.Settings, _discussionLayout.DiscussionColumnWidth);
         Program.Settings.NeedShiftReplies = _discussionLayout.NeedShiftReplies;
         updateSaveDefaultLayoutState();
      }

      private void onReaplyFilterClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onRefreshByUser();
         hideReapplyFilter();
      }

      private void onPageCountChanged()
      {
         updatePageNavigationButtonState();
      }

      private void onPageSizeChanged()
      {
         updatePageNavigationButtonTooltip();
      }

      private void onPageChangeRequest(int page)
      {
         if (page == _pageFilter.FilterState.Page)
         {
            return;
         }

         _pageFilter.FilterState = new DiscussionFilterState(
            _pageFilter.FilterState.ByCurrentUserOnly,
            _pageFilter.FilterState.ServiceMessages,
            _pageFilter.FilterState.ByAnswers,
            _pageFilter.FilterState.ByResolution,
            _pageFilter.FilterState.EnabledDiscussions,
            page);
         updatePageNavigationButtonState();
      }

      private void onNextPageClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onPageChangeRequest(_pageFilter.FilterState.Page + 1);
      }

      private void onPrevPageClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onPageChangeRequest(_pageFilter.FilterState.Page - 1);
      }

      private void updatePageNavigationButtonState()
      {
         int pageCount = discussionPanel.PageCount;
         bool needShowButtons = pageCount > 1;
         linkLabelPrevPage.Visible = needShowButtons;
         linkLabelNextPage.Visible = needShowButtons;

         int currentPage = _pageFilter.FilterState.Page;
         bool needEnablePrevButton = currentPage != 0;
         linkLabelPrevPage.Enabled = needEnablePrevButton;
         bool needEnableNextButton = currentPage != pageCount - 1;
         linkLabelNextPage.Enabled = needEnableNextButton;
      }

      private void updatePageNavigationButtonTooltip()
      {
         toolTip.SetToolTip(linkLabelPrevPage,
            String.Format("Show previous {0} discussions", discussionPanel.PageSize));
         toolTip.SetToolTip(linkLabelNextPage,
            String.Format("Show next {0} discussions", discussionPanel.PageSize));
      }

      private void showReapplyFilter()
      {
         linkLabelReapplyFilter.Visible = true;
      }

      private void hideReapplyFilter()
      {
         linkLabelReapplyFilter.Visible = false;
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
         get => String.Format("\"{0}\" ({1})", _mergeRequestTitle, _mergeRequestKey.ProjectKey.ProjectName);
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _mergeRequestTitle;
      private readonly Action _onRefresh;
      private readonly AsyncDiscussionLoader _discussionLoader;
      private readonly DiscussionLayout _discussionLayout;
      private FormWindowState _previousState;
      private DiscussionFilter _pageFilter;
      private DiscussionFilter _searchFilter;
   }

   internal class NoDiscussionsToShow : Exception { };
}

