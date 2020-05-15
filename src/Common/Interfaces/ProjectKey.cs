namespace mrHelper.Common.Interfaces
{
   public struct ProjectKey
   {
      public ProjectKey(string hostName, string projectName)
      {
         HostName = hostName;
         ProjectName = projectName;
      }

      public string HostName { get; }
      public string ProjectName { get; }
   }
}

