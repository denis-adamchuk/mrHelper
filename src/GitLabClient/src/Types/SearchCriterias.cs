namespace mrHelper.Client.Types
{
   public class SearchByProject
   {
      public SearchByProject(string projectName)
      {
         ProjectName = projectName;
      }

      public string ProjectName { get; }
   }

   public class SearchByIId
   {
      public SearchByIId(string projectName, int iId)
      {
         ProjectName = projectName;
         IId = iId;
      }

      public string ProjectName { get; }
      public int IId { get; }
   }

   public class SearchByTargetBranch
   {
      public SearchByTargetBranch(string targetBranchName)
      {
         TargetBranchName = targetBranchName;
      }

      public string TargetBranchName { get; }
   }

   public class SearchByText
   {
      public SearchByText(string text)
      {
         Text = text;
      }

      public string Text { get; }
   }
}

