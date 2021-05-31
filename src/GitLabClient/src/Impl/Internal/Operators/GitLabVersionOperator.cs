using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class GitLabVersionOperator : BaseOperator
   {
      internal GitLabVersionOperator(string host, IHostProperties settings,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(host, settings, networkOperationStatusListener)
      {
      }

      async internal Task<GitLabVersion> GetGitLabVersionAsync()
      {
         GitLabVersion version = GlobalCache.GetGitLabVersion(Hostname);
         if (version == null)
         {
            version = await callWithSharedClient(
               async (client) =>
                  await OperatorCallWrapper.Call(
                     async () =>
                        (GitLabVersion)await client.RunAsync(
                           async (gl) =>
                              await gl.Version.LoadTaskAsync())));
            if (version != null)
            {
               GlobalCache.SetGitLabVersion(Hostname, version);
            }
         }
         return version;
      }
   }
}

