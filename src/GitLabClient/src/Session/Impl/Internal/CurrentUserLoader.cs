using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;

namespace mrHelper.Client.Session
{
   internal class CurrentUserLoader : BaseSessionLoader
   {
      internal CurrentUserLoader(SessionOperator op)
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

