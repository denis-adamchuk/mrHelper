using System.Threading.Tasks;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Workflow
{
   internal interface IVersionLoader : ILoader<IVersionLoaderListener>
   {
      Task<bool> LoadCommitsAsync(MergeRequestKey mrk);
      Task<bool> LoadVersionsAsync(MergeRequestKey mrk, bool invokeCompareableEntitiesCallback);
   }
}

