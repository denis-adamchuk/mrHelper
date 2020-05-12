using System.Collections.Generic;
using mrHelper.Client.Session;
using mrHelper.Client.Repository;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         _workflowManager = new SessionManager(clientContext);
         _searchManager = new SearchManager(clientContext.HostProperties);
         _repositoryManager = new RepositoryManager(clientContext.HostProperties);
      }

      ISessionManager WorkflowManager => _workflowManager;
      ISearchManager SearchManager => _searchManager;
      IRepositoryManager RepositoryManager => _repositoryManager;

      private readonly ISessionManager _workflowManager;
      private readonly ISearchManager _searchManager;
      private readonly IRepositoryManager _repositoryManager;
   }
}

