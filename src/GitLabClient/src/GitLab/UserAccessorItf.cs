using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Common
{
   public interface IUserAccessor
   {
      Task<User> GetCurrentUserAsync();

      Task<User> SearchUserByNameAsync(string name, bool isUsername);
   }
}

