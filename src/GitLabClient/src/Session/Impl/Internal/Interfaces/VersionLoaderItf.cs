using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   internal interface IVersionLoader
   {
      Task LoadVersionsAndCommits(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests);
      Task LoadCommitsAsync(MergeRequestKey mrk);
      Task LoadVersionsAsync(MergeRequestKey mrk);
   }
}

