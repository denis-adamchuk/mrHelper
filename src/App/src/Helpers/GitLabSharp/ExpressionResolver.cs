using System;
using System.Collections.Generic;
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
         if (_currentUser == null)
         {
            return expression;
         }

         if (_cached.TryGetValue(expression, out string value))
         {
            return value;
         }

         value = expression.Replace("%CurrentUsername%", _currentUser.Username);
         _cached[expression] = value;
         return value;
      }

      private void onSessionStarted(string hostname, User user)
      {
         _currentUser = user;
         _cached.Clear();
      }

      private User _currentUser;
      private readonly ISession _session;
      private Dictionary<string, string> _cached = new Dictionary<string, string>();
   }
}

