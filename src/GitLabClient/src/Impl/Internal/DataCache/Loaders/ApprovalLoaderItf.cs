using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IApprovalLoader
   {
      Task LoadApprovals(IEnumerable<MergeRequestKey> mergeRequestKeys);
   }
}

