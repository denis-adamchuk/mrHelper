using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   internal interface IVersionLoader
   {
      Task<bool> LoadVersionsAndCommits(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests);
      Task<bool> LoadCommitsAsync(MergeRequestKey mrk);
      Task<bool> LoadVersionsAsync(MergeRequestKey mrk);
   }
}

