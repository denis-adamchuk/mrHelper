using System;
using System.Collections.Generic;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : Exception {}

   /// <summary>
   /// Manages Client workflows
   /// </summary>
   public class WorkflowManager : IDisposable
   {
      public WorkflowManager(UserDefinedSettings settings)
      {
      }

      async public Task<Workflow> CreateWorkflow()
      {
         return new Workflow(settings);
      }
   }
}

