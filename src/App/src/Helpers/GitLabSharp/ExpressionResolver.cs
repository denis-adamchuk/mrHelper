using System;
using GitLabSharp.Entities;
using mrHelper.Client.Session;

namespace mrHelper.App.Helpers
{
   public class ExpressionResolver : IDisposable
   {
      public ExpressionResolver(ISession session)
      {
         _session = session;
         _session.Started += onSessionStarted;
      }

      public void Dispose()
      {
         _session.Started -= onSessionStarted;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", _currentUser.Username);
      }

      private void onSessionStarted(string hostname, User user, SessionContext sessionContext)
      {
         _currentUser = user;
      }

      private User _currentUser;
      private readonly ISession _session;
   }
}

