using System;
using System.Diagnostics;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   public enum RevisionType
   {
      Version,
      Commit
   }

   internal abstract class RevisionBrowserBaseItem
   {
      internal RevisionBrowserBaseItem(RevisionBrowserBaseItem parent, RevisionBrowserModel owner)
      {
         Parent = parent;
         Owner = owner;
      }

      public RevisionBrowserBaseItem Parent { get; }
      public RevisionBrowserModel Owner { get; }

      public virtual string Name { get; }
      public virtual string TimeAgo { get; }
   }

   internal class RevisionBrowserTypeItem : RevisionBrowserBaseItem
   {
      internal RevisionBrowserTypeItem(RevisionType type, RevisionBrowserModel owner)
         : base(null, owner)
      {
         Type = type;
         switch (type)
         {
            case RevisionType.Commit: Name = "Commits"; break;
            case RevisionType.Version: Name = "Versions"; break;
            default: Debug.Assert(false); break;
         }
         TimeAgo = String.Empty;
      }

      internal RevisionType Type { get; }
      public override string Name { get; }
      public override string TimeAgo { get; }
   }

   internal class RevisionBrowserItem : RevisionBrowserBaseItem
   {
      internal RevisionBrowserItem(string name, DateTime timestamp, string sha,
         RevisionBrowserBaseItem parent, RevisionBrowserModel owner, string description, bool isReviewed,
         int invertedDisplayIndex)
         : base(parent, owner)
      {
         Name = name;
         InvertedDisplayIndex = invertedDisplayIndex;
         TimeAgo = TimeUtils.DateTimeToStringAgo(timestamp);
         Timestamp = TimeUtils.DateTimeToString(timestamp);
         FullSHA = sha;
         Description = description;
         IsReviewed = isReviewed;
      }

      public override string Name { get; }
      public override string TimeAgo { get; }
      public int InvertedDisplayIndex { get; }
      public string FullSHA { get; }
      public string Description { get; }
      public string Timestamp { get; }
      public bool IsReviewed { get; }
   }
}

