using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class ApprovalLoader : BaseDataCacheLoader, IApprovalLoader
   {
      internal ApprovalLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
      }

      async public Task LoadApprovals(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         Exception exception = null;
         async Task loadApprovalsLocal(MergeRequestKey mrk)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               await loadApprovalsAsync(mrk);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(mergeRequestKeys, x => loadApprovalsLocal(x),
            () => Constants.ApprovalLoaderMergeRequestBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
      }

      async private Task loadApprovalsAsync(MergeRequestKey mrk)
      {
         MergeRequestApprovalConfiguration approvals = await call(
            () => _operator.GetApprovalsAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading approvals for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load approvals for merge request with IId {0}", mrk.IId));
         _cacheUpdater.UpdateApprovals(mrk, approvals);
      }

      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

