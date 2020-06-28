using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   internal interface IGitRepository : ILocalCommitStorage
   {
      Task<bool> ContainsSHAAsync(string sha);

      bool ExpectingClone { get; }
   }
}

