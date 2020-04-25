using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.Client.Workflow
{
   public interface IWorkflowEventNotifier
   {
      event Action<string> Connecting;
      event Action<string, User, IEnumerable<Project>> Connected;
   }
}

