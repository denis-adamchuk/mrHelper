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
      public WorkflowFactory(UserDefinedSettings settings, PersistenceManager persistenceManager)
      {
         Settings = settings;
         PersistenceManager = persistenceManager;
      }

      public Workflow CreateWorkflow()
      {
         return new Workflow(Settings, PersistenceManager);
      }

      private readonly UserDefinedSettings Settings;
      private readonly PersistenceManager PersistenceManager;
   }
}

