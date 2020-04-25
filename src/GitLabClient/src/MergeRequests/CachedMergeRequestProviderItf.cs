using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   public interface ICachedMergeRequestProvider
   {
      event Action<UserEvents.MergeRequestEvent> MergeRequestEvent;

      /// <summary>
      /// Return open merge requests in the given project
      /// </summary>
      IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey);

      /// <summary>
      /// Return currently cached Merge Request by its key or null if nothing is cached
      /// </summary>
      MergeRequest? GetMergeRequest(MergeRequestKey mrk);

      /// <summary>
      /// Return currently cached latest version of the given Merge Request
      /// </summary>
      GitLabSharp.Entities.Version GetLatestVersion(MergeRequestKey mrk);

      /// <summary>
      /// Return currently cached latest version among all cached Merge Requests
      /// </summary>
      GitLabSharp.Entities.Version GetLatestVersion(ProjectKey projectKey);
   }
}

