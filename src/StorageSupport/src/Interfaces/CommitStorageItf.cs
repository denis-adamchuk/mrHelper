using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   public interface ICommitStorage
   {
      IGitCommandService Git { get; }

      ProjectKey ProjectKey { get; }
   }
}

