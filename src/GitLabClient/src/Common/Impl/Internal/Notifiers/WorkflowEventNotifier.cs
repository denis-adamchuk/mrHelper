using GitLabSharp.Entities;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Common
{
   internal class WorkflowEventNotifier : BaseNotifier<IWorkflowEventListener>, IWorkflowEventListener
   {
      public void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoader,
         ILoader<IVersionLoaderListener> versionLoader) =>
         notifyAll(x => x.PreLoadWorkflow(hostname, mergeRequestListLoader, versionLoader));

      public void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade) =>
         notifyAll(x => x.PostLoadWorkflow(hostname, user, context, facade));
   }
}

