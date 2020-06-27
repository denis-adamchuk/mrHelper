using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.FileStorage
{
   internal interface IFileStorage : ILocalGitCommitStorage
   {
      bool ExpectingClone { get; }

      MergeRequestKey MergeRequestKey { get; }
   }
}

