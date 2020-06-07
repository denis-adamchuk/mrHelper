using System.Threading.Tasks;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   public interface IMergeRequestLoader
   {
      Task LoadMergeRequest(MergeRequestKey mrk);
   }
}

