using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   internal class ProjectOperator : BaseOperator
   {
      internal ProjectOperator(string hostname, IHostProperties hostProperties)
         : base(hostname, hostProperties)
      {
      }

      internal Task<IEnumerable<Project>> GetProjects()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Project>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.LoadAllTaskAsync(new GitLabSharp.Accessors.ProjectsFilter(false, true)))));
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
   }
}

