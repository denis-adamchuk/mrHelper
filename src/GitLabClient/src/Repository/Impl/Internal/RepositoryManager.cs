using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;

namespace mrHelper.Client.Repository
{
   internal class RepositoryManager : IRepositoryManager
   {
      internal RepositoryManager(IHostProperties settings)
      {
         _settings = settings;
      }

      async public Task<Comparison?> Compare(ProjectKey projectKey, string from, string to)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.CompareAsync(projectKey.ProjectName, from, to);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot perform comparison", ex);
         }
      }

      async public Task<File?> LoadFile(ProjectKey projectKey, string filename, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.LoadFileAsync(projectKey.ProjectName, filename, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot load file", ex);
         }
      }

      async public Task<Commit?> LoadCommit(ProjectKey projectKey, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.LoadCommitAsync(projectKey.ProjectName, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot load commit", ex);
         }
      }

      async public Task<Branch?> CreateNewBranch(ProjectKey projectKey, string name, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.CreateNewBranchAsync(projectKey.ProjectName, name, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot create a new branch", ex);
         }
      }

      async public Task DeleteBranch(ProjectKey projectKey, string name)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            await _operator.DeleteBranchAsync(projectKey.ProjectName, name);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return;
            }
            throw new RepositoryManagerException("Cannot delete a branch", ex);
         }
      }

      async public Task Cancel()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      private readonly IHostProperties _settings;
      private RepositoryOperator _operator;
   }
}

