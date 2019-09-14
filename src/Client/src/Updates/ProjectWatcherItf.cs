using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Updates
{
   public struct ProjectUpdate
   {
      public string HostName;
      public string ProjectName;

      /// <summary>
      /// Timestamp of an event within the given project that caused this update.
      /// If there are multiple events, Timestamp belongs to the latest of them.
      /// </summary>
      public DateTime Timestamp;
   }

   public interface IProjectWatcher
   {
      event Action<List<ProjectUpdate>> OnProjectUpdate;
   }
}

