using System;
using System.Collections.Generic;

namespace mrHelper.Client
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

