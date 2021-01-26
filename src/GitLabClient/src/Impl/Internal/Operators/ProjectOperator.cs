using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class ProjectOperator : BaseOperator
   {
      internal ProjectOperator(string hostname, IHostProperties hostProperties,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(hostname, hostProperties, networkOperationStatusListener)
      {
      }

      internal Task<Project> SearchProjectAsync(string projectname)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Project)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectname).LoadTaskAsync())));
      }

      internal Task<IEnumerable<User>> GetUsersAsync(string projectname)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<User>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectname).Users.LoadAllTaskAsync())));
      }
   }
}

