using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   public interface IProjectUpdateContextProviderFactory
   {
      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IProjectUpdateContextProvider GetLocalBasedContextProvider(ProjectKey pk);

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IProjectUpdateContextProvider GetRemoteBasedContextProvider(MergeRequestKey mrk);
   }
}

