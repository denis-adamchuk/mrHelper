using System;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;

namespace mrHelper.Client.Repository
{
   /// <summary>
   /// Implements Repository-related interaction with GitLab
   /// </summary>
   internal class RepositoryOperator
   {
      internal RepositoryOperator(string host, string token)
      {
         _client = new GitLabClient(host, token);
      }

      internal Task<Comparison> CompareAsync(string projectname, string from, string to)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Comparison)(await _client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).Repository.CompareAsync(new CompareParameters(from, to)))));
      }

      internal Task<File> LoadFileAsync(string projectname, string filename, string sha)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (File)(await _client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).Repository.Files. Get(filename).LoadTaskAsync(sha))));
      }

      internal Task<Commit> LoadCommitAsync(string projectname, string sha)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Commit)(await _client.RunAsync(
                  async (gitlab) =>
                     await gitlab.Projects.Get(projectname).Repository.Commits.Get(sha).LoadTaskAsync())));
      }

      internal Task<Branch> CreateNewBranchAsync(string projectname, string name, string sha)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Branch)(await _client.RunAsync(async (gl) =>
                  await gl.Projects.Get(projectname).Repository.Branches.CreateNewTaskAsync(
                     new CreateNewBranchParameters(name, sha)))));
      }

      internal Task DeleteBranchAsync(string projectname, string name)
      {
         return OperatorCallWrapper.Call(
            async () =>
               await _client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).Repository.Branches.Get(name).DeleteTaskAsync()));
      }

      async internal Task CancelAsync()
      {
         await _client.CancelAsync();
      }

      private readonly GitLabClient _client;
   }
}

