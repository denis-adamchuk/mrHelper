using GitLabSharp.Entities;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Common
{
   public interface IWorkflowEventListener
   {
      void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoader,
         ILoader<IVersionLoaderListener> versionLoader);
      void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade);
   }
}

