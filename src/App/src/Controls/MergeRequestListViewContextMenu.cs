using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(Action onDiscussions, Action onRefresh, Action onEdit,
         Action onMerge, Action onClose, Action onDefault)
      {
         ToolStripItemCollection items = Items;
         if (onDiscussions != null)
         {
            var item = items.Add("&Discussions", null, (s, e) => onDiscussions());
            if (onDiscussions == onDefault)
            {
               _defaultItem = item;
            }
         }
         if (onRefresh != null)
         {
            items.Add("-", null, null);
            var item = items.Add("&Refresh selected", null, (s, e) => onRefresh());
            if (onRefresh == onDefault)
            {
               _defaultItem = item;
            }
         }
         if (onEdit != null || onMerge != null || onClose != null)
         {
            items.Add("-", null, null);
            if (onEdit != null)
            {
               _editItem = items.Add("&Edit...", null, (s, e) => onEdit());
               if (onEdit == onDefault)
               {
                  _defaultItem = _editItem;
               }
            }
            if (onMerge != null)
            {
               _mergeItem = items.Add("&Merge...", null, (s, e) => onMerge());
               if (onEdit == onDefault)
               {
                  _defaultItem = _mergeItem;
               }
            }
            if (onClose != null)
            {
               var item = items.Add("&Close", null, (s, e) => onClose());
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

         if (_editItem != null)
         {
            _editItem.Enabled = _isEditActionEnabled && !_disabledAll;
         }

         if (_mergeItem != null)
         {
            _mergeItem.Enabled = _isMergeActionEnabled && !_disabledAll;
         }
      }

      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private readonly ToolStripItem _defaultItem;
      private bool _isEditActionEnabled;
      private bool _isMergeActionEnabled;
      private bool _disabledAll;
   }
}

