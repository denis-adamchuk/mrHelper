namespace mrHelper.Client
{
   public class WorkflowState
   {
      public struct Host
      {
         string Name;
         string AccessToken;
         public List<Project> Projects;
      }

      public Host GitLab
      {
         get;
         set
         {
            Host = value;
            Project = null;
            MergeRequest = null;
         }
      }

      public Project Project
      {
         get;
         set
         {
            Project = value;
            MergeRequest = null;
         }
      }

      public MergeRequest MergeRequest
      {
         get;
         set;
      }
   }
}

