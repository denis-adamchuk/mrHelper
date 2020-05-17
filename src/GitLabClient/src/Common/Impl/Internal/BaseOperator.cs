using GitLabSharp;
using mrHelper.Common.Interfaces;
using System;
using System.Threading.Tasks;

namespace mrHelper.Client.Common
{
   internal class BaseOperator
   {
      internal BaseOperator(IHostProperties hostProperties)
      {
         _settings = hostProperties;
      }

      async protected Task<T> callWithNewClient<T>(string hostname, Func<GitLabClient, Task<T>> func)
      {
         GitLabClient client = new GitLabClient(hostname, _settings.GetAccessToken(hostname));
         return await func(client);
      }

      private IHostProperties _settings;
   }
}

