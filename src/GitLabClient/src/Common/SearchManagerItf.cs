using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Common
{
   public interface ISearchManager
   {
      Task<MergeRequest> SearchMergeRequestAsync(string hostname, string projectName, int mergeRequestIId);

      Task<User> SearchUserAsync(string hostname, string name);

      Task<Project> SearchProjectAsync(string hostname, string projectname);
   }
}

