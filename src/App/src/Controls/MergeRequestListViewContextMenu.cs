using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class MergeRequestListViewContextMenu : ContextMenuStrip
   {
      public MergeRequestListViewContextMenu(EventHandler onRefresh, EventHandler onEdit,
         EventHandler onMerge, EventHandler onClose)
      {
         InitializeComponent();

         ToolStripItemCollection items = Items;
         items.Add("&Refresh selected", null, onRefresh);
         items.Add("-", null, null);
         _editItem = items.Add("&Edit...", null, onEdit);
         _mergeItem = items.Add("&Merge...", null, onMerge);
         items.Add("&Close", null, onClose);
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

