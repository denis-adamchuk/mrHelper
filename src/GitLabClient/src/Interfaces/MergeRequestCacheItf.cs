using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestCache
   {
      IEnumerable<ProjectKey> GetProjects();

      /// <summary>
      /// Return open merge requests in the given project
      /// </summary>
      IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey);

      /// <summary>
      /// Return currently cached Merge Request by its key or null if nothing is cached
      /// </summary>
      MergeRequest GetMergeRequest(MergeRequestKey mrk);

      /// <summary>
      /// Return currently cached latest version of the given Merge Request
      /// </summary>
      GitLabSharp.Entities.Version GetLatestVersion(MergeRequestKey mrk);

      /// <summary>
      /// Return currently cached latest version among all cached Merge Requests
      /// </summary>
      GitLabSharp.Entities.Version GetLatestVersion(ProjectKey projectKey);

      /// <summary>
      /// </summary>
      IEnumerable<GitLabSharp.Entities.Version> GetVersions(MergeRequestKey mrk);

      /// <summary>
      /// </summary>
      IEnumerable<GitLabSharp.Entities.Version> GetVersions(ProjectKey projectKey);

      /// <summary>
      /// </summary>
      IEnumerable<GitLabSharp.Entities.Commit> GetCommits(MergeRequestKey mrk);

      /// <summary>
      /// </summary>
      void RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished);

      /// <summary>
      /// </summary>
      event Action<UserEvents.MergeRequestEvent> MergeRequestEvent;
   }
}

