using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal interface ILocalGitCommitStorageFactory : System.IDisposable
   {
      string ParentFolder { get; }

      ILocalGitCommitStorage GetStorage(MergeRequestKey mrk);
   }
}

