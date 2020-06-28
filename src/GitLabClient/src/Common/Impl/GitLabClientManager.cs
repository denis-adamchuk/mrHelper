using System;
using System.Threading.Tasks;
using mrHelper.Client.Session;
using mrHelper.Client.Repository;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         SessionManager = new SessionManager(clientContext);
         SearchManager = new SearchManager(clientContext.HostProperties);
      }

      public enum ConnectionCheckStatus
      {
         OK,
         BadHostname,
         BadAccessToken
      }

      async public Task<ConnectionCheckStatus> VerifyConnection(string host, string token)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(host, token);
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
                  System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
                  if (response != null && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                  {
                     return ConnectionCheckStatus.BadAccessToken;
                  }
               }
            }
         }
         return ConnectionCheckStatus.BadHostname;
      }

      public ISessionManager SessionManager { get; }
      public ISearchManager SearchManager { get; }
   }
}

