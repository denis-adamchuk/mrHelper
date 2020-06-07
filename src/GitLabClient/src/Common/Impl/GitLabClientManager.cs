using System;
using System.Threading.Tasks;
using mrHelper.Client.Session;
using mrHelper.Client.Repository;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager : IDisposable
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         SessionManager = new SessionManager(clientContext);
         SearchManager = new SearchManager(clientContext.HostProperties);
         _repositoryManager = new RepositoryManager(clientContext.HostProperties);
      }

      public enum EConnectionCheckStatus
      {
         OK,
         BadHostname,
         BadAccessToken
      }

      async public Task<EConnectionCheckStatus> VerifyConnection(string host, string token)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(host, token);
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

      public void Dispose()
      {
         _repositoryManager.Dispose();
      }

      public ISessionManager SessionManager { get; }
      public ISearchManager SearchManager { get; }
      public IRepositoryManager RepositoryManager => _repositoryManager;

      private readonly RepositoryManager _repositoryManager;
   }
}

