using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IVersionLoader
   {
      Task LoadVersionsAndCommits(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests);
   }
}

