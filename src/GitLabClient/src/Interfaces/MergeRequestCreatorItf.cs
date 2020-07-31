using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestCreator
   {
      Task<MergeRequest> CreateMergeRequest(CreateNewMergeRequestParameters parameters);
   }
}

