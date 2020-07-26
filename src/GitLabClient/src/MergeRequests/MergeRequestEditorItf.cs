using System.Threading.Tasks;
using GitLabSharp.Accessors;

namespace mrHelper.Client.MergeRequests
{
   public interface IMergeRequestEditor
   {
      Task ModifyMergeRequest(CreateNewMergeRequestParameters parameters);
   }
}

