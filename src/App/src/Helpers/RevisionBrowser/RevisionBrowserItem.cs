using System;
using System.Diagnostics;
using mrHelper.Common.Constants;

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
      public virtual string Timestamp { get; }
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
         Timestamp = String.Empty;
      }

      internal RevisionType Type { get; }
      public override string Name { get; }
      public override string Timestamp { get; }
   }

   internal class RevisionBrowserItem : RevisionBrowserBaseItem
   {
      internal RevisionBrowserItem(string name, DateTime timestamp, string sha,
         RevisionBrowserBaseItem parent, RevisionBrowserModel owner, string tooltipText, bool isReviewed,
         int invertedDisplayIndex)
         : base(parent, owner)
      {
         Name = name;
         InvertedDisplayIndex = invertedDisplayIndex;
         Timestamp = timestamp.ToString(Constants.TimeStampFormat) + " (UTC)";
         FullSHA = sha;
         TooltipText = tooltipText;
         IsReviewed = isReviewed;
      }

      public override string Name { get; }
      public override string Timestamp { get; }
      public int InvertedDisplayIndex { get; }
      public string FullSHA { get; }
      public string TooltipText { get; }
      public bool IsReviewed { get; }
   }
}

