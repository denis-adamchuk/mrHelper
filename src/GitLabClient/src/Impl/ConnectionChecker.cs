using System;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.GitLabClient
{
   public enum ConnectionCheckStatus
   {
      OK,
      BadHostname,
      BadAccessToken
   }

   public static class ConnectionChecker
   {
      async static public Task<ConnectionCheckStatus> CheckConnectionAsync(string hostname, string token)
      {
         using (GitLabTaskRunner client = new GitLabTaskRunner(hostname, token))
         {
            try
            {
               await client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync());
               return ConnectionCheckStatus.OK;
            }
            catch (Exception ex)
            {
               if (ex.InnerException is GitLabRequestException rx)
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
   }
}

