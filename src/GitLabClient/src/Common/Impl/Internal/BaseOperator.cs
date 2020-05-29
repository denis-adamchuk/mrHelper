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
         _client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
      }

      async protected Task<T> callWithSharedClient<T>(Func<GitLabTaskRunner, Task<T>> func)
      {
         return await func(_client);
      }

      protected void Cancel()
      {
         _client.CancelAll();
      }

      private readonly IHostProperties _settings;
      private readonly GitLabTaskRunner _client;
   }
}

