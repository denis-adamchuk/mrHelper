namespace mrHelper.Client.Types
{
   public class SearchByProject
   {
      public string ProjectName;
   }

   public class SearchByIId : SearchByProject
   {
      public int IId;
   }

   public class SearchByTargetBranch
   {
      public string TargetBranchName;
   }

   public class SearchByText
   {
      public string Text;
   }

   public enum EComparableEntityType
   {
      Commit,
      Version
   }
}

