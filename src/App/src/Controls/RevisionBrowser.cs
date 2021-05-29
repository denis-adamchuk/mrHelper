using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   internal partial class RevisionBrowser : UserControl
   {
      private class NameTooltipProvider : IToolTipProvider
      {
         public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
         {
            return (node.Tag is RevisionBrowserItem leafNode) ? leafNode.Description : String.Empty;
         }
      }

      private class TimeStampTooltipProvider : IToolTipProvider
      {
         public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
         {
            return (node.Tag is RevisionBrowserItem leafNode) ? leafNode.Timestamp : String.Empty;
         }
      }

      readonly bool _initializing;
      public RevisionBrowser()
      {
         _initializing = true;
         InitializeComponent();
         _initializing = false;

         _treeView.Model = new RevisionBrowserModel();
         _treeView.SelectionChanged += onTreeViewSelectionChanged;
         _treeView.NodeMouseDoubleClick += onTreeViewNodeMouseDoubleClick;
         _treeView.RowDraw += onTreeViewDrawRow;

         _name.ToolTipProvider = new NameTooltipProvider();
         _name.DrawText += onTreeViewDrawNode;

         _timestamp.ToolTipProvider = new TimeStampTooltipProvider();
         _timestamp.DrawText += onTreeViewDrawNode;
      }

      internal void AssignContextMenu(RevisionBrowserContextMenu contextMenu)
      {
         if (ContextMenuStrip != null)
         {
            ContextMenuStrip.Opening -= onContextMenuStripOpening;
            ContextMenuStrip.Dispose();
         }

         ContextMenuStrip = contextMenu;

         if (ContextMenuStrip != null)
         {
            contextMenu.Opening += onContextMenuStripOpening;
         }
      }

      internal string GetBaseCommitSha()
      {
         return getModel().Data.BaseSha;
      }

      internal string GetHeadSha(RevisionType revisionType)
      {
         TreeNodeAdv revisionTypeNode = getRevisionTypeNode(revisionType);
         IEnumerable<RevisionBrowserItem> sortedChildren = getSortedChildrenCasted(revisionTypeNode);
         return sortedChildren?.FirstOrDefault()?.FullSHA;
      }

      internal string[] GetIncludedSha(RevisionType revisionType)
      {
         TreeNodeAdv revisionTypeNode = getRevisionTypeNode(revisionType);
         IEnumerable<RevisionBrowserItem> sortedChildren = getSortedChildrenCasted(revisionTypeNode);
         return sortedChildren?.Select(item => item.FullSHA).ToArray();
      }

      internal string[] GetSelectedSha(out RevisionType? revisionType)
      {
         revisionType = new RevisionType?();
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out revisionType);
         return leaves.OrderBy(x => x.InvertedDisplayIndex).Select(x => x.FullSHA).ToArray();
      }

      internal string[] GetIncludedBySelectedSha()
      {
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out RevisionType? revisionType);
         RevisionBrowserItem latestSelected = leaves.OrderByDescending(x => x.InvertedDisplayIndex).FirstOrDefault();
         return getEarlierLeafNodes(latestSelected, revisionType).Select(x => x.FullSHA).ToArray();
      }

      internal string GetParentShaForSelected()
      {
         return GetIncludedBySelectedSha()?.Skip(1)?.FirstOrDefault();
      }

      internal void SetData(RevisionBrowserModelData data, RevisionType defaultRevisionType)
      {
         // We want to re-expand all previously expanded types if they are collapsed by upgrading the Model
         Dictionary<RevisionType, bool> oldExpandedState = new Dictionary<RevisionType, bool>();
         foreach (RevisionType type in new RevisionType[] { RevisionType.Commit, RevisionType.Version })
         {
            oldExpandedState[type] = getRevisionTypeNode(type).IsExpanded;
         }

         getModel().Data = data;
         autoSelectRevision(getRevisionTypeNode(defaultRevisionType));

         foreach (KeyValuePair<RevisionType, bool> kv in oldExpandedState.Where(x => x.Value))
         {
            getRevisionTypeNode(kv.Key).Expand();
         }

         if (_treeView.SelectedNode != null)
         {
            _treeView.EnsureVisible(_treeView.SelectedNode);
         }
      }

      internal void ClearData(RevisionType defaultRevisionType)
      {
         SetData(new RevisionBrowserModelData(), defaultRevisionType);
      }

      internal void UpdateReviewedRevisions(HashSet<string> revisions, RevisionType affectedType)
      {
         RevisionBrowserModel model = getModel();
         if (model.Data != null && model.Data.BaseSha != null && model.Data.Revisions != null)
         {
            RevisionBrowserModelData newData = new RevisionBrowserModelData(
               model.Data.BaseSha,
               model.Data.Revisions[RevisionType.Commit].Cast<Commit>(),
               model.Data.Revisions[RevisionType.Version].Cast<GitLabSharp.Entities.Version>(),
               revisions);
            SetData(newData, affectedType);
         }
      }

      public event EventHandler SelectionChanged;

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         if (Program.Settings != null)
         {
            loadColumnWidths(Program.Settings.RevisionBrowserColumnWidths);
         }
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         base.OnVisibleChanged(e);
         if (Visible && Program.Settings != null)
         {
            loadColumnWidths(Program.Settings.RevisionBrowserColumnWidths);
         }
      }

      protected override void OnFontChanged(EventArgs eventArgs)
      {
         base.OnFontChanged(eventArgs);
         _treeView.Font = this.Font;
      }

      private void onTreeViewDrawRow(object sender, TreeViewRowDrawEventArgs e)
      {
         if (e.Node.IsSelected)
         {
            Rectangle focusRect = new Rectangle(
               _treeView.OffsetX, e.RowRect.Y, _treeView.ClientRectangle.Width, e.RowRect.Height);
            e.Graphics.FillRectangle(SystemBrushes.Highlight, focusRect);
         }
      }

      private void onTreeViewDrawNode(object sender, DrawEventArgs e)
      {
         e.BackgroundBrush = null;
         if (e.Node.Tag is RevisionBrowserItem leafNode && leafNode.IsReviewed)
         {
            e.TextColor = Color.LightGray;
         }
         else if (e.Node.IsSelected)
         {
            e.TextColor = SystemColors.HighlightText;
         }
         else
         {
            e.TextColor = SystemColors.ControlText;
         }
      }

      private void onTreeViewColumnWidthChanged(object sender, TreeColumnEventArgs e)
      {
         if (!_loadingColumnWidth && !_initializing)
         {
            saveColumnWidths(x => Program.Settings.RevisionBrowserColumnWidths = x);
         }
      }

      private void onTreeViewSelectionChanged(object sender, EventArgs e)
      {
         SelectionChanged?.Invoke(sender, e);
      }

      private void onTreeViewNodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
      {
         SelectionChanged?.Invoke(sender, e);

         RevisionBrowserContextMenu contextMenu = (RevisionBrowserContextMenu)ContextMenuStrip;
         contextMenu?.LaunchDefaultAction();
      }

      private void onContextMenuStripOpening(object sender, System.ComponentModel.CancelEventArgs e)
      {
         RevisionBrowserContextMenu contextMenu = (RevisionBrowserContextMenu)sender;
         contextMenu.UpdateItemState();
      }

      private IEnumerable<RevisionBrowserItem> getSelectedLeafNodes(out RevisionType? type)
      {
         type = new RevisionType?();

         int totalSelectedCount = _treeView.SelectedNodes.Count;
         int selectedLeafCount = _treeView.SelectedNodes
            .Count(x => x.Tag is RevisionBrowserItem item && item.FullSHA != "N/A");
         if (totalSelectedCount == 0 || totalSelectedCount != selectedLeafCount)
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         Debug.Assert(_treeView.SelectedNodes.Select(x => x.Parent).Distinct().Count() == 1);
         type = (_treeView.SelectedNodes.First().Parent.Tag as RevisionBrowserTypeItem).Type;
         return _treeView.SelectedNodes.Select(x => x.Tag).Cast<RevisionBrowserItem>();
      }

      private IEnumerable<RevisionBrowserItem> getEarlierLeafNodes(
         RevisionBrowserItem item, RevisionType? revisionType)
      {
         if (item == null || !revisionType.HasValue)
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         TreeNodeAdv revisionTypeNode = getRevisionTypeNode(revisionType.Value);
         return getSortedChildrenCasted(revisionTypeNode)
            .Where(x => x.InvertedDisplayIndex <= item.InvertedDisplayIndex);
      }

      private IEnumerable<TreeNodeAdv> getSortedChildren(TreeNodeAdv revisionTypeNode)
      {
         return revisionTypeNode?.Children
            .Where(x => x.Tag is RevisionBrowserItem)
            .OrderByDescending(x => (x.Tag as RevisionBrowserItem).InvertedDisplayIndex);
      }

      private IEnumerable<RevisionBrowserItem> getSortedChildrenCasted(TreeNodeAdv revisionTypeNode)
      {
         return revisionTypeNode?.Children
            .Where(x => x.Tag is RevisionBrowserItem)
            .Select(x => x.Tag)
            .Cast<RevisionBrowserItem>()
            .OrderByDescending(x => x.InvertedDisplayIndex);
      }

      private void autoSelectRevision(TreeNodeAdv revisionTypeNode)
      {
         _treeView.ClearSelection();
         revisionTypeNode?.Expand();

         IEnumerable<TreeNodeAdv> sortedChildren = getSortedChildren(revisionTypeNode);
         if (sortedChildren == null || !sortedChildren.Any())
         {
            return;
         }

         IEnumerable<string> reviewedRevisions = getModel().Data.ReviewedRevisions;
         TreeNodeAdv newestRevisionNode = sortedChildren.First();
         TreeNodeAdv newestReviewedRevisionNode = sortedChildren
            .FirstOrDefault(x => reviewedRevisions.Contains((x.Tag as RevisionBrowserItem).FullSHA));
         if (newestReviewedRevisionNode == null)
         {
            newestRevisionNode.IsSelected = true;
         }
         else
         {
            var mode = ConfigurationHelper.GetRevisionAutoSelectionMode(Program.Settings);
            switch (mode)
            {
               case ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest:
                  newestReviewedRevisionNode.IsSelected = true;
                  if (newestReviewedRevisionNode != newestRevisionNode)
                  {
                     newestRevisionNode.IsSelected = true;
                  }
                  break;

               case ConfigurationHelper.RevisionAutoSelectionMode.LastVsNext:
                  newestReviewedRevisionNode.IsSelected = true;
                  TreeNodeAdv earliestNotReviewedRevisionNode = newestReviewedRevisionNode.PreviousNode;
                  if (earliestNotReviewedRevisionNode != null)
                  {
                     earliestNotReviewedRevisionNode.IsSelected = true;
                  }
                  break;

               case ConfigurationHelper.RevisionAutoSelectionMode.BaseVsLatest:
                  newestReviewedRevisionNode.IsSelected = true;
                  break;
            }
         }
      }

      private RevisionBrowserModel getModel()
      {
         return _treeView.Model as RevisionBrowserModel;
      }

      private void saveColumnWidths(Action<Dictionary<string, int>> saveProperty)
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (TreeColumn column in _treeView.Columns)
         {
            columnWidths[(string)column.Header] = column.Width;
         }
         saveProperty(columnWidths);
      }

      private bool _loadingColumnWidth = false;
      private void loadColumnWidths(Dictionary<string, int> storedWidths)
      {
         _loadingColumnWidth = true;
         try
         {
            foreach (TreeColumn column in _treeView.Columns)
            {
               string columnName = (string)column.Header;
               if (storedWidths.ContainsKey(columnName))
               {
                  column.Width = storedWidths[columnName];
               }
            }
         }
         finally
         {
            _loadingColumnWidth = false;
         }
      }

      private TreeNodeAdv getRevisionTypeNode(RevisionType type)
      {
         return _treeView.Root.Children
            .SingleOrDefault(x => x.Tag is RevisionBrowserTypeItem root && root.Type == type);
      }
   }
}

