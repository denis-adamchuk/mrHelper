using System;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   public struct GitLabClientContext
   {
      public ISynchronizeInvoke SynchronizeInvoke;
      public IHostProperties HostProperties;
      public MergeRequestFilter MergeRequestFilter;
      public IEnumerable<string> Keywords;
      public int AutoUpdatePeriodMs;
      public Action<ProjectKey> OnForbiddenProject;
      public Action<ProjectKey> OnNotFoundProject;
      public int MaxSearchResults;
   }
}

