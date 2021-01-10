using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public class SingleProjectAccessor
   {
      internal SingleProjectAccessor(ProjectKey projectKey, IHostProperties settings,
         IModificationListener modificationListener, IConnectionLossListener connectionLossListener)
      {
         _projectKey = projectKey;
         _settings = settings;
         _modificationListener = modificationListener;
         _connectionLossListener = connectionLossListener;
      }

      async public Task<IEnumerable<User>> GetUsersAsync()
      {
         using (ProjectOperator projectOperator = new ProjectOperator(
            _projectKey.HostName, _settings, _connectionLossListener))
         {
            try
            {
               return await projectOperator.GetUsersAsync(_projectKey.ProjectName);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      public RepositoryAccessor GetRepositoryAccessor() =>
         new RepositoryAccessor(_settings, _projectKey, _connectionLossListener);

      public MergeRequestAccessor MergeRequestAccessor =>
         new MergeRequestAccessor(_settings, _projectKey, _modificationListener, _connectionLossListener);

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _settings;
      private readonly IModificationListener _modificationListener;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

