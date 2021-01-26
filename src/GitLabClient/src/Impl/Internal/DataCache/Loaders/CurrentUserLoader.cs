using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class CurrentUserLoader : BaseDataCacheLoader
   {
      internal CurrentUserLoader(DataCacheOperator op)
         : base(op)
      {
      }

      async public Task Load(string hostName, string accessToken)
      {
         if (GlobalCache.GetAuthenticatedUser(hostName, accessToken) == null)
         {
            User user = await call(() => _operator.SearchCurrentUserAsync(),
               String.Format("Cancelled loading current user from host \"{0}\"", hostName),
               String.Format("Cannot load user from host \"{0}\"", hostName));
            GlobalCache.AddAuthenticatedUser(hostName, accessToken, user);
         }
      }
   }
}

