using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(Action onRefresh, Action onEdit,
         Action onMerge, Action onClose)
      {
         InitializeComponent();

         ToolStripItemCollection items = Items;
         if (onRefresh != null)
         {
            items.Add("&Refresh selected", null, (s, e) => onRefresh());
         }
         if (onEdit != null || onMerge != null || onClose != null)
         {
            items.Add("-", null, null);
            if (onEdit != null)
            {
               _editItem = items.Add("&Edit...", null, (s, e) => onEdit());
            }
            if (onMerge != null)
            {
               _mergeItem = items.Add("&Merge...", null, (s, e) => onMerge());
            }
            if (onClose != null)
            {
               items.Add("&Close", null, (s, e) => onClose());
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

      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
   }
}

