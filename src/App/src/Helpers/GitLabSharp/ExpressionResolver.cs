using GitLabSharp.Entities;
using mrHelper.Client.Workflow;
using System;

namespace mrHelper.App.Helpers
{
   public class ExpressionResolver : IDisposable
   {
      public ExpressionResolver(Workflow workflow)
      {
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;

         _workflow = workflow;
         _workflow.PostLoadCurrentUser += onPostLoadCurrentUser;
      }

      public void Dispose()
      {
         _workflow.PostLoadCurrentUser -= onPostLoadCurrentUser;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", _currentUser.Username);
      }

      private void onPostLoadCurrentUser(User user)
      {
         _currentUser = user;
      }

      private User _currentUser;
      private readonly Workflow _workflow;
   }
}

