using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Workflow
{
   public interface IWorkflowEventNotifier
   {
      event Action<string, User> Connecting;
      event Action<string, IEnumerable<Project>> Connected;
   }
}

