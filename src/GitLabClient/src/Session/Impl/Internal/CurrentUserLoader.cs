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

      async public Task<User> Load(string hostName)
      {
         try
         {
            return await _operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host \"{0}\"", hostName);
            string errorMessage = String.Format("Cannot load user from host \"{0}\"", hostName);
            handleOperatorException(ex, cancelMessage, errorMessage);
         }
         return null;
      }
   }
}

