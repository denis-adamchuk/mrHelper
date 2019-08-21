using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   /// <summary>
   /// Manages Client workflows
   /// </summary>
   public class WorkflowManager : IDisposable
   {
      public WorkflowManager(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      public void Dispose()
      {
         foreach (Workflow w in Workflows)
         {
            w.Dispose();
         }
      }

      public Workflow CreateWorkflow()
      {
         Workflow w = new Workflow(Settings);
         Workflows.Add(w);
         return w;
      }

      private readonly UserDefinedSettings Settings;
      private readonly List<Workflow> Workflows = new List<Workflow>();
   }
}

