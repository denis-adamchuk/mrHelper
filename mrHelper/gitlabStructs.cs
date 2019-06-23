using System.Collections.Generic;

namespace mrHelper
{
   struct Author
   {
      public int Id;
      public string Name;
      public string Username;
   }

   enum MergeRequestState
   {
      Opened,
      Closed,
      Locked,
      Merged
   }

   struct MergeRequest
   {
      public int Id;
      public string Title;
      public string Description;
      public string SourceBranch;
      public string TargetBranch;
      public MergeRequestState State;
      public List<string> Labels;
      public string WebUrl;
      public bool WorkInProgress;
      public Author Author;
      public string BaseSHA;
      public string HeadSHA;
      public string StartSHA;
   }

   struct Commit
   {
      public string Id;
      public string ShortId;
      public string Title;
      public string Message;
      public System.DateTime CommitedDate;
   }

   struct Version
   {
      public int Id;
      public string HeadSHA;
      public string BaseSHA;
      public string StartSHA;
      public System.DateTime CreatedAt;
   }
}
