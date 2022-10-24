using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal interface IOperationController
   {
      bool CanDiscussions();
      bool CanDiffTool(DiffToolMode mode);
      bool CanEdit();
      bool CanMerge();
   }

   internal class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(
         IOperationController operationController,
         Action onDiscussions, Action onRefreshList, Action onRefresh, Action onEdit,
         Action onMerge, Action onClose, Action onDiffToBase, Action onDiffDefault,
         Action onMuteUntilTomorrow, Action onMuteUntilMonday, Action onUnmute,
         Action onExclude, Action onOpenAuthorProfile, Action onPin, Action onDefault)
      {
         _discussionsItem = addItem(onDiscussions, "&Discussions", onDiscussions == onDefault);

         if (onDiffToBase != null || onDiffDefault != null)
         {
            addSeparator();
         }

         _diffToolItem = addItem(onDiffDefault, "Diff &Tool", onDiffDefault == onDefault);
         _diffToBaseItem = addItem(onDiffToBase, "Diff to &Base", onDiffToBase == onDefault);

         if (onRefreshList != null || onRefresh != null)
         {
            addSeparator();
         }

         _refreshListItem = addItem(onRefreshList, "&Refresh list", onRefreshList == onDefault);
         addItem(onRefresh, "R&efresh selected", onRefresh == onDefault);

         if (onEdit != null || onMerge != null || onClose != null)
         {
            addSeparator();
         }

         _editItem = addItem(onEdit, "Ed&it...", onEdit == onDefault);
         _mergeItem = addItem(onMerge, "&Merge...", onMerge == onDefault);
         addItem(onClose, "&Close", onClose == onDefault);

         if (onMuteUntilTomorrow != null || onMuteUntilMonday != null || onUnmute != null)
         {
            addSeparator();
         }

         _muteUntilTomorrowItem = addItem(onMuteUntilTomorrow,
            "Don't &highlight until tomorrow", onMuteUntilTomorrow == onDefault);
         _muteUntilMondayItem = addItem(onMuteUntilMonday,
            "Don't &highlight until Monday", onMuteUntilMonday == onDefault);
         _unmuteItem = addItem(onUnmute, "Restore high&light", onUnmute == onDefault);

         if (onExclude != null)
         {
            addSeparator();
         }

         _excludeItem = addItem(onExclude, "Hi&de/Unhi&de", onExclude == onDefault);

         if (onPin != null)
         {
            addSeparator();
         }

         _pinItem = addItem(onPin, "Pin/Unpin", onPin == onDefault);

         if (onOpenAuthorProfile != null)
         {
            addSeparator();
         }

         _openAuthorProfileItem = addItem(onOpenAuthorProfile, "Open author profile...", onOpenAuthorProfile == onDefault);

         _operationController = operationController;
      }

      private ToolStripMenuItem addItem(Action action, string name, bool isDefault)
      {
         if (action == null)
         {
            return null;
         }

         ToolStripMenuItem item = new ToolStripMenuItem(name, null, (s, e) => action());
         Items.Add(item);

         if (isDefault)
         {
            _defaultItem = item;
         }

         return item;
      }

      private void addSeparator()
      {
         Items.Add("-", null, null);
      }

      public void SetUnmuteActionEnabled(bool enabled)
      {
         _isUnmuteActionEnabled = enabled;
      }

      public void SetExcludeAbilityState(bool canBeExcluded)
      {
         _canBeExcluded = canBeExcluded;
      }

      public void SetPinItemText(string pinItemText)
      {
         _pinItemText = pinItemText;
      }

      public void DisableAll()
      {
         _disabledAll = true;
      }

      public void EnableAll()
      {
         _disabledAll = false;
      }

      public void LaunchDefaultAction()
      {
         _defaultItem?.PerformClick();
      }

      public void UpdateItemState()
      {
         foreach (ToolStripItem item in Items)
         {
            item.Enabled = !_disabledAll;
         }

         if (_discussionsItem != null)
         {
            _discussionsItem.Enabled = _operationController.CanDiscussions() && !_disabledAll;
         }

         if (_diffToolItem != null)
         {
            bool canLaunchDefaultDiff =
               _operationController.CanDiffTool(DiffToolMode.DiffBetweenSelected)
            || _operationController.CanDiffTool(DiffToolMode.DiffSelectedToBase);
            _diffToolItem.Enabled = canLaunchDefaultDiff && !_disabledAll;
         }

         if (_diffToBaseItem != null)
         {
            bool canLaunchDiffToBase = _operationController.CanDiffTool(DiffToolMode.DiffSelectedToBase);
            _diffToBaseItem.Enabled = canLaunchDiffToBase && !_disabledAll;
         }

         if (_refreshListItem != null)
         {
            _refreshListItem.Enabled = true;
         }

         if (_editItem != null)
         {
            _editItem.Enabled = _operationController.CanEdit() && !_disabledAll;
         }

         if (_mergeItem != null)
         {
            _mergeItem.Enabled = _operationController.CanMerge() && !_disabledAll;
         }

         if (_muteUntilTomorrowItem != null)
         {
            _muteUntilTomorrowItem.Enabled = !_disabledAll;
         }

         if (_muteUntilMondayItem != null)
         {
            _muteUntilMondayItem.Enabled = !_disabledAll;
         }

         if (_unmuteItem != null)
         {
            _unmuteItem.Enabled = _isUnmuteActionEnabled && !_disabledAll;
         }

         if (_excludeItem != null)
         {
            _excludeItem.Text = _canBeExcluded ? "Hi&de" : "Unhi&de";
         }

         if (_pinItem != null)
         {
            _pinItem.Text = _pinItemText;
         }

         if (_openAuthorProfileItem != null)
         {
            _openAuthorProfileItem.Enabled = !_disabledAll;
         }
      }

      private readonly IOperationController _operationController;

      private readonly ToolStripMenuItem _refreshListItem;
      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private ToolStripItem _defaultItem;
      private readonly ToolStripItem _muteUntilTomorrowItem;
      private readonly ToolStripItem _muteUntilMondayItem;
      private readonly ToolStripItem _unmuteItem;
      private readonly ToolStripItem _excludeItem;
      private readonly ToolStripItem _pinItem;
      private readonly ToolStripMenuItem _diffToolItem;
      private readonly ToolStripMenuItem _diffToBaseItem;
      private readonly ToolStripMenuItem _discussionsItem;
      private readonly ToolStripMenuItem _openAuthorProfileItem;
      private bool _disabledAll;
      private bool _isUnmuteActionEnabled;
      private bool _canBeExcluded;
      private string _pinItemText;
   }
}

