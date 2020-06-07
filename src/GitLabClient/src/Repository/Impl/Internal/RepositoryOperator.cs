using System.Threading.Tasks;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Repository
{
   /// <summary>
   /// Implements Repository-related interaction with GitLab
   /// </summary>
   internal class RepositoryOperator : BaseOperator
   {
      internal RepositoryOperator(string hostname, IHostProperties settings)
         : base(hostname, settings)
      {
      }

      internal Task<Comparison> CompareAsync(string projectname, string from, string to)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Comparison)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectname).Repository.CompareAsync(new CompareParameters(from, to)))));
      }

      internal Task<File> LoadFileAsync(string projectname, string filename, string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (File)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectname).Repository.Files. Get(filename).LoadTaskAsync(sha)))));
      }

      internal Task<Commit> LoadCommitAsync(string projectname, string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Commit)(await client.RunAsync(
                        async (gitlab) =>
                           await gitlab.Projects.Get(projectname).Repository.Commits.Get(sha).LoadTaskAsync()))));
      }

      internal Task<Branch> CreateNewBranchAsync(string projectname, string name, string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Branch)(await client.RunAsync(async (gl) =>
                        await gl.Projects.Get(projectname).Repository.Branches.CreateNewTaskAsync(
                           new CreateNewBranchParameters(name, sha))))));
      }

      internal Task DeleteBranchAsync(string projectname, string name)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectname).Repository.Branches.Get(name).DeleteTaskAsync())));
      }
   }
}

