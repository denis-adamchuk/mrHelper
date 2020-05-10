using mrHelper.Client.Common;
using mrHelper.Client.Types;
using System.Threading.Tasks;

namespace mrHelper.Client.Workflow
{
   public interface IMergeRequestLoader : ILoader<IMergeRequestLoaderListener>
   {
      Task<bool> LoadMergeRequest(MergeRequestKey mrk, EComparableEntityType comparableEntityType);
   }
}

