using mrHelper.Client.Session;
using mrHelper.Client.Repository;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         SessionManager = new SessionManager(clientContext);
         SearchManager = new SearchManager(clientContext.HostProperties);
         RepositoryManager = new RepositoryManager(clientContext.HostProperties);
      }

      public ISessionManager SessionManager { get; }
      public ISearchManager SearchManager { get; }
      public IRepositoryManager RepositoryManager { get; }
   }
}

