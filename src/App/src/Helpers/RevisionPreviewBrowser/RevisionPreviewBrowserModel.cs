#pragma warning disable 67  // Event never used

using System;
using System.Linq;
using System.Collections.Generic;
using Aga.Controls.Tree;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   internal class RevisionPreviewBrowserModelData
   {
      public RevisionPreviewBrowserModelData()
      {
      }

      internal RevisionPreviewBrowserModelData(ComparisonEx.Statistic statistic)
      {
         Statistic = statistic;
      }

      internal ComparisonEx.Statistic Statistic { get; }
   }

   internal class RevisionPreviewBrowserModel : ITreeModel
   {
      internal RevisionPreviewBrowserModelData Data
      {
         get
         {
            return _data;
         }
         set
         {
            _data = value;
            StructureChanged?.Invoke(this, new TreePathEventArgs()); // refresh the view
         }
      }

      public System.Collections.IEnumerable GetChildren(TreePath treePath)
      {
         if (!treePath.IsEmpty() && !(treePath.LastNode is RevisionPreviewBrowserBaseItem))
         {
            return null;
         }

         if (Data?.Statistic == null)
         {
            return null;
         }
         else if (treePath.IsEmpty())
         {
            return getItems(null, Data.Statistic.Tree);
         }
         else if (treePath.LastNode is RevisionPreviewBrowserFolderItem parentFolderItem)
         {
            return getItems(treePath.LastNode as RevisionPreviewBrowserBaseItem, parentFolderItem.FolderItem);
         }
         return null;
      }

      private System.Collections.IEnumerable getItems(
         RevisionPreviewBrowserBaseItem parent, CompositeItem compositeItem)
      {
         List<RevisionPreviewBrowserBaseItem> items = new List<RevisionPreviewBrowserBaseItem>();
         foreach (BaseItem child in compositeItem.ChildItems)
         {
            if (child is FolderItem folderItem)
            {
               items.Add(new RevisionPreviewBrowserFolderItem(parent, this, folderItem));
            }
            else if (child is FileDiffItem fileItem)
            {
               items.Add(new RevisionPreviewBrowserFileItem(parent, this, fileItem));
            }
         }
         return sortItems(items);
      }

      private IEnumerable<RevisionPreviewBrowserBaseItem> sortItems(
         IEnumerable<RevisionPreviewBrowserBaseItem> items)
      {
         return items
            // put folders first
            .OrderBy(item => (item is RevisionPreviewBrowserFileItem))
            // sort alphabetically
            .ThenBy(item => item.Name);
      }

      public bool IsLeaf(TreePath treePath)
      {
         return treePath.LastNode is RevisionPreviewBrowserFileItem;
      }

      public event EventHandler<TreeModelEventArgs> NodesChanged;
      public event EventHandler<TreeModelEventArgs> NodesInserted;
      public event EventHandler<TreeModelEventArgs> NodesRemoved;
      public event EventHandler<TreePathEventArgs> StructureChanged;

      private RevisionPreviewBrowserModelData _data;
   }
}

