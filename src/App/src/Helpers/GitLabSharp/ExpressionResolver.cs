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
         _workflowEventNotifier.Connecting += onConnecting;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connecting -= onConnecting;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", _currentUser.Username);
      }

      private void onConnecting(string hostname, User user)
      {
         _currentUser = user;
      }

      private User _currentUser;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

