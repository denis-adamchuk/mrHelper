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
         items.Add("&Refresh selected", null, (s, e) => onRefresh());
         items.Add("-", null, null);
         _editItem = items.Add("&Edit...", null, (s, e) => onEdit());
         _mergeItem = items.Add("&Merge...", null, (s, e) => onMerge());
         items.Add("&Close", null, (s, e) => onClose());
      }

      public void SetEditActionEnabled(bool enabled)
      {
         _editItem.Enabled = enabled;
      }

      public void SetMergeActionEnabled(bool enabled)
      {
         _mergeItem.Enabled = enabled;
      }

      private readonly ToolStripItem _editItem;
      private readonly ToolStripItem _mergeItem;
   }
}

