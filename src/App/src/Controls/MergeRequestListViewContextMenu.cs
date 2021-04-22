using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public interface IOperationController
   {
      bool CanDiscussions();
      bool CanDiffTool();
      bool CanEdit();
      bool CanMerge();
   }

   public class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(
         IOperationController operationController,
         Action onDiscussions, Action onRefreshList, Action onRefresh, Action onEdit,
         Action onMerge, Action onClose, Action onDiffToBase, Action onDiffDefault,
         Action onMuteUntilTomorrow, Action onMuteUntilMonday, Action onUnmute,
         Action onDefault)
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

         _operationController = operationController;
      }

      private ToolStripMenuItem addItem(Action action, string name, bool isDefault)
      {
         if (action == null)
         {
            return null;
         }

         ToolStripMenuItem item = new ToolStripMenuItem(name, null, (s, e) => action())
         {
            //ShortcutKeys = shortcutKeys
         };
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
            _diffToolItem.Enabled = _operationController.CanDiffTool() && !_disabledAll;
         }

         if (_diffToBaseItem != null)
         {
            _diffToBaseItem.Enabled = _operationController.CanDiffTool() && !_disabledAll;
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
      }

      private readonly IOperationController _operationController;

      private readonly ToolStripMenuItem _refreshListItem;
      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private ToolStripItem _defaultItem;
      private readonly ToolStripItem _muteUntilTomorrowItem;
      private readonly ToolStripItem _muteUntilMondayItem;
      private readonly ToolStripItem _unmuteItem;
      private readonly ToolStripMenuItem _diffToolItem;
      private readonly ToolStripMenuItem _diffToBaseItem;
      private readonly ToolStripMenuItem _discussionsItem;
      private bool _disabledAll;
      private bool _isUnmuteActionEnabled;
   }
}

