using System.Threading.Tasks;
using GitLabSharp.Accessors;

namespace mrHelper.Client.MergeRequests
{
   public interface IMergeRequestCreator
   {
      Task CreateMergeRequest(CreateNewMergeRequestParameters parameters);
   }
}

