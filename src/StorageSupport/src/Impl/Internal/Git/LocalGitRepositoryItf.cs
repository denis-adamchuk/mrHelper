using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   internal interface ILocalGitRepository : ILocalGitCommitStorage
   {
      Task<bool> ContainsSHAAsync(string sha);

      bool ExpectingClone { get; }

      ProjectKey ProjectKey { get; }
   }
}
