using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Client.Tools;
using mrHelper.Client.Persistence;

namespace mrHelper.Client.Workflow
{
   /// <summary>
   /// Creates workflows
   /// </summary>
   public class WorkflowFactory
   {
      public WorkflowFactory(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      public Workflow CreateWorkflow()
      {
         return new Workflow(Settings);
      }

      private readonly UserDefinedSettings Settings;
   }
}

