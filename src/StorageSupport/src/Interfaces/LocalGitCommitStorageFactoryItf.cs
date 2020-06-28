using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   public interface ILocalGitCommitStorageFactory : System.IDisposable
   {
      string ParentFolder { get; }

      ILocalGitCommitStorage GetStorage(MergeRequestKey mrk);
   }
}

