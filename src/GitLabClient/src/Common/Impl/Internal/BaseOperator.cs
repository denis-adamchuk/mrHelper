using System;
using System.Threading.Tasks;
using GitLabSharp;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class BaseOperator
   {
      internal BaseOperator(string hostname, IHostProperties hostProperties)
      {
         _settings = hostProperties;
         _client = new GitLabClient(hostname, _settings.GetAccessToken(hostname));
      }

      async protected Task<T> callWithSharedClient<T>(Func<GitLabClient, Task<T>> func)
      {
         return await func(_client);
      }

      protected Task CancelAsync()
      {
         return _client.CancelAsync();
      }

      protected void Cancel()
      {
         _client.Cancel();
      }

      private readonly IHostProperties _settings;
      private readonly GitLabClient _client;
   }
}

