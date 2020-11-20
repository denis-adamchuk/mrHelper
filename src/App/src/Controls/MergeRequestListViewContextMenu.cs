using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(Action onDiscussions, Action onRefresh, Action onEdit,
         Action onMerge, Action onClose, Action onDefault)
      {
         InitializeComponent();

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
         if (_editItem != null)
         {
            _editItem.Enabled = enabled;
         }
      }

      public void SetMergeActionEnabled(bool enabled)
      {
         if (_mergeItem != null)
         {
            _mergeItem.Enabled = enabled;
         }
      }

      public void LaunchDefaultAction()
      {
         _defaultItem?.PerformClick();
      }

      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
      private readonly ToolStripItem _defaultItem;
   }
}

