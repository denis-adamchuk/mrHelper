using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class MergeRequestListLoaderNotifier : BaseNotifier<IMergeRequestListLoaderListener>, IMergeRequestListLoaderListener
   {
      public void OnPreLoadProjectMergeRequests(ProjectKey project) =>
         notifyAll(x => x.OnPreLoadProjectMergeRequests(project));

      public void OnPostLoadProjectMergeRequests(ProjectKey project, IEnumerable<MergeRequest> mergeRequests) =>
         notifyAll(x => x.OnPostLoadProjectMergeRequests(project, mergeRequests));

      public void OnFailedLoadProjectMergeRequests(ProjectKey project) =>
         notifyAll(x => x.OnFailedLoadProjectMergeRequests(project));
   }
}

