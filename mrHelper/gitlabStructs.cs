namespace mrHelper
{
   struct Author
   {
      public int Id;
      public string Name;
      public string Username;
   }

   struct MergeRequest
   {
      public int Id;
      public string Title;
      public string Description;
      public string SourceBranch;
      public string TargetBranch;
      public MergeRequestState State;
      public string[] Labels;
      public string WebUrl;
      public bool WorkInProgress;
      public Author Author;
   }

   struct Commit
   {
      public string Id;
      public string ShortId;
      public string Title;
      public string Message;
      public System.DateTime CommitedDate;
   }

   enum MergeRequestState
   {
      Opened,
      Closed,
      Locked,
      Merged
   }
}
