using mrHelper.Client.Common;

namespace mrHelper.Client.Workflow
{
   internal static class MergeRequestListLoaderFactory
   {
      internal static IMergeRequestListLoader CreateMergeRequestListLoader(
         GitLabClientContext clientContext, WorkflowDataOperator op,
         IWorkflowContext context, IVersionLoader versionLoader)
      {
         IMergeRequestListLoader listLoader = null;
         if (context is ProjectBasedContext)
         {
            listLoader = new ProjectBasedMergeRequestLoader(clientContext, op, versionLoader);
         }
         else if (context is LabelBasedContext)
         {
            listLoader = new LabelBasedMergeRequestLoader(clientContext, op, versionLoader);
         }
         return listLoader;
      }
   }
}

