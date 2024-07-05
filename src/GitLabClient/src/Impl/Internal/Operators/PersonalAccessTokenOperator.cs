using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class PersonalAccessTokenOperator : BaseOperator
   {
      internal PersonalAccessTokenOperator(string host, IHostProperties settings,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(host, settings, networkOperationStatusListener)
      {
      }

      async internal Task<PersonalAccessToken> GetTokenAsync()
      {
         return await callWithSharedClient(
               async (client) =>
                  await OperatorCallWrapper.Call(
                     async () =>
                        (PersonalAccessToken)await client.RunAsync(
                           async (gl) =>
                              await gl.PersonalAccessToken.LoadTaskAsync())));
      }

      async internal Task<PersonalAccessToken> RotateTokenAsync(string expiresAt)
      {
         return await callWithSharedClient(
               async (client) =>
                  await OperatorCallWrapper.Call(
                     async () =>
                        (PersonalAccessToken)await client.RunAsync(
                           async (gl) =>
                              await gl.PersonalAccessToken.RotateTaskAsync(expiresAt))));
      }
   }
}

