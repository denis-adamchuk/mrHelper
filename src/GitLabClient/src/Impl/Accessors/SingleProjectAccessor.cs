using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class SingleProjectAccessor
   {
      internal SingleProjectAccessor(ProjectKey projectKey, IHostProperties settings,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _projectKey = projectKey;
         _settings = settings;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<IEnumerable<User>> GetUsersAsync()
      {
         using (ProjectOperator projectOperator = new ProjectOperator(
            _projectKey.HostName, _settings, _networkOperationStatusListener))
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
         new RepositoryAccessor(_settings, _projectKey, _networkOperationStatusListener);

      public MergeRequestAccessor MergeRequestAccessor =>
         new MergeRequestAccessor(_settings, _projectKey, _modificationListener, _networkOperationStatusListener);

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _settings;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

