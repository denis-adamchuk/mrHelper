using System.Threading.Tasks;
using GitLabSharp.Accessors;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestCreator
   {
      Task CreateMergeRequest(CreateNewMergeRequestParameters parameters);
   }
}

