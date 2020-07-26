using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class UserAccessor : IUserAccessor
   {
      public UserAccessor(string hostname, IHostProperties hostProperties)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
      }

      public Task<User> GetCurrentUserAsync()
      {
         UserOperator userOperator = new UserOperator(_hostname, _hostProperties);
         try
         {
            return userOperator.SearchCurrentUserAsync();
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<User> SearchUserByNameAsync(string name, bool isUsername)
      {
         UserOperator userOperator = new UserOperator(_hostname, _hostProperties);
         try
         {
            IEnumerable<User> users = await userOperator.SearchUserAsync(name, isUsername);
            return users.Any() ? users.First() : null;
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
   }
}
