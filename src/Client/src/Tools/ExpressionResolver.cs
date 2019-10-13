using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Tools
{
   public class ExpressionResolver
   {
      public ExpressionResolver(Workflow.Workflow workflow)
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

