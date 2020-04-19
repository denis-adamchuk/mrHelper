using System;

namespace mrHelper.Common.Interfaces
{
   public struct ProjectKey
   {
      public string HostName;
      public string ProjectName;
   }

   public class ProjectWatcherUpdateArgs
   {
      public ProjectKey ProjectKey;

      /// <summary>
      /// Project Update contains the timestamp of an event within the given project that caused this update.
      /// If there were multiple events, Timestamp belongs to the latest of them.
      /// </summary>
      public FullProjectUpdate ProjectUpdate;
   }

   public interface IProjectWatcher
   {
      event Action<ProjectWatcherUpdateArgs> OnProjectUpdate;
   }
}

