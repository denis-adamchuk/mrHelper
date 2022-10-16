using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Operators
{
   internal class AvatarOperator : IDisposable
   {
      public void Dispose()
      {
         foreach (var cts in _avatarCancellationToken)
         {
            cts.Cancel();
            cts.Dispose();
         }

         foreach (var cl in _avatarClient)
         {
            cl.Dispose();
         }
      }

      async internal Task<byte[]> GetAvatarAsync(string avatarUrl)
      {
         return await getHttpClient().GetDataTaskAsync(avatarUrl);
      }

      private GitLabSharp.HttpClient getHttpClient()
      {
         System.Threading.CancellationTokenSource cancellationToken =
            new System.Threading.CancellationTokenSource();
         GitLabSharp.HttpClient httpClient =
            new GitLabSharp.HttpClient(String.Empty, cancellationToken); // TODO !!

         _avatarClient.Add(httpClient);
         _avatarCancellationToken.Add(cancellationToken);

         return httpClient;
      }

      private List<System.Threading.CancellationTokenSource> _avatarCancellationToken =
         new List<System.Threading.CancellationTokenSource>();
      private List<GitLabSharp.HttpClient> _avatarClient = new List<GitLabSharp.HttpClient>();
   }
}

