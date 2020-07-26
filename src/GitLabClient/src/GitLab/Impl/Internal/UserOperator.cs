using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class UserOperator : BaseOperator
   {
      internal UserOperator(string host, IHostProperties settings)
         : base(host, settings)
      {
      }

      internal Task<User> SearchCurrentUserAsync()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (User)await client.RunAsync(
                        async (gl) =>
                           await gl.CurrentUser.LoadTaskAsync())));
      }

      internal Task<IEnumerable<User>> SearchUserAsync(string name, bool isUsername)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<User>)await client.RunAsync(
                        async (gl) =>
                           await (isUsername ? gl.Users.SearchByUsernameTaskAsync(name) : gl.Users.SearchTaskAsync(name)))));
      }
   }
}

