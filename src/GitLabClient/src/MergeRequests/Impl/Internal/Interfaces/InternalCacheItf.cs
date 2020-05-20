using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
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
      /// Return a list of versions of a specified merge request
      /// </summary>
      IEnumerable<Version> GetVersions(MergeRequestKey mrk);

      /// <summary>
      /// Return a list of commits of a specified merge request
      /// </summary>
      IEnumerable<Commit> GetCommits(MergeRequestKey mrk);
   }
}

