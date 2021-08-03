using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.StorageSupport
{
   public class BaseItem
   {
      public BaseItem(string name)
      {
         Name = name;
      }

      public string Name;
      public virtual int Added { get; }
      public virtual int Deleted { get; }
   }

   public class CompositeItem : BaseItem
   {
      public CompositeItem(string name)
         : base(name)
      {
      }

      public List<BaseItem> ChildItems = new List<BaseItem>();
      public override int Added => ChildItems.Sum(child => child.Added);
      public override int Deleted => ChildItems.Sum(child => child.Deleted);
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

   public struct DiffDescription
   {
      public DiffDescription(int added, int deleted, DiffKind kind, string anotherName)
      {
         Added = added;
         Deleted = deleted;
         Kind = kind;
         AnotherName = anotherName;
      }

      public int Added { get; }
      public int Deleted { get; }
      public DiffKind Kind { get; }
      public string AnotherName { get; }
   }

   public class FileDiffItem : BaseItem
   {
      public FileDiffItem(string name, DiffDescription data)
         : base(name)
      {
         Data = data;
      }

      public DiffDescription Data;
      public override int Added => Data.Added;
      public override int Deleted => Data.Deleted;
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

