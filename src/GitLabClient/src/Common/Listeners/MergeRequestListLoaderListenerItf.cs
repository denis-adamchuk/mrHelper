using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   public interface IMergeRequestListLoaderListener
   {
      void OnPreLoadProjectMergeRequests(ProjectKey project);
      void OnPostLoadProjectMergeRequests(ProjectKey project, IEnumerable<MergeRequest> mergeRequests);
      void OnFailedLoadProjectMergeRequests(ProjectKey project);
   }
}

