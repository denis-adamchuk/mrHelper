using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Updates
{
   public struct ProjectUpdate
   {
      public string HostName;
      public string ProjectName;
      public DateTime LatestChange;
   }

   public interface IProjectWatcher
   {
      event Action<List<ProjectUpdate>> OnProjectUpdate;
   }
}

