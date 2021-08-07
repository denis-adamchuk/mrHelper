using System;
using System.Diagnostics;
using mrHelper.Common.Constants;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   internal abstract class RevisionPreviewBrowserBaseItem
   {
      internal RevisionPreviewBrowserBaseItem(
         RevisionPreviewBrowserBaseItem parent, RevisionPreviewBrowserModel owner)
      {
         Parent = parent;
         Owner = owner;
      }

      public RevisionPreviewBrowserBaseItem Parent { get; }
      public RevisionPreviewBrowserModel Owner { get; }

      public virtual string Name { get; }
      public virtual string Added { get; }
      public virtual string Deleted { get; }
   }

   internal class RevisionPreviewBrowserFolderItem : RevisionPreviewBrowserBaseItem
   {
      internal RevisionPreviewBrowserFolderItem(
         RevisionPreviewBrowserBaseItem parent, RevisionPreviewBrowserModel owner, FolderItem folderItem)
         : base(parent, owner)
      {
         FolderItem = folderItem;
      }

      internal FolderItem FolderItem { get; }

      public override string Name => FolderItem.Name;
      public override string Added => FolderItem.DiffSize?.Added.ToString() ?? String.Empty;
      public override string Deleted => FolderItem.DiffSize?.Deleted.ToString() ?? String.Empty;
   }

   internal class RevisionPreviewBrowserFileItem : RevisionPreviewBrowserBaseItem
   {
      internal RevisionPreviewBrowserFileItem(
         RevisionPreviewBrowserBaseItem parent, RevisionPreviewBrowserModel owner, FileDiffItem fileItem)
         : base(parent, owner)
      {
         FileItem = fileItem;
      }

      internal FileDiffItem FileItem { get; }

      public override string Name => FileItem.Name;
      public override string Added => getAdded();
      public override string Deleted => getDeleted();

      private string getAdded()
      {
         switch (FileItem.Data.Kind)
         {
            case DiffKind.New:
            case DiffKind.Deleted:
            case DiffKind.Modified:
            case DiffKind.RenamedTo:
            case DiffKind.MovedTo:
               return FileItem.DiffSize?.Added.ToString() ?? Constants.NoDataAtGitLab;
            case DiffKind.RenamedFrom:
               return "renamed";
            case DiffKind.MovedFrom:
               return "moved";
            default:
               Debug.Assert(false);
               break;
         }
         return String.Empty;
      }

      private string getDeleted()
      {
         switch (FileItem.Data.Kind)
         {
            case DiffKind.New:
            case DiffKind.Deleted:
            case DiffKind.Modified:
            case DiffKind.RenamedTo:
            case DiffKind.MovedTo:
               return FileItem.DiffSize?.Deleted.ToString() ?? String.Empty;
            case DiffKind.RenamedFrom:
               return "renamed";
            case DiffKind.MovedFrom:
               return "moved";
            default:
               Debug.Assert(false);
               break;
         }
         return String.Empty;
      }
   }
}

