namespace mrHelper.Client
{
   public class WorkflowState
   {
      public string HostName
      {
         get;
         set
         {
            HostName = value;
            Projects = null;
         }
      }

      public List<Project> Projects
      {
         get;
         set
         {
            Projects = value;
            Project = null;
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

