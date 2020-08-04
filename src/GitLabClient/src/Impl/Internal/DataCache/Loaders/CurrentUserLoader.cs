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

      public Task<User> Load(string hostName)
      {
         return call(() => _operator.GetCurrentUserAsync(),
            String.Format("Cancelled loading current user from host \"{0}\"", hostName),
            String.Format("Cannot load user from host \"{0}\"", hostName));
      }
   }
}

