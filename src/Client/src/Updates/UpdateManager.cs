using System;
using mrHelper.Client.Tools

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
      }

      public WorkflowUpdateChecker GetWorkflowUpdateChecker(Workflow workflow)
      {
         return new WorkflowUpdateChecker(settings, UpdateOperator);
      }

      public CommitChecker GetCommitChecker(MergeRequestDescriptor mrd)
      {
         return new CommitChecker(mrd, UpdateOperator);
      }

      private UpdateOperator UpdateOperator { get; }
      private WorkflowUpdateChecker WorkflowUpdateChecker { get; }
   }
}

