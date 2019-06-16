namespace mrHelper
{
   struct MergeRequest
   {
      public int Id;
      public string Title;
      public string Description;
      public string SourceBranch;
      public string TargetBranch;
   }

   struct Commit
   {
      public string Id;
      public string ShortId;
      public string Title;
   }
}
