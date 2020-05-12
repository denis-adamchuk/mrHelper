using mrHelper.Client.Common;

namespace mrHelper.Client.Session
{
   internal class SessionManager : ISessionManager
   {
      public SessionManager(GitLabClientContext clientContext)
      {
         _clientContext = clientContext;
      }

      public ISession CreateSession()
      {
         return new Session(_clientContext);
      }

      private readonly GitLabClientContext _clientContext;
   }
}

