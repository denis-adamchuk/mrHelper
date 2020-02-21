using System;
using System.Collections.Generic;

namespace mrHelper.Common.Interfaces
{
   public struct ProjectKey
   {
      public string HostName;
      public string ProjectName;
   }

   public struct ProjectUpdate
   {
      public ProjectKey ProjectKey;

      /// <summary>
      /// Timestamp of an event within the given project that caused this update.
      /// If there are multiple events, Timestamp belongs to the latest of them.
      /// </summary>
      public DateTime Timestamp;
   }

   public interface IProjectWatcher
   {
      event Action<IEnumerable<ProjectUpdate>> OnProjectUpdate;
   }
}

