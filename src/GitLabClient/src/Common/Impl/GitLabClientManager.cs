using mrHelper.Client.Session;
using mrHelper.Client.Repository;
using GitLabSharp;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         SessionManager = new SessionManager(clientContext);
         SearchManager = new SearchManager(clientContext.HostProperties);
         RepositoryManager = new RepositoryManager(clientContext.HostProperties);
      }

      public enum EConnectionCheckStatus
      {
         OK,
         BadHostname,
         BadAccessToken
      }

      async public Task<EConnectionCheckStatus> VerifyConnection(string host, string token)
      {
         GitLabClient client = new GitLabClient(host, token);
         try
         {
            await CommonOperator.SearchCurrentUserAsync(client);
            return EConnectionCheckStatus.OK;
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
                     return EConnectionCheckStatus.BadAccessToken;
                  }
               }
            }
         }
         return EConnectionCheckStatus.BadHostname;
      }

      public ISessionManager SessionManager { get; }
      public ISearchManager SearchManager { get; }
      public IRepositoryManager RepositoryManager { get; }
   }
}

