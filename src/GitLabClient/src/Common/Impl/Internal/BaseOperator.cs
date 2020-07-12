using System;
using System.Threading.Tasks;
using GitLabSharp;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class BaseOperator : IDisposable
   {
      internal BaseOperator(string hostname, IHostProperties hostProperties)
      {
         _settings = hostProperties;
         Hostname = hostname;
         _client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
      }

      async protected Task<T> callWithSharedClient<T>(Func<GitLabTaskRunner, Task<T>> func)
      {
         return await func(_client);
      }

      protected string Hostname { get; }

      public void Dispose()
      {
         _client.Dispose();
      }

      private readonly IHostProperties _settings;
      private readonly GitLabTaskRunner _client;
   }
}

