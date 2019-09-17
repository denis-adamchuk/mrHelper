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
      public WorkflowFactory(UserDefinedSettings settings, PersistentStorage persistentStorage)
      {
         Settings = settings;
         PersistentStorage = persistentStorage;
      }

      public Workflow CreateWorkflow()
      {
         return new Workflow(Settings, PersistentStorage);
      }

      private readonly UserDefinedSettings Settings;
      private readonly PersistentStorage PersistentStorage;
   }
}

