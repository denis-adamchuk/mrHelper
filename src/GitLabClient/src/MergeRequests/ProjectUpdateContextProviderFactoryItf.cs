using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   public interface IProjectUpdateContextProviderFactory
   {
      /// <summary>
      /// </summary>
      ICommitStorageUpdateContextProvider GetLocalBasedContextProvider(MergeRequestKey mrk);
   }
}

