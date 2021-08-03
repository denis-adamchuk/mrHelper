using System;
using System.Diagnostics;
using System.Linq;
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
      public override string Added => FolderItem.Added.ToString();
      public override string Deleted => FolderItem.Deleted.ToString();
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

      public override string Name => getName();
      public override string Added => FileItem.Added.ToString();
      public override string Deleted => FileItem.Deleted.ToString();

      private string getName()
      {
         switch (FileItem.Data.Kind)
         {
            case DiffKind.New:
            case DiffKind.Deleted:
            case DiffKind.Modified:
            case DiffKind.RenamedTo:
            case DiffKind.MovedTo:
               return FileItem.Name;
            case DiffKind.RenamedFrom:
               return String.Format("{0} (renamed)", FileItem.Name);
            case DiffKind.MovedFrom:
               return String.Format("{0} (moved)", FileItem.Name);
            default:
               Debug.Assert(false);
               break;
         }
         return String.Empty;
      }
   }
}

