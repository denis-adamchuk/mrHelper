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
      private class TooltipProvider : IToolTipProvider
      {
         public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
         {
            return (node.Tag is RevisionBrowserItem leafNode) ? leafNode.TooltipText : String.Empty;
         }
      }

      bool _initializing;
      internal RevisionBrowser()
      {
         _initializing = true;
         InitializeComponent();
         _initializing = false;

         _treeView.Model = new RevisionBrowserModel();
         _treeView.SelectionChanged += (s, e) => SelectionChanged?.Invoke(s, e);
         _treeView.RowDraw += treeView_DrawRow;

         _name.ToolTipProvider = new TooltipProvider();
         _name.DrawText += treeView_DrawNode;
         _timestamp.DrawText += treeView_DrawNode;
      }

      internal string GetBaseCommitSha()
      {
         return getModel().Data.BaseSha;
      }

      internal string[] GetSelectedSha(out RevisionType? type)
      {
         type = new RevisionType?();
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out type);
         return leaves.OrderBy(x => x.OriginalTimestamp).Select(x => x.FullSHA).ToArray();
      }

      internal string[] GetIncludedSha()
      {
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out RevisionType? type);
         RevisionBrowserItem latestSelected = leaves.OrderByDescending(x => x.OriginalTimestamp).FirstOrDefault();
         return getEarlierLeafNodes(latestSelected).Select(x => x.FullSHA).ToArray();
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
         RevisionBrowserModelData newData = new RevisionBrowserModelData(
            model.Data.BaseSha,
            model.Data.Revisions[RevisionType.Commit].Cast<Commit>(),
            model.Data.Revisions[RevisionType.Version].Cast<GitLabSharp.Entities.Version>(),
            revisions);
         SetData(newData, affectedType);
      }

      public event EventHandler SelectionChanged;

      protected override void OnFontChanged(EventArgs eventArgs)
      {
         base.OnFontChanged(eventArgs);

         _treeView.Font = this.Font;
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

      private IEnumerable<RevisionBrowserItem> getEarlierLeafNodes(RevisionBrowserItem item)
      {
         if (item == null)
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         return _treeView.AllNodes
            .Where(x => x.Tag is RevisionBrowserItem)
            .Select(x => x.Tag)
            .Cast<RevisionBrowserItem>()
            .OrderByDescending(x => x.OriginalTimestamp)
            .Where(x => x.OriginalTimestamp <= item.OriginalTimestamp);
      }

      private void autoSelectRevision(TreeNodeAdv revisionTypeNode)
      {
         _treeView.ClearSelection();
         revisionTypeNode?.Expand();

         IEnumerable<TreeNodeAdv> sortedChildren = revisionTypeNode?.Children
            .Where(x => x.Tag is RevisionBrowserItem)
            .OrderByDescending(x => (x.Tag as RevisionBrowserItem).OriginalTimestamp);
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
         else if (Program.Settings.AutoSelectNewestRevision)
         {
            newestReviewedRevisionNode.IsSelected = true;
            if (newestReviewedRevisionNode != newestRevisionNode)
            {
               newestRevisionNode.IsSelected = true;
            }
         }
         else
         {
            newestReviewedRevisionNode.IsSelected = true;
            TreeNodeAdv earliestNotReviewedRevisionNode = newestReviewedRevisionNode.PreviousNode;
            if (earliestNotReviewedRevisionNode != null)
            {
               earliestNotReviewedRevisionNode.IsSelected = true;
            }
         }
      }

      private RevisionBrowserModel getModel()
      {
         return _treeView.Model as RevisionBrowserModel;
      }

      private void treeView_DrawRow(object sender, TreeViewRowDrawEventArgs e)
      {
         if (e.Node.IsSelected)
         {
            Rectangle focusRect = new Rectangle(
               _treeView.OffsetX, e.RowRect.Y, _treeView.ClientRectangle.Width, e.RowRect.Height);
            e.Graphics.FillRectangle(SystemBrushes.Highlight, focusRect);
         }
      }

      private void treeView_DrawNode(object sender, DrawEventArgs e)
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

      private void treeView_ColumnWidthChanged(object sender, TreeColumnEventArgs e)
      {
         if (!_initializing)
         {
            saveColumnWidths(x => Program.Settings.RevisionBrowserColumnWidths = x);
         }
      }

      private void RevisionBrowser_Load(object sender, EventArgs e)
      {
         loadColumnWidths(Program.Settings.RevisionBrowserColumnWidths);
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

      private void loadColumnWidths(Dictionary<string, int> storedWidths)
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

      private TreeNodeAdv getRevisionTypeNode(RevisionType type)
      {
         return _treeView.Root.Children
            .SingleOrDefault(x => x.Tag is RevisionBrowserTypeItem root && root.Type == type);
      }
   }
}

