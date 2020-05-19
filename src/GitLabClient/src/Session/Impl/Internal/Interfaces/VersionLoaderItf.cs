using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal interface IVersionLoader
   {
      Task<bool> LoadVersionsAndCommits(IEnumerable<MergeRequestKey> mergeRequestKeys);
      Task<bool> LoadCommitsAsync(MergeRequestKey mrk);
      Task<bool> LoadVersionsAsync(MergeRequestKey mrk);
   }
}

