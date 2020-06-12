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

      internal RevisionBrowser()
      {
         InitializeComponent();
         _treeView.Model = new RevisionBrowserModel();
         _treeView.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.MultiSameParent;
         _treeView.SelectionChanged += (s, e) => SelectionChanged?.Invoke(s, e);

         _name.ToolTipProvider = new TooltipProvider();
         _name.DrawText += row_DrawText;
         _timestamp.DrawText += row_DrawText;
      }

      internal string[] GetSelected(out RevisionType? type)
      {
         type = new Nullable<RevisionType>();
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out type);
         return leaves.Any() ? leaves.Select(x => x.FullSHA).ToArray() : Array.Empty<string>();
      }

      internal string GetBaseCommitSha()
      {
         return getModel().Data.BaseSha;
      }

      internal IEnumerable<string> GetIncludedSha()
      {
         IEnumerable<RevisionBrowserItem> leaves = getSelectedLeafNodes(out RevisionType? type);
         if (!leaves.Any())
         {
            return Array.Empty<string>();
         }

         RevisionBrowserItem latestSelected = leaves.OrderByDescending(x => x.OriginalTimestamp).First();
         return _treeView.AllNodes
            .Where(x => x.Tag is RevisionBrowserItem)
            .Select(x => x.Tag as RevisionBrowserItem)
            .OrderByDescending(x => x.OriginalTimestamp)
            .Where(x => x.OriginalTimestamp <= latestSelected.OriginalTimestamp)
            .Select(x => x.FullSHA);
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
         type = new Nullable<RevisionType>();

         // Nothing selected
         if (_treeView.SelectedNodes.Count == 0)
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         // At least one of selected nodes is a root node
         if (_treeView.SelectedNodes
               .Select(x => x.Tag is RevisionBrowserTypeItem)
               .Where(x => x)
               .Any())
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         if (_treeView.SelectedNodes
               .Any(x => (x.Tag as RevisionBrowserItem).FullSHA == "N/A"))
         {
            return Array.Empty<RevisionBrowserItem>();
         }

         Debug.Assert(_treeView.SelectedNodes.Any());
         Debug.Assert(!_treeView.SelectedNodes.Any(x => !(x.Tag is RevisionBrowserItem)));
         Debug.Assert(_treeView.SelectedNodes.Select(x => x.Parent).Distinct().Count() == 1);
         type = (_treeView.SelectedNodes[0].Parent.Tag as RevisionBrowserTypeItem).Type;
         return _treeView.SelectedNodes.Select(x => x.Tag as RevisionBrowserItem);
      }

      private void autoSelectRevision(TreeNodeAdv revisionTypeNode)
      {
         _treeView.ClearSelection();

         IEnumerable<TreeNodeAdv> sortedChildren = revisionTypeNode?.Children
            .Where(x => x.Tag is RevisionBrowserItem)
            .OrderByDescending(x => (x.Tag as RevisionBrowserItem).OriginalTimestamp);
         if (sortedChildren == null || !sortedChildren.Any())
         {
            return;
         }

         IEnumerable<string> reviewedRevisions = getModel().Data.ReviewedRevisions;
         TreeNodeAdv newestReviewedRevision = sortedChildren
            .FirstOrDefault(x => reviewedRevisions.Contains((x.Tag as RevisionBrowserItem).FullSHA));
         if (newestReviewedRevision == null)
         {
            sortedChildren.First().IsSelected = true;
         }
         else if (Program.Settings.AutoSelectNewestCommit)
         {
            newestReviewedRevision.IsSelected = true;
            if (newestReviewedRevision != sortedChildren.First())
            {
               sortedChildren.First().IsSelected = true;
            }
         }
         else
         {
            newestReviewedRevision.IsSelected = true;
            if (newestReviewedRevision.PreviousNode != null)
            {
               newestReviewedRevision.PreviousNode.IsSelected = true;
            }
         }
      }

      private RevisionBrowserModel getModel()
      {
         return _treeView.Model as RevisionBrowserModel;
      }

      private void row_DrawText(object sender, DrawEventArgs e)
      {
         if (e.Node.Tag is RevisionBrowserItem leafNode && leafNode.IsReviewed)
         {
            e.TextColor = Color.LightGray;
         }
      }

      private TreeNodeAdv getRevisionTypeNode(RevisionType type)
      {
         return _treeView.Root.Children
            .SingleOrDefault(x => x.Tag is RevisionBrowserTypeItem root && root.Type == type);
      }
   }
}

