using System;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using mrHelper.App.Helpers;

namespace mrHelper.App.src.Controls
{
   public partial class VersionBrowser : UserControl
   {
      public VersionBrowser()
      {
         InitializeComponent();
         _treeView.Model = new VersionBrowserModel();
         _treeView.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.MultiSameParent;
         _treeView.SelectionChanged += (s, e) => SelectionChanged?.Invoke(s, e);
         _treeView.Model.StructureChanged += (s, e) => onStructureChanged(s, e);

         // TODO
         // Tooltips
         // AutoSelect not reviewed
         // Collect reviewed
         // Owner Draw
      }

      public string[] GetSelected()
      {
         // Nothing selected
         if (_treeView.SelectedNodes.Count == 0)
         {
            return Array.Empty<string>();
         }

         // At least one of selected nodes is a root node
         if (_treeView.SelectedNodes
               .Select(x => _treeView.GetPath(x).LastNode is RootVersionBrowserItem)
               .Where(x => x)
               .Any())
         {
            return Array.Empty<string>();
         }

         // Gather SHA from selected nodes
         return _treeView.SelectedNodes
            .Select(x => _treeView.GetPath(x).LastNode as BaseVersionBrowserItem)
            .Select(x => x.FullSHA)
            .ToArray();
      }

      public string GetBaseCommitSha()
      {
         return (_treeView.Model as VersionBrowserModel).Data.BaseSha;
      }

      public void SetData(VersionBrowserModelData data)
      {
         (_treeView.Model as VersionBrowserModel).Data = data;
      }

      public event EventHandler SelectionChanged;

      private void onStructureChanged(object s, TreePathEventArgs e)
      {
         // TODO Temporary code
         TreeNodeAdv xx = _treeView.Root.Children
            .SingleOrDefault(x => (_treeView.GetPath(x).LastNode is RootVersionBrowserItem root) && root.Name == "Versions");
         xx?.Expand();
      }
   }
}

