using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class UserAccessor
   {
      internal UserAccessor(string hostname, IHostProperties hostProperties,
         IConnectionLossListener connectionLossListener)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
         _connectionLossListener = connectionLossListener;
      }

      async public Task<User> GetCurrentUserAsync()
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties, _connectionLossListener))
         {
            try
            {
               return await userOperator.SearchCurrentUserAsync();
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      async public Task<User> SearchUserByNameAsync(string name)
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties, _connectionLossListener))
         {
            try
            {
               return await userOperator.SearchUserByNameAsync(name);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      async public Task<User> SearchUserByUsernameAsync(string username)
      {
         using (UserOperator userOperator = new UserOperator(_hostname, _hostProperties, _connectionLossListener))
         {
            try
            {
               return await userOperator.SearchUserByUsernameAsync(username);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

