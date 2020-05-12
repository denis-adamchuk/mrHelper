using System.Threading.Tasks;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal interface IVersionLoader
   {
      Task<bool> LoadCommitsAsync(MergeRequestKey mrk);
      Task<bool> LoadVersionsAsync(MergeRequestKey mrk);
   }
}

