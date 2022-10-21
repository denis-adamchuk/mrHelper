using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Operators
{
   internal class AvatarOperator : IDisposable
   {
      public void Dispose()
      {
         foreach (System.Threading.CancellationTokenSource cts in _avatarCancellationToken)
         {
            cts.Cancel();
            cts.Dispose();
         }
         _avatarCancellationToken.Clear();

         foreach (GitLabSharp.HttpClient cl in _avatarClient)
         {
            cl.Dispose();
         }
         _avatarClient.Clear();
      }

      async internal Task<byte[]> GetAvatarAsync(string avatarUrl)
      {
         System.Threading.CancellationTokenSource cancellationToken =
            new System.Threading.CancellationTokenSource();
         GitLabSharp.HttpClient httpClient =
            new GitLabSharp.HttpClient(String.Empty, cancellationToken);

         _avatarClient.Add(httpClient);
         _avatarCancellationToken.Add(cancellationToken);

         byte[] result = await httpClient.GetDataTaskAsync(avatarUrl);

         if (_avatarCancellationToken.Contains(cancellationToken))
         {
            cancellationToken.Dispose();
            _avatarCancellationToken.Remove(cancellationToken);
         }

         if (_avatarClient.Contains(httpClient))
         {
            httpClient.Dispose();
            _avatarClient.Remove(httpClient);
         }

         return result;
      }

      private List<System.Threading.CancellationTokenSource> _avatarCancellationToken =
         new List<System.Threading.CancellationTokenSource>();
      private List<GitLabSharp.HttpClient> _avatarClient = new List<GitLabSharp.HttpClient>();
   }
}

