using System;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Manages updates
   /// </summary>
   public class UpdateManager
   {
      public UpdateManager(UserDefinedSettings settings)
      {
         UpdateOperator UpdateOperator = new UpdateOperator(settings);
         Settings = settings;
      }

      public WorkflowUpdateChecker GetWorkflowUpdateChecker(Workflow.Workflow workflow)
      {
         return new WorkflowUpdateChecker(Settings, UpdateOperator, workflow);
      }

      public CommitChecker GetCommitChecker(MergeRequestDescriptor mrd)
      {
         return new CommitChecker(mrd, UpdateOperator);
      }

      private UpdateOperator UpdateOperator { get; }
      private UserDefinedSettings Settings { get; }
   }
}

