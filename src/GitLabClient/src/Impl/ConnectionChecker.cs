using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.GitLabClient.Interfaces;
using mrHelper.GitLabClient.Operators;
using System;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient
{
   public enum ConnectionCheckStatus
   {
      OK,
      BadHostname,
      BadAccessToken
   }

   public class ConnectionChecker : IConnectionLossListener
   {
      async public Task<ConnectionCheckStatus> CheckConnection(string hostname, string token)
      {
         using (GitLabTaskRunner client = new GitLabTaskRunner(hostname, token))
         {
            try
            {
               await CommonOperator.SearchCurrentUserAsync(client);
               return ConnectionCheckStatus.OK;
            }
            catch (OperatorException ox)
            {
               if (ox.InnerException is GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     if (wx.Response is System.Net.HttpWebResponse response
                      && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                        return ConnectionCheckStatus.BadAccessToken;
                     }
                  }
               }
            }
            return ConnectionCheckStatus.BadHostname;
         }
      }

      public void OnConnectionLost(string hostname)
      {
         ConnectionLost?.Invoke(hostname);
      }

      public void OnConnectionRestored(string hostname)
      {
         ConnectionLost?.Invoke(hostname);
      }

      public event Action<string> ConnectionLost;
      public event Action<string> ConnectionRestored;
   }
}

