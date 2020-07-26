using mrHelper.Client.Common;

namespace mrHelper.Client.Session
{
   internal class SessionManager : ISessionManager
   {
      public SessionManager(GitLabClientContext clientContext, IModificationNotifier modificationNotifier)
      {
         _clientContext = clientContext;
         _modificationNotifier = modificationNotifier;
      }

      public ISession CreateSession()
      {
         return new Session(_clientContext, _modificationNotifier);
      }

      private readonly GitLabClientContext _clientContext;
      private readonly IModificationNotifier _modificationNotifier;
   }
}

