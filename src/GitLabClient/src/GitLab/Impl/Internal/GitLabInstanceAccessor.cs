using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Projects;
using mrHelper.Common.Interfaces;
using System.Threading.Tasks;

namespace mrHelper.Client.Common
{
   internal class GitLabInstanceAccessor : IGitLabInstanceAccessor
   {
      public GitLabInstanceAccessor(string hostname, IHostProperties hostProperties,
         ModificationNotifier modificationNotifier)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
         _modificationNotifier = modificationNotifier;
      }

      public IProjectAccessor ProjectAccessor =>
         new Projects.ProjectAccessor(_hostProperties, _hostname, _modificationNotifier);

      public IUserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties);

      async public Task<ConnectionCheckStatus> VerifyConnection(string token)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(_hostname, token);
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

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly ModificationNotifier _modificationNotifier;
   }
}
