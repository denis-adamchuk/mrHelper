using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Common
{
   public interface ISearchManager
   {
      Task<MergeRequest> SearchMergeRequestAsync(string hostname, string projectName, int mergeRequestIId);

      Task<User> GetCurrentUserAsync(string hostname);

      Task<User> SearchUserByNameAsync(string hostname, string name, bool isUsername);

      Task<Project> SearchProjectAsync(string hostname, string projectname);
   }
}

