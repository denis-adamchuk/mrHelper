namespace mrHelper
{
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
   }

   struct Commit
   {
      public string Id;
      public string ShortId;
      public string Title;
   }

   enum MergeRequestState
   {
      Opened,
      Closed,
      Locked,
      Merged
   }
}
