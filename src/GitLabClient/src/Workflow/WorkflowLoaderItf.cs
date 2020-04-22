using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Workflow
{
   public interface IWorkflowLoader
   {
      event Action<string> PreLoadCurrentUser;
      event Action<string, User> PostLoadCurrentUser;
      event Action FailedLoadCurrentUser;

      event Action<Project> PreLoadProjectMergeRequests;
      event Action<string, Project, IEnumerable<MergeRequest>> PostLoadProjectMergeRequests;
      event Action FailedLoadProjectMergeRequests;

      event Action<int> PreLoadSingleMergeRequest;
      event Action<string, string, MergeRequest> PostLoadSingleMergeRequest;
      event Action FailedLoadSingleMergeRequest;

      event Action PreLoadComparableEntities;
      event Action<string, string, MergeRequest, System.Collections.IEnumerable> PostLoadComparableEntities;
      event Action FailedLoadComparableEntities;

      event Action PreLoadVersions;
      event Action<string, string, MergeRequest, IEnumerable<GitLabSharp.Entities.Version>> PostLoadVersions;
      event Action FailedLoadVersions;
   }
}

