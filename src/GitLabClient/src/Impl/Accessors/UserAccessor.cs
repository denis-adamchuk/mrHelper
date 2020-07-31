using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class UserAccessor
   {
      internal UserAccessor(string hostname, IHostProperties hostProperties)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
      }

      public Task<User> GetCurrentUserAsync()
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties))
         {
            try
            {
               return userOperator.SearchCurrentUserAsync();
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      public Task<User> SearchUserByNameAsync(string name)
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties))
         {
            try
            {
               return userOperator.SearchUserByNameAsync(name);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      public Task<User> SearchUserByUsernameAsync(string username)
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties))
         {
            try
            {
               return userOperator.SearchUserByUsernameAsync(username);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
   }
}

