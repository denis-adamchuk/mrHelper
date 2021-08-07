using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.StorageSupport
{
   public struct DiffSize
   {
      public DiffSize(int added, int deleted)
      {
         Added = added;
         Deleted = deleted;
      }

      public int Added { get; }
      public int Deleted { get; }
   }

   public class BaseItem
   {
      public BaseItem(string name)
      {
         Name = name;
      }

      public string Name;
      public virtual DiffSize? DiffSize { get; }
   }

   public class CompositeItem : BaseItem
   {
      public CompositeItem(string name)
         : base(name)
      {
      }

      public List<BaseItem> ChildItems = new List<BaseItem>();
      public override DiffSize? DiffSize =>
         ChildItems.All(child => !child.DiffSize.HasValue)
            ? new DiffSize?()
            : new DiffSize(ChildItems.Sum(child => child.DiffSize?.Added ?? 0),
                           ChildItems.Sum(child => child.DiffSize?.Deleted ?? 0));
   }

   public enum DiffKind
   {
      New,
      RenamedFrom,
      RenamedTo,
      MovedFrom,
      MovedTo,
      Deleted,
      Modified
   }

   public struct FileDiffDescription
   {
      public FileDiffDescription(DiffSize? diffSize, DiffKind kind, string anotherName)
      {
         DiffSize = diffSize;
         Kind = kind;
         AnotherName = anotherName;
      }

      public DiffSize? DiffSize { get; }
      public DiffKind Kind { get; }
      public string AnotherName { get; }
   }

   public class FileDiffItem : BaseItem
   {
      public FileDiffItem(string name, FileDiffDescription data)
         : base(name)
      {
         Data = data;
      }

      public FileDiffDescription Data;
      public override DiffSize? DiffSize => Data.DiffSize;
   }

   public class FolderItem : CompositeItem
   {
      public FolderItem(string name)
         : base(name)
      {
      }
   }

   public class DiffTree : CompositeItem
   {
      public DiffTree()
         : base(null)
      {
      }
   }
}

