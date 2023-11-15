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
   internal class EnvironmentStatusLoader : BaseDataCacheLoader, IEnvironmentStatusLoader
   {
      internal EnvironmentStatusLoader(DataCacheOperator op,
         InternalCacheUpdater cacheUpdater, DataCacheCallbacks callbacks)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _callbacks = callbacks;
      }

      async public Task LoadEnvironmentStatus(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         Exception exception = null;
         async Task loadEnvironmentStatusLocal(MergeRequestKey mrk)
         {
            bool isSupported = _callbacks?.IsEnvironmentStatusSupported?.Invoke(mrk.ProjectKey) ?? false;
            if (exception != null || !isSupported)
            {
               return;
            }

            try
            {
               await loadEnvironmentStatusAsync(mrk);
            }
            catch (BaseLoaderException ex)
            {
               // This is ok if Environment Status page is not found
               System.Net.HttpWebResponse response = ex.GetWebResponse();
               if (response != null && response.StatusCode != System.Net.HttpStatusCode.NotFound)
               {
                  exception = ex;
               }
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(mergeRequestKeys, x => loadEnvironmentStatusLocal(x),
            () => Constants.EnvironmentStatusLoaderMergeRequestBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
      }

      async private Task loadEnvironmentStatusAsync(MergeRequestKey mrk)
      {
         IEnumerable<EnvironmentStatus> status = await call(
            () => _operator.GetEnvironmentStatusAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading environment status for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load environment status for merge request with IId {0}", mrk.IId));
         _cacheUpdater.UpdateEnvironmentStatus(mrk, status);
      }

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheCallbacks _callbacks;
   }
}

