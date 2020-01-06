using GitLabSharp.Entities;
using mrHelper.Client.Workflow;

namespace mrHelper.App.Helpers
{
   public class ExpressionResolver
   {
      public ExpressionResolver(Workflow workflow)
      {
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", _currentUser.Username);
      }

      private User _currentUser;
   }
}

