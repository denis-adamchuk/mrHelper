using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IVersionLoader
   {
      Task LoadVersionsAndCommits(IEnumerable<MergeRequestKey> mergeRequestKeys);
   }
}

