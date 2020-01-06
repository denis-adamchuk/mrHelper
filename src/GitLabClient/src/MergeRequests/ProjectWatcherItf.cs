using System;
using System.Collections.Generic;
using mrHelper.Client.Types;

namespace mrHelper.Client.MergeRequests
{
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
      event Action<List<ProjectUpdate>> OnProjectUpdate;
   }
}

