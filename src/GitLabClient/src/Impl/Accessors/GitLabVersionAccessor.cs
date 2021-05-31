using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class GitLabVersionAccessor
   {
      internal GitLabVersionAccessor(string hostname, IHostProperties hostProperties,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<GitLabVersion> GetGitLabVersionAsync()
      {
         using (GitLabVersionOperator op = new GitLabVersionOperator(
            _hostname, _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               return await op.GetGitLabVersionAsync();
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

