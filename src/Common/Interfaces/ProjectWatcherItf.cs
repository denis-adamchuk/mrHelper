using System;

namespace mrHelper.Common.Interfaces
{
   public struct ProjectKey
   {
      public string HostName;
      public string ProjectName;
   }

   /// <summary>
   /// Timestamp of an event within the given project that caused this update.
   /// If there are multiple events, Timestamp belongs to the latest of them.
   /// </summary>
   public class ProjectUpdate : System.Collections.Generic.Dictionary<ProjectKey, ProjectSnapshot> {}

   public interface IProjectWatcher
   {
      event Action<ProjectUpdate> OnProjectUpdate;
   }
}

