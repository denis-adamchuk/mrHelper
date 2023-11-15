using GitLabSharp.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IEnvironmentStatusLoader
   {
      Task LoadEnvironmentStatus(IEnumerable<MergeRequestKey> mergeRequestKeys);
   }
}

