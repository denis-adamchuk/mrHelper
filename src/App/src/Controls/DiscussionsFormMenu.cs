using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Constants;
using mrHelper.App.Forms.Helpers;
using mrHelper.CommonControls.Tools;
using mrHelper.App.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionsFormMenu : UserControl
   {
      public DiscussionsFormMenu()
      {
         InitializeComponent();

         _sortMenuItemGroup = new ToolStripMenuItem[] {
            defaultToolStripMenuItem,
            reverseToolStripMenuItem,
            byReviewerToolStripMenuItem
         };

         _filterByAnswersMenuItemGroup = new ToolStripMenuItem[] {
            showAnsweredAndUnansweredThreadsToolStripMenuItem,
            showAnsweredThreadsOnlyToolStripMenuItem,
            showUnansweredThreadsOnlyToolStripMenuItem
         };

         _filterByResolutionMenuItemGroup = new ToolStripMenuItem[] {
            showResolvedAndNotResolvedThreadsToolStripMenuItem,
            showResolvedThreadsOnlyToolStripMenuItem,
            showNotResolvedThreadsOnlyToolStripMenuItem
         };

         Font = menuStrip.Font;
      }

      internal MenuStrip MenuStrip => menuStrip;

      internal void Initialize(
         DiscussionSort discussionSort,
         DiscussionFilter displayFilter,
         DiscussionLayout discussionLayout,
         AsyncDiscussionHelper discussionHelper,
         IEnumerable<ICommand> commands,
         Action<string> onFontSelected,
         ColorScheme colorScheme,
         Func<ICommand, CommandState> isCommandEnabled,
         Func<ICommand, Task> onCommand,
         Action onRefresh)
      {
         _loadingConfiguration = true;
         _discussionSort = discussionSort;
         setDiscussionSortStateInControls(_discussionSort.SortState);

         _displayFilter = displayFilter;
         setDiscussionFilterStateInControls(_displayFilter.FilterState);
         _displayFilter.FilterStateChanged += () => setDiscussionFilterStateInControls(_displayFilter.FilterState);

         _discussionLayout = discussionLayout;
         setDiscussionLayoutStateInControls();
         updateColumnWidthSizeMenuItemState();

         emulateNativeLineBreaksToolStripMenuItem.Checked = Program.Settings.EmulateNativeLineBreaksInDiscussions;

         _onRefresh = onRefresh;
         _discussionHelper = discussionHelper;

         _isCommandEnabled = isCommandEnabled;
         _onCommand = onCommand;
         addCustomActions(commands);

         addFontSizes();
         _onFontSelected = onFontSelected;

         _colorScheme = colorScheme;

         _loadingConfiguration = false;
      }

      internal void OnMergeRequestEvent()
      {
         updateCustomActionVisibility();
      }

      private void onConfigureColorsClicked(object sender, EventArgs e)
      {
         using (ConfigureColorsForm form = new ConfigureColorsForm(DefaultCategory.Discussions, _colorScheme))
         {
            WinFormsHelpers.ShowDialogOnControl(form, this.ParentForm);
         }
      }

      private void onRefreshMenuItemClicked(object sender, EventArgs e)
      {
         Trace.TraceInformation("[DiscussionsFormMenu] Refreshing by user request");
         onRefreshAction();
      }

      private void onStartAThreadMenuItemClicked(object sender, EventArgs e)
      {
         onAddThreadAction();
      }

      private void onAddACommentMenuItemClicked(object sender, EventArgs e)
      {
         onAddCommentAction();
      }

      private void onSortMenuItemCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(_sortMenuItemGroup, checkBox);
         checkBox.CheckOnClick = false;
         _discussionSort.SortState = getDiscussionSortStateFromControls();
      }

      private void onFilterByAnswersCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(_filterByAnswersMenuItemGroup, checkBox);
         checkBox.CheckOnClick = false;
         _displayFilter.FilterState = getDisplayFilterStateFromControls();
      }

      private void onFilterByResolutionCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(_filterByResolutionMenuItemGroup, checkBox);
         checkBox.CheckOnClick = false;
         _displayFilter.FilterState = getDisplayFilterStateFromControls();
      }

      private void onFilterMenuItemCheckedChanged(object sender, EventArgs e)
      {
         _displayFilter.FilterState = getDisplayFilterStateFromControls();
      }

      private void onFontMenuItemCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(fontSizeToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         _onFontSelected(checkBox.Text);
      }

      private void onIncreaseColumnWidthClicked(object sender, EventArgs e)
      {
         ConfigurationHelper.DiscussionColumnWidth currentWidth = _discussionLayout.DiscussionColumnWidth;
         var nextWidth = ConfigurationHelper.GetNextColumnWidth(currentWidth);
         _discussionLayout.DiscussionColumnWidth = nextWidth;
         updateColumnWidthSizeMenuItemState();
      }

      private void onDecreaseColumnWidthClicked(object sender, EventArgs e)
      {
         ConfigurationHelper.DiscussionColumnWidth currentWidth = _discussionLayout.DiscussionColumnWidth;
         var prevWidth = ConfigurationHelper.GetPrevColumnWidth(currentWidth);
         _discussionLayout.DiscussionColumnWidth = prevWidth;
         updateColumnWidthSizeMenuItemState();
      }

      private void updateColumnWidthSizeMenuItemState()
      {
         ConfigurationHelper.DiscussionColumnWidth currentWidth = _discussionLayout.DiscussionColumnWidth;

         var prevWidth = ConfigurationHelper.GetPrevColumnWidth(currentWidth);
         bool canDecrease = currentWidth != prevWidth;
         decreaseColumnWidthToolStripMenuItem.Enabled = canDecrease;

         var nextWidth = ConfigurationHelper.GetNextColumnWidth(currentWidth);
         bool canIncrease = currentWidth != nextWidth;
         increaseColumnWidthToolStripMenuItem.Enabled = canIncrease;
      }

      private void onDiffContextPositionCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(diffContextPositionToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         _discussionLayout.DiffContextPosition = getDiffContextPositionFromControls();
      }

      private void onDiffContextDepthCheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (_loadingConfiguration || !checkBox.Checked || !int.TryParse(checkBox.Text, out int diffContextDepthInteger))
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(diffContextDepthToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         Program.Settings.DiffContextDepth = diffContextDepthInteger;
         _discussionLayout.DiffContextDepth = new Core.Context.ContextDepth(0, diffContextDepthInteger);
      }

      private void onFlatListOfRepliesCheckedChanged(object sender, EventArgs e)
      {
         _discussionLayout.NeedShiftReplies = !flatListOfRepliesToolStripMenuItem.Checked;
      }

      private void onEmulateNativeLineBreaksCheckedChanged(Object sender, EventArgs e)
      {
         Program.Settings.EmulateNativeLineBreaksInDiscussions = emulateNativeLineBreaksToolStripMenuItem.Checked;
         if (!_loadingConfiguration)
         {
            MessageBox.Show("New setting will apply once Discussions view is reopened",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
         }
      }

      private void onRefreshAction()
      {
         _onRefresh?.Invoke();
      }

      private void onAddThreadAction()
      {
         BeginInvoke(new Action(async () =>
         {
            if (await _discussionHelper.AddThreadAsync(ParentForm))
            {
               Trace.TraceInformation("[DiscussionsFormMenu] onAddThreadAction()");
               onRefreshAction();
            }
         }));
      }

      private void onAddCommentAction()
      {
         BeginInvoke(new Action(async () =>
         {
            if (await _discussionHelper.AddCommentAsync(ParentForm))
            {
               Trace.TraceInformation("[DiscussionsFormMenu] onAddCommentAction()");
               onRefreshAction();
            }
         }));
      }

      private void onCommandAction(ICommand command)
      {
         Trace.TraceInformation("[DiscussionsFormMenu] onCommandAction({0})", command.Name);
         BeginInvoke(new Action(async () =>
         {
            await _onCommand(command);
            onRefreshAction();
         }));
      }

      private void updateCustomActionVisibility()
      {
         foreach (ToolStripItem menuItem in actionsToolStripMenuItem.DropDownItems)
         {
            ICommand command = (ICommand)menuItem.Tag;
            if (command != null)
            {
               CommandState state = _isCommandEnabled(command);
               menuItem.Enabled = state.Enabled;
               menuItem.Visible = state.Visible;
            }
         }
      }

      private void addCustomActions(IEnumerable<ICommand> commands)
      {
         if (commands == null)
         {
            return;
         }

         int id = 0;
         foreach (ICommand command in commands)
         {
            ToolStripMenuItem item = new ToolStripMenuItem
            {
               AutoToolTip = false,
               Name = "customAction" + id,
               Tag = command,
               Text = command.Name,
               ToolTipText = command.Hint
            };
            item.Click += (x, y) => onCommandAction(command);
            actionsToolStripMenuItem.DropDownItems.Add(item);
            id++;
         }

         updateCustomActionVisibility();
      }

      private void addFontSizes()
      {
         foreach (string fontSizeChoice in Constants.DiscussionsWindowFontSizeChoices)
         {
            bool isDefaultFont = fontSizeChoice == Program.Settings.MainWindowFontSizeName;
            ToolStripMenuItem item = new ToolStripMenuItem
            {
               Name = "fontSize" + fontSizeChoice,
               Text = fontSizeChoice,
               CheckOnClick = !isDefaultFont,
               Checked = isDefaultFont
            };
            item.CheckedChanged += onFontMenuItemCheckedChanged;
            fontSizeToolStripMenuItem.DropDownItems.Add(item);
         }
      }

      DiscussionSortState getDiscussionSortStateFromControls()
      {
         if (defaultToolStripMenuItem.Checked)
         {
            return DiscussionSortState.Default;
         }
         else if (reverseToolStripMenuItem.Checked)
         {
            return DiscussionSortState.Reverse;
         }
         else if (byReviewerToolStripMenuItem.Checked)
         {
            return DiscussionSortState.ByReviewer;
         }
         Debug.Assert(false);
         return DiscussionSortState.Default;
      }

      DiscussionFilterState getDisplayFilterStateFromControls()
      {
         FilterByAnswers filterByAnswers = 0;
         if (showAnsweredAndUnansweredThreadsToolStripMenuItem.Checked
          || showAnsweredThreadsOnlyToolStripMenuItem.Checked)
         {
            filterByAnswers |= FilterByAnswers.Answered;
         }
         if (showAnsweredAndUnansweredThreadsToolStripMenuItem.Checked
          || showUnansweredThreadsOnlyToolStripMenuItem.Checked)
         {
            filterByAnswers |= FilterByAnswers.Unanswered;
         }

         FilterByResolution filterByResolution = 0;
         if (showResolvedAndNotResolvedThreadsToolStripMenuItem.Checked
          || showResolvedThreadsOnlyToolStripMenuItem.Checked)
         {
            filterByResolution |= FilterByResolution.Resolved;
         }
         if (showResolvedAndNotResolvedThreadsToolStripMenuItem.Checked
          || showNotResolvedThreadsOnlyToolStripMenuItem.Checked)
         {
            filterByResolution |= FilterByResolution.NotResolved;
         }

         return new DiscussionFilterState
            (
               showThreadsStartedByMeOnlyToolStripMenuItem.Checked,
               showServiceMessagesToolStripMenuItem.Checked,
               filterByAnswers,
               filterByResolution,
               null,
               0
            );
      }

      ConfigurationHelper.DiffContextPosition getDiffContextPositionFromControls()
      {
         if (topToolStripMenuItem.Checked)
         {
            return ConfigurationHelper.DiffContextPosition.Top;
         }
         else if (leftToolStripMenuItem.Checked)
         {
            return ConfigurationHelper.DiffContextPosition.Left;
         }
         else if (rightToolStripMenuItem.Checked)
         {
            return ConfigurationHelper.DiffContextPosition.Right;
         }
         Debug.Assert(false);
         return ConfigurationHelper.DiffContextPosition.Top;
      }

      void setDiscussionSortStateInControls(DiscussionSortState state)
      {
         if (state == DiscussionSortState.Default)
         {
            checkGroupItem(defaultToolStripMenuItem);
         }
         else if (state == DiscussionSortState.Reverse)
         {
            checkGroupItem(reverseToolStripMenuItem);
         }
         else if (state == DiscussionSortState.ByReviewer)
         {
            checkGroupItem(byReviewerToolStripMenuItem);
         }
      }

      void setDiscussionFilterStateInControls(DiscussionFilterState state)
      {
         if (state.ByAnswers.HasFlag(FilterByAnswers.Answered)
          && state.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            checkGroupItem(showAnsweredAndUnansweredThreadsToolStripMenuItem);
         }
         else if (state.ByAnswers.HasFlag(FilterByAnswers.Answered))
         {
            checkGroupItem(showAnsweredThreadsOnlyToolStripMenuItem);
         }
         else if (state.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            checkGroupItem(showUnansweredThreadsOnlyToolStripMenuItem);
         }

         if (state.ByResolution.HasFlag(FilterByResolution.Resolved)
          && state.ByResolution.HasFlag(FilterByResolution.NotResolved))
         {
            checkGroupItem(showResolvedAndNotResolvedThreadsToolStripMenuItem);
         }
         else if (state.ByResolution.HasFlag(FilterByResolution.Resolved))
         {
            checkGroupItem(showResolvedThreadsOnlyToolStripMenuItem);
         }
         else if (state.ByResolution.HasFlag(FilterByResolution.NotResolved))
         {
            checkGroupItem(showNotResolvedThreadsOnlyToolStripMenuItem);
         }

         showThreadsStartedByMeOnlyToolStripMenuItem.Checked = state.ByCurrentUserOnly;
         showServiceMessagesToolStripMenuItem.Checked = state.ServiceMessages;
      }

      private void setDiscussionLayoutStateInControls()
      {
         if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Top)
         {
            checkGroupItem(topToolStripMenuItem);
         }
         else if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Left)
         {
            checkGroupItem(leftToolStripMenuItem);
         }
         else if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Right)
         {
            checkGroupItem(rightToolStripMenuItem);
         }

         flatListOfRepliesToolStripMenuItem.Checked = !_discussionLayout.NeedShiftReplies;

         setDiffContextDepthStateInControls();
      }

      private void setDiffContextDepthStateInControls()
      {
         int defaultContextDepth = 2;
         Dictionary<int, ToolStripMenuItem> kv = new Dictionary<int, ToolStripMenuItem>
         {
            { 0, toolStripMenuItemDiffContextDepth0 },
            { 1, toolStripMenuItemDiffContextDepth1 },
            { 2, toolStripMenuItemDiffContextDepth2 },
            { 3, toolStripMenuItemDiffContextDepth3 },
            { 4, toolStripMenuItemDiffContextDepth4 }
         };

         if (!kv.TryGetValue(Program.Settings.DiffContextDepth, out ToolStripMenuItem item))
         {
            item = kv[defaultContextDepth];
         }
         checkGroupItem(item);
      }

      private static void checkGroupItem(ToolStripMenuItem checkBox)
      {
         checkBox.Checked = true;
         checkBox.CheckOnClick = true;
      }

      private DiscussionSort _discussionSort;
      private DiscussionFilter _displayFilter; // filters out discussions by user preferences
      private DiscussionLayout _discussionLayout;
      private Action _onRefresh;
      private AsyncDiscussionHelper _discussionHelper;
      private Action<string> _onFontSelected;
      private Func<ICommand, CommandState> _isCommandEnabled;
      private Func<ICommand, Task> _onCommand;
      private ColorScheme _colorScheme;
      private bool _loadingConfiguration;
      private readonly ToolStripMenuItem[] _sortMenuItemGroup;
      private readonly ToolStripMenuItem[] _filterByAnswersMenuItemGroup;
      private readonly ToolStripMenuItem[] _filterByResolutionMenuItemGroup;
   }
}

