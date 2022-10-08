using System;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class AvatarOperator : BaseOperator, IDisposable
   {
      internal AvatarOperator(string hostname, IHostProperties hostProperties,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(hostname, hostProperties, networkOperationStatusListener)
      {
      }

      public new void Dispose()
      {
         base.Dispose();
         _avatarClient?.Dispose();
      }

      async internal Task<byte[]> GetAvatarAsync(string avatarUrl)
      {
         GitLabSharp.HttpClient httpClient = getHttpClientForAvatars();
         return await httpClient.GetDataTaskAsync(avatarUrl);
      }

      private GitLabSharp.HttpClient getHttpClientForAvatars()
      {
         if (_avatarClient == null)
         {
            if (_avatarCancellationToken == null)
            {
               _avatarCancellationToken = new System.Threading.CancellationTokenSource();
            }
            _avatarClient = new GitLabSharp.HttpClient(Hostname, _avatarCancellationToken);
         }
         return _avatarClient;
      }

      private System.Threading.CancellationTokenSource _avatarCancellationToken;
      private GitLabSharp.HttpClient _avatarClient;
   }
}

