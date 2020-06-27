using System;
using System.ComponentModel;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal class GitLabBasedCommitStorageFactory : ILocalGitCommitStorageFactory, IDisposable
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// </summary>
      public GitLabBasedCommitStorageFactory(string parentFolder, ISynchronizeInvoke synchronizeInvoke)
      {
         throw new NotImplementedException();
      }

      public ILocalGitCommitStorage GetStorage(MergeRequestKey mrk)
      {
         throw new NotImplementedException();
      }

      public void Dispose()
      {
         throw new NotImplementedException();
      }
   }
}

