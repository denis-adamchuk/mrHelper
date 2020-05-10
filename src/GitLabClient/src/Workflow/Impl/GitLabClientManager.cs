using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;
using mrHelper.Client.TimeTracking;

namespace mrHelper.Client.Workflow
{
   public class GitLabClientManager : IWorkflowLoader
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         _clientContext = clientContext;
         _searchManager = new SearchManager(clientContext.HostProperties);
         _repositoryManager = new RepositoryManager(clientContext.HostProperties);
      }

      ISearchManager SearchManager => _searchManager;
      IRepositoryManager RepositoryManager => _repositoryManager;

      public INotifier<IWorkflowEventListener> GetNotifier()
      {
         return _workflowEventNotifier;
      }

      async public Task<bool> Load(string hostname, IWorkflowContext context)
      {
         await DisposeAsync();

         _operator = new WorkflowDataOperator(hostname, _clientContext.HostProperties.GetAccessToken(hostname));

         User? currentUser = await loadCurrentUserAsync(hostname);
         if (!currentUser.HasValue)
         {
            return false;
         }

         IVersionLoader versionLoader = new VersionLoader(_operator);
         IMergeRequestListLoader mergeRequestListLoader =
            MergeRequestListLoaderFactory.CreateMergeRequestListLoader(_clientContext,
               _operator, context, versionLoader);

         _workflowEventNotifier.PreLoadWorkflow(hostname, mergeRequestListLoader, versionLoader);

         if (await mergeRequestListLoader.Load(context))
         {
            _facade = buildFacade(versionLoader);
            _workflowEventNotifier.PostLoadWorkflow(hostname, currentUser.Value, context, _facade);
            return true;
         }

         return false;
      }

      async public Task DisposeAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
            _operator = null;
         }

         if (_facade != null)
         {
            _facade.Dispose();
            _facade = null;
         }
      }

      async private Task<User?> loadCurrentUserAsync(string hostName)
      {
         try
         {
            return await _operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host \"{0}\"", hostName);
            string errorMessage = String.Format("Cannot load user from host \"{0}\"", hostName);

            bool cancelled = ex.InnerException is GitLabSharp.GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[WorkflowManager] {0}", cancelMessage));
               return null;
            }

            throw new WorkflowException(errorMessage, ex);
         }
      }

      private GitLabFacade buildFacade(IVersionLoader versionLoader)
      {
         return new GitLabFacade(
            new MergeRequestLoader(_operator, versionLoader),
            new MergeRequestManager(_clientContext, this),
            new DiscussionManager(_clientContext, this),
            new TimeTrackingManager(_clientContext, this));
      }

      private WorkflowDataOperator _operator;
      private GitLabFacade _facade;

      private readonly GitLabClientContext _clientContext;
      private readonly WorkflowEventNotifier _workflowEventNotifier = new WorkflowEventNotifier();
      private readonly ISearchManager _searchManager;
      private readonly IRepositoryManager _repositoryManager;
   }
}

