using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(
         Action onDiscussions, Action onRefreshList, Action onRefresh, Action onEdit,
         Action onMerge, Action onClose, Action onDiffToBase, Action onDiffDefault,
         Action onMuteUntilTomorrow, Action onMuteUntilMonday, Action onUnmute,
         Action onDefault)
      {
         addItem(onDiscussions, "&Discussions", Keys.F2, onDiscussions == onDefault);

         if (onDiffToBase != null || onDiffDefault != null)
         {
            addSeparator();
         }

         addItem(onDiffDefault, "Diff &Tool", Keys.F3, onDiffDefault == onDefault);
         addItem(onDiffToBase, "Diff to &Base", Keys.Shift | Keys.F3, onDiffToBase == onDefault);

         if (onRefreshList != null || onRefresh != null)
         {
            addSeparator();
         }

         _refreshListItem = addItem(onRefreshList, "&Refresh list", Keys.F5, onRefreshList == onDefault);
         addItem(onRefresh, "R&efresh selected", Keys.Shift | Keys.F5, onRefresh == onDefault);

         if (onEdit != null || onMerge != null || onClose != null)
         {
            addSeparator();
         }

         _editItem = addItem(onEdit, "Ed&it...", Keys.None, onEdit == onDefault);
         _mergeItem = addItem(onMerge, "&Merge...", Keys.None, onMerge == onDefault);
         addItem(onClose, "&Close", Keys.None, onClose == onDefault);

         if (onMuteUntilTomorrow != null || onMuteUntilMonday != null || onUnmute != null)
         {
            addSeparator();
         }

         _muteUntilTomorrowItem = addItem(onMuteUntilTomorrow,
            "Don't &highlight until tomorrow", Keys.None, onMuteUntilTomorrow == onDefault);
         _muteUntilMondayItem = addItem(onMuteUntilMonday,
            "Don't &highlight until Monday", Keys.None, onMuteUntilMonday == onDefault);
         _unmuteItem = addItem(onUnmute, "Restore high&light", Keys.None, onUnmute == onDefault);
      }

      private ToolStripMenuItem addItem(Action action, string name, Keys shortcutKeys, bool isDefault)
      {
         if (action == null)
         {
            return null;
         }

         ToolStripMenuItem item = new ToolStripMenuItem(name, null, (s, e) => action())
         {
            ShortcutKeys = shortcutKeys
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

      public void SetEditActionEnabled(bool enabled)
      {
         _isEditActionEnabled = enabled;
      }

      public void SetMergeActionEnabled(bool enabled)
      {
         _isMergeActionEnabled = enabled;
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

         if (_refreshListItem != null)
         {
            _refreshListItem.Enabled = true;
         }

         if (_editItem != null)
         {
            _editItem.Enabled = _isEditActionEnabled && !_disabledAll;
         }

         if (_mergeItem != null)
         {
            _mergeItem.Enabled = _isMergeActionEnabled && !_disabledAll;
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
            _unmuteItem.Enabled = _isUnmuteActionEnabled;
         }
      }

      private readonly ToolStripMenuItem _refreshListItem;
      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private ToolStripItem _defaultItem;
      private readonly ToolStripItem _muteUntilTomorrowItem;
      private readonly ToolStripItem _muteUntilMondayItem;
      private readonly ToolStripItem _unmuteItem;
      private bool _isEditActionEnabled;
      private bool _isMergeActionEnabled;
      private bool _disabledAll;
      private bool _isUnmuteActionEnabled;
   }
}

