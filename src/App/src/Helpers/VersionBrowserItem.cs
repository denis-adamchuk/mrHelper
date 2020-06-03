using System;
using mrHelper.Common.Constants;

namespace mrHelper.App.Helpers
{
   internal abstract class BaseVersionBrowserItem
   {
      internal BaseVersionBrowserItem(BaseVersionBrowserItem parent, VersionBrowserModel owner)
      {
         Parent = parent;
         Owner = owner;
      }

      public BaseVersionBrowserItem Parent { get; }
      public VersionBrowserModel Owner { get; }

      public virtual string Name { get; }
      public virtual string Timestamp { get; }
      public virtual string SHA { get; }

      public override string ToString()
      {
         throw new NotImplementedException();
      }
   }

   internal class RootVersionBrowserItem : BaseVersionBrowserItem
   {
      internal RootVersionBrowserItem(string name, VersionBrowserModel owner)
         : base(null, owner)
      {
         Name = name;
         Timestamp = String.Empty;
         SHA = String.Empty;
      }

      public override string Name { get; }
      public override string Timestamp { get; }
      public override string SHA { get; }
   }

   internal class LeafVersionBrowserItem : BaseVersionBrowserItem
   {
      internal LeafVersionBrowserItem(string name, DateTime timestamp, string sha,
         BaseVersionBrowserItem parent, VersionBrowserModel owner)
         : base(parent, owner)
      {
         Name = name;
         Timestamp = timestamp.ToLocalTime().ToString(Constants.TimeStampFormat);
         SHA = sha;
      }

      public override string Name { get; }
      public override string Timestamp { get; }
      public override string SHA { get; }
   }
}

