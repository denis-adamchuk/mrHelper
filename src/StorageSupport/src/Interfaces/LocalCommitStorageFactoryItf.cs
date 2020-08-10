using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   public enum LocalCommitStorageType
   {
      FullGitRepository,
      ShallowGitRepository,
      FileStorage
   }

   public interface ILocalCommitStorageFactory
   {
      string ParentFolder { get; }

      ILocalCommitStorage GetStorage(ProjectKey projectKey, LocalCommitStorageType type);

      event Action<ILocalCommitStorage> GitRepositoryCloned;
   }
}

