using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class RevisionBrowserContextMenu : ContextMenuStrip
   {
      public RevisionBrowserContextMenu(
         IOperationController operationController,
         Action onDiffBetweenSelected, Action onDiffSelectedToBase,
         Action onDiffSelectedToParent, Action onDiffLatestToBase,
         Action onDefault)
      {
         if (onDiffBetweenSelected != null)
         {
            _onDiffBetweenSelected = addItem(onDiffBetweenSelected,
               "Diff between selected", onDiffBetweenSelected == onDefault);
         }

         if (onDiffSelectedToBase != null || onDiffSelectedToParent != null || onDiffLatestToBase != null)
         {
            addSeparator();

            if (onDiffSelectedToBase != null)
            {
               _onDiffSelectedToBase = addItem(onDiffSelectedToBase,
                  "Diff selected to Base", onDiffSelectedToBase == onDefault);
            }

            if (onDiffSelectedToParent != null)
            {
               _onDiffSelectedToParent = addItem(onDiffSelectedToParent,
                  "Diff selected to parent", onDiffSelectedToParent == onDefault);
            }

            if (onDiffLatestToBase != null)
            {
               addSeparator();

               _onDiffLatestToBase = addItem(onDiffLatestToBase,
                  "Diff Head to Base", onDiffLatestToBase == onDefault);
            }
         }

         _operationController = operationController;
      }

      public void LaunchDefaultAction()
      {
         _defaultItem?.PerformClick();
      }

      public void UpdateItemState()
      {
         if (_onDiffBetweenSelected != null)
         {
            _onDiffBetweenSelected.Enabled = _operationController.CanDiffTool(DiffToolMode.DiffBetweenSelected);
         }

         if (_onDiffSelectedToBase != null)
         {
            _onDiffSelectedToBase.Enabled = _operationController.CanDiffTool(DiffToolMode.DiffSelectedToBase);
         }

         if (_onDiffSelectedToParent != null)
         {
            _onDiffSelectedToParent.Enabled = _operationController.CanDiffTool(DiffToolMode.DiffSelectedToParent);
         }

         if (_onDiffLatestToBase != null)
         {
            _onDiffLatestToBase.Enabled = _operationController.CanDiffTool(DiffToolMode.DiffLatestToBase);
         }
      }

      private void addSeparator()
      {
         Items.Add("-", null, null);
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

      private readonly IOperationController _operationController;

      private readonly ToolStripMenuItem _onDiffBetweenSelected;
      private readonly ToolStripMenuItem _onDiffSelectedToBase;
      private readonly ToolStripMenuItem _onDiffSelectedToParent;
      private readonly ToolStripMenuItem _onDiffLatestToBase;
      private ToolStripItem _defaultItem;
   }
}

