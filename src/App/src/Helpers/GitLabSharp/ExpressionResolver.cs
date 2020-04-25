using GitLabSharp.Entities;
using mrHelper.Client.Workflow;
using System;
using System.Collections.Generic;

namespace mrHelper.App.Helpers
{
   public class ExpressionResolver : IDisposable
   {
      public ExpressionResolver(IWorkflowEventNotifier workflowEventNotifier)
      {
         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", _currentUser.Username);
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         _currentUser = user;
      }

      private User _currentUser;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

