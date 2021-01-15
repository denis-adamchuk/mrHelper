using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class UserOperator : BaseOperator
   {
      internal UserOperator(string host, IHostProperties settings,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(host, settings, networkOperationStatusListener)
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

      async internal Task<User> SearchUserByNameAsync(string name)
      {
         IEnumerable<User> users = await callWithSharedClient(
         async (client) =>
            await OperatorCallWrapper.Call(
               async () =>
                  (IEnumerable<User>)await client.RunAsync(
                     async (gl) =>
                        await  gl.Users.SearchTaskAsync(name))));
         if (!users.Any())
         {
            return null;
         }
         return users.First();
      }

      async internal Task<User> SearchUserByUsernameAsync(string username)
      {
         User user = GlobalCache.GetUser(Hostname, username);
         if (user == null)
         {
            var users = await callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<User>)await client.RunAsync(
                        async (gl) =>
                           await gl.Users.SearchByUsernameTaskAsync(username))));
            if (!users.Any())
            {
               return null;
            }
            user = users.First();
            GlobalCache.AddUser(Hostname, user);
         }
         return user;
      }
   }
}

