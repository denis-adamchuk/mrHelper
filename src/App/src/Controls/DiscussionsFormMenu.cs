using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Constants;
using mrHelper.App.Forms.Helpers;

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

         _diffContextPositionMenuItemGroup = new ToolStripMenuItem[] {
            topToolStripMenuItem,
            leftToolStripMenuItem,
            rightToolStripMenuItem
         };

         Font = menuStrip.Font;
      }

      internal MenuStrip MenuStrip => menuStrip;

      internal void Initialize(
         DiscussionSort discussionSort,
         DiscussionFilter displayFilter,
         DiscussionLayout discussionLayout,
         AsyncDiscussionLoader discussionLoader,
         AsyncDiscussionHelper discussionHelper,
         IEnumerable<ICommand> commands,
         Action<string> onFontSelected)
      {
         _discussionSort = discussionSort;
         setDiscussionSortStateInControls(_discussionSort.SortState);

         _displayFilter = displayFilter;
         setDiscussionFilterStateInControls(_displayFilter.FilterState);

         _discussionLayout = discussionLayout;
         setDiscussionLayoutStateInControls();
         updateColumnWidthSizeMenuItemState();

         _discussionLoader = discussionLoader;
         _discussionHelper = discussionHelper;

         addCustomActions(commands);

         addFontSizes();
         _onFontSelected = onFontSelected;
      }

      private void onRefreshMenuItemClicked(object sender, EventArgs e)
      {
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

         uncheckAllExceptOne(_sortMenuItemGroup, checkBox);
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

         uncheckAllExceptOne(_filterByAnswersMenuItemGroup, checkBox);
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

         uncheckAllExceptOne(_filterByResolutionMenuItemGroup, checkBox);
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

         uncheckAllExceptOne(fontSizeToolStripMenuItem.DropDownItems
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

         uncheckAllExceptOne(diffContextPositionToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         _discussionLayout.DiffContextPosition = getDiffContextPositionFromControls();
      }

      private void onFlatListOfRepliesCheckedChanged(object sender, EventArgs e)
      {
         _discussionLayout.NeedShiftReplies = !flatListOfRepliesToolStripMenuItem.Checked;
      }

      private void onRefreshAction()
      {
         Trace.TraceInformation("[DiscussionsFormMenu] Refreshing by user request");
         _discussionLoader.LoadDiscussions();
      }

      private void onAddThreadAction()
      {
         BeginInvoke(new Action(async () =>
         {
            if (await _discussionHelper.AddThreadAsync())
            {
               onRefreshAction();
            }
         }));
      }

      private void onAddCommentAction()
      {
         BeginInvoke(new Action(async () =>
         {
            if (await _discussionHelper.AddCommentAsync())
            {
               onRefreshAction();
            }
         }));
      }

      private void onCommandAction(ICommand command)
      {
         BeginInvoke(new Action(async () =>
         {
            try
            {
               await command.Run();
            }
            catch (Exception ex) // Exception type does not matter
            {
               string errorMessage = "Custom action failed";
               ExceptionHandlers.Handle(errorMessage, ex);
               MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            onRefreshAction();
         }));
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
               Name = "customAction" + id,
               Text = String.Format("{0} ({1})", command.Hint, command.Name)
            };
            item.Click += (x, y) => onCommandAction(command);
            actionsToolStripMenuItem.DropDownItems.Add(item);
            id++;
         }
      }

      private void addFontSizes()
      {
         foreach (string fontSizeChoice in Constants.DiscussionsWindowFontSizeChoices)
         {
            bool isDefaultFont = fontSizeChoice == Constants.DefaultMainWindowFontSizeChoice;
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
               filterByResolution
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
            checkGroupItem(_sortMenuItemGroup, defaultToolStripMenuItem);
         }
         else if (state == DiscussionSortState.Reverse)
         {
            checkGroupItem(_sortMenuItemGroup, reverseToolStripMenuItem);
         }
         else if (state == DiscussionSortState.ByReviewer)
         {
            checkGroupItem(_sortMenuItemGroup, byReviewerToolStripMenuItem);
         }
      }

      void setDiscussionFilterStateInControls(DiscussionFilterState state)
      {
         if (state.ByAnswers.HasFlag(FilterByAnswers.Answered)
          && state.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            checkGroupItem(_filterByAnswersMenuItemGroup, showAnsweredAndUnansweredThreadsToolStripMenuItem);
         }
         else if (state.ByAnswers.HasFlag(FilterByAnswers.Answered))
         {
            checkGroupItem(_filterByAnswersMenuItemGroup, showAnsweredThreadsOnlyToolStripMenuItem);
         }
         else if (state.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            checkGroupItem(_filterByAnswersMenuItemGroup, showUnansweredThreadsOnlyToolStripMenuItem);
         }

         if (state.ByResolution.HasFlag(FilterByResolution.Resolved)
          && state.ByResolution.HasFlag(FilterByResolution.NotResolved))
         {
            checkGroupItem(_filterByResolutionMenuItemGroup, showResolvedAndNotResolvedThreadsToolStripMenuItem);
         }
         else if (state.ByResolution.HasFlag(FilterByResolution.Resolved))
         {
            checkGroupItem(_filterByResolutionMenuItemGroup, showResolvedThreadsOnlyToolStripMenuItem);
         }
         else if (state.ByResolution.HasFlag(FilterByResolution.NotResolved))
         {
            checkGroupItem(_filterByResolutionMenuItemGroup, showNotResolvedThreadsOnlyToolStripMenuItem);
         }

         showThreadsStartedByMeOnlyToolStripMenuItem.Checked = state.ByCurrentUserOnly;
         showServiceMessagesToolStripMenuItem.Checked = state.ServiceMessages;
      }

      private void setDiscussionLayoutStateInControls()
      {
         if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Top)
         {
            checkGroupItem(_diffContextPositionMenuItemGroup, topToolStripMenuItem);
         }
         else if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Left)
         {
            checkGroupItem(_diffContextPositionMenuItemGroup, leftToolStripMenuItem);
         }
         else if (_discussionLayout.DiffContextPosition == ConfigurationHelper.DiffContextPosition.Right)
         {
            checkGroupItem(_diffContextPositionMenuItemGroup, rightToolStripMenuItem);
         }

         flatListOfRepliesToolStripMenuItem.Checked = !_discussionLayout.NeedShiftReplies;
      }

      private static void checkGroupItem(ToolStripMenuItem[] checkBoxGroup, ToolStripMenuItem checkBox)
      {
         checkBox.Checked = true;
         checkBox.CheckOnClick = true;
         uncheckAllExceptOne(checkBoxGroup, checkBox);
      }

      private static void uncheckAllExceptOne(ToolStripMenuItem[] checkBoxGroup, ToolStripMenuItem checkBox)
      {
         if (checkBoxGroup.Contains(checkBox))
         {
            foreach (ToolStripMenuItem cb in checkBoxGroup)
            {
               if (cb != checkBox)
               {
                  cb.Checked = false;
                  cb.CheckOnClick = true;
               }
            }
         }
      }

      private DiscussionSort _discussionSort;
      private DiscussionFilter _displayFilter; // filters out discussions by user preferences
      private DiscussionLayout _discussionLayout;
      private AsyncDiscussionLoader _discussionLoader;
      private AsyncDiscussionHelper _discussionHelper;
      private Action<string> _onFontSelected;

      private readonly ToolStripMenuItem[] _sortMenuItemGroup;
      private readonly ToolStripMenuItem[] _filterByAnswersMenuItemGroup;
      private readonly ToolStripMenuItem[] _filterByResolutionMenuItemGroup;
      private readonly ToolStripMenuItem[] _diffContextPositionMenuItemGroup;
   }
}

