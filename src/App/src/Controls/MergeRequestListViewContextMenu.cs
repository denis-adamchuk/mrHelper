using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(
         Action onDiscussions,
         Action onRefreshList,
         Action onRefresh,
         Action onEdit,
         Action onMerge,
         Action onClose,
         Action onDiffToBase,
         Action onDiffDefault,
         Action onDefault)
      {
         ToolStripItemCollection items = Items;
         if (onDiscussions != null)
         {
            var item = new ToolStripMenuItem("&Discussions", null, (s, e) => onDiscussions())
            {
               ShortcutKeys = Keys.F2
            };
            items.Add(item);
            if (onDiscussions == onDefault)
            {
               _defaultItem = item;
            }
         }
         if (onDiffToBase != null || onDiffDefault != null)
         {
            items.Add("-", null, null);
            if (onDiffDefault != null)
            {
               var item = new ToolStripMenuItem("Diff &Tool", null, (s, e) => onDiffDefault())
               {
                  ShortcutKeys = Keys.F3
               };
               items.Add(item);
               if (onDiffDefault == onDefault)
               {
                  _defaultItem = item;
               }
            }
            if (onDiffToBase != null)
            {
               var item = new ToolStripMenuItem("Diff to &Base", null, (s, e) => onDiffToBase())
               {
                  ShortcutKeys = Keys.Shift | Keys.F3
               };
               items.Add(item);
               if (onDiffToBase == onDefault)
               {
                  _defaultItem = item;
               }
            }
         }
         if (onRefreshList != null || onRefresh != null)
         {
            items.Add("-", null, null);
            if (onRefreshList != null)
            {
               _refreshListItem = new ToolStripMenuItem("&Refresh list", null, (s, e) => onRefreshList())
               {
                  ShortcutKeys = Keys.F5
               };
               items.Add(_refreshListItem);
               if (onRefreshList == onDefault)
               {
                  _defaultItem = _refreshListItem;
               }
            }
            if (onRefresh != null)
            {
               var item = new ToolStripMenuItem("R&efresh selected", null, (s, e) => onRefresh())
               {
                  ShortcutKeys = Keys.Shift | Keys.F5
               };
               items.Add(item);
               if (onRefresh == onDefault)
               {
                  _defaultItem = item;
               }
            }
         }
         if (onEdit != null || onMerge != null || onClose != null)
         {
            items.Add("-", null, null);
            if (onEdit != null)
            {
               _editItem = new ToolStripMenuItem("Ed&it...", null, (s, e) => onEdit());
               items.Add(_editItem);
               if (onEdit == onDefault)
               {
                  _defaultItem = _editItem;
               }
            }
            if (onMerge != null)
            {
               _mergeItem = new ToolStripMenuItem("&Merge...", null, (s, e) => onMerge());
               items.Add(_mergeItem);
               if (onEdit == onDefault)
               {
                  _defaultItem = _mergeItem;
               }
            }
            if (onClose != null)
            {
               var item = new ToolStripMenuItem("&Close", null, (s, e) => onClose());
               items.Add(item);
               if (onClose == onDefault)
               {
                  _defaultItem = item;
               }
            }
         }
      }

      public void SetEditActionEnabled(bool enabled)
      {
         _isEditActionEnabled = enabled;
      }

      public void SetMergeActionEnabled(bool enabled)
      {
         _isMergeActionEnabled = enabled;
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
      }

      private readonly ToolStripMenuItem _refreshListItem;
      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private readonly ToolStripItem _defaultItem;
      private bool _isEditActionEnabled;
      private bool _isMergeActionEnabled;
      private bool _disabledAll;
   }
}

