using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.GitLabClient.Loaders.Cache
{
   internal interface IInternalCache
   {
      /// <summary>
      /// Create a copy of object
      /// </summary>
      IInternalCache Clone();

      /// <summary>
      /// Return a list of cached projects
      /// </summary>
      IEnumerable<ProjectKey> GetProjects();

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      IEnumerable<MergeRequest> GetMergeRequests(ProjectKey key);

      /// <summary>
      /// Return single merge request by its key
      /// </summary>
      MergeRequest GetMergeRequest(MergeRequestKey mrk);

      /// <summary>
      /// Return a list of versions of a specified merge request
      /// </summary>
      IEnumerable<Version> GetVersions(MergeRequestKey mrk);

      /// <summary>
      /// Return a list of commits of a specified merge request
      /// </summary>
      IEnumerable<Commit> GetCommits(MergeRequestKey mrk);

      /// <summary>
      /// Return a list of approvals of a specified merge request
      /// </summary>
      MergeRequestApprovalConfiguration GetApprovals(MergeRequestKey mrk);

      /// <summary>
      /// Return avatar image byte array for a specified user id
      /// </summary>
      byte[] GetAvatar(int userId);
   }
}

