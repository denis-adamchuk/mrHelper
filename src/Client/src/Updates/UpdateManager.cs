using System;
using System.ComponentModel;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Manages updates
   /// </summary>
   public class UpdateManager
   {
      public UpdateManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings)
      {
         UpdateOperator updateOperator = new UpdateOperator(settings);
         WorkflowUpdateChecker = new WorkflowUpdateChecker(settings, updateOperator, workflow, synchronizeInvoke);
      }

      public WorkflowUpdateChecker GetWorkflowUpdateChecker()
      {
         return WorkflowUpdateChecker;
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return WorkflowUpdateChecker;
      }

      public CommitChecker GetCommitChecker(int mergeRequestId)
      {
         return new CommitChecker(mergeRequestId, getDetailsCache());
      }

      private IWorkflowDetailsCache getDetailsCache()
      {
         return WorkflowUpdateChecker;
      }

      private WorkflowUpdateChecker WorkflowUpdateChecker { get; }
   }
}

