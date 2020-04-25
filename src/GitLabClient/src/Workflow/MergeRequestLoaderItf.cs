using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Workflow
{
   public interface IMergeRequestLoader
   {
      event Action<int> PreLoadMergeRequest;
      event Action<string, string, MergeRequest> PostLoadMergeRequest;
      event Action FailedLoadMergeRequest;

      event Action PreLoadComparableEntities;
      event Action<string, string, MergeRequest, System.Collections.IEnumerable> PostLoadComparableEntities;
      event Action FailedLoadComparableEntities;

      event Action PreLoadVersions;
      event Action<string, string, MergeRequest, IEnumerable<GitLabSharp.Entities.Version>> PostLoadVersions;
      event Action FailedLoadVersions;
   }
}

