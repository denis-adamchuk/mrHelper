using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   /// <summary>
   /// Implements Repository-related interaction with GitLab
   /// </summary>
   internal class RepositoryOperator : BaseOperator
   {
      internal RepositoryOperator(ProjectKey projectKey, IHostProperties settings)
         : base(projectKey.HostName, settings)
      {
         _projectname = projectKey.ProjectName;
      }

      internal Task<Comparison> CompareAsync(string from, string to)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Comparison)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(_projectname).Repository.CompareAsync(new CompareParameters(from, to)))));
      }

      internal Task<File> LoadFileAsync(string filename, string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (File)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(_projectname).Repository.Files. Get(filename).LoadTaskAsync(sha)))));
      }

      internal Task<Commit> LoadCommitAsync(string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Commit)(await client.RunAsync(
                        async (gitlab) =>
                           await gitlab.Projects.Get(_projectname).Repository.Commits.Get(sha).LoadTaskAsync()))));
      }

      internal Task<IEnumerable<CommitRef>> LoadCommitRefsAsync(string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<CommitRef>)(await client.RunAsync(
                        async (gitlab) =>
                           await gitlab.Projects.Get(_projectname).Repository.Commits.Get(sha).LoadRefsTaskAsync()))));
      }

      internal Task<IEnumerable<Branch>> GetBranches(string search)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Branch>)(await client.RunAsync(async (gl) =>
                        await gl.Projects.Get(_projectname).Repository.Branches.LoadAllTaskAsync(search)))));
      }

      internal Task<Branch> CreateNewBranchAsync(string name, string sha)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Branch)(await client.RunAsync(async (gl) =>
                        await gl.Projects.Get(_projectname).Repository.Branches.CreateNewTaskAsync(
                           new CreateNewBranchParameters(name, sha))))));
      }

      internal Task DeleteBranchAsync(string name)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(_projectname).Repository.Branches.Get(name).DeleteTaskAsync())));
      }

      private readonly string _projectname;
   }
}

