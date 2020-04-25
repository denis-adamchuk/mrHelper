using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Workflow
{
   public interface IMergeRequestListLoader
   {
      event Action<Project> PreLoadProjectMergeRequests;
      event Action<string, Project, IEnumerable<MergeRequest>> PostLoadProjectMergeRequests;
      event Action FailedLoadProjectMergeRequests;
   }
}

