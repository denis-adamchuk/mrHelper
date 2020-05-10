using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Workflow
{
   internal class MergeRequestLoader : BaseWorkflowLoader, IMergeRequestLoader
   {
      internal MergeRequestLoader(WorkflowDataOperator op, IVersionLoader versionLoader)
         : base(op)
      {
         _versionLoader = versionLoader;
         _operator = op;
      }

      public INotifier<IMergeRequestLoaderListener> GetNotifier() => _notifier;

      async public Task<bool> LoadMergeRequest(MergeRequestKey mrk, EComparableEntityType comparableEntityType)
      {
         return await loadMergeRequestAsync(mrk, comparableEntityType);
      }

      async private Task<bool> loadMergeRequestAsync(MergeRequestKey mrk, EComparableEntityType comparableEntityType)
      {
         _notifier.OnPreLoadMergeRequest(mrk);

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            SearchByIId searchByIId = new SearchByIId { ProjectName = mrk.ProjectKey.ProjectName, IId = mrk.IId };
            IEnumerable<MergeRequest> mergeRequests = await _operator.SearchMergeRequestsAsync(searchByIId, null, true);
            mergeRequest = mergeRequests.FirstOrDefault();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading MR with IId {0}", mrk.IId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage,
               new Action[] { new Action(() => _notifier.OnFailedLoadMergeRequest(mrk)) });
            return false;
         }

         _notifier.OnPostLoadMergeRequest(mrk, mergeRequest);

         switch (comparableEntityType)
         {
            case EComparableEntityType.None:
               return true;

            case EComparableEntityType.Commit:
               return await _versionLoader.LoadVersionsAsync(mrk, false)
                   && await _versionLoader.LoadCommitsAsync(mrk);

            case EComparableEntityType.Version:
               return await _versionLoader.LoadVersionsAsync(mrk, true);
         }

         Debug.Assert(false);
         return true;
      }

      private readonly IVersionLoader _versionLoader;
      private readonly MergeRequestLoaderNotifier _notifier = new MergeRequestLoaderNotifier();
   }
}

