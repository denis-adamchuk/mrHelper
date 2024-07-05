using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class PersonalAccessTokenAccessor
   {
      internal PersonalAccessTokenAccessor(string hostname, IHostProperties hostProperties,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<PersonalAccessToken> GetPersonalAccessTokenAsync()
      {
         using (PersonalAccessTokenOperator op = new PersonalAccessTokenOperator(
            _hostname, _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               return await op.GetTokenAsync();
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      async public Task<PersonalAccessToken> RotatePersonalAccessTokenAsync(string expiresAt)
      {
         using (PersonalAccessTokenOperator op = new PersonalAccessTokenOperator(
            _hostname, _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               return await op.RotateTokenAsync(expiresAt);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

