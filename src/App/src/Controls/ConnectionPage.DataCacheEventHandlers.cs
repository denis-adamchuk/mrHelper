using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      private void onGitStatisticManagerUpdate()
      {
         getListView(EDataCacheType.Live).Invalidate();
      }

      private void onPreLoadTrackedTime(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         onTrackedTimeManagerEvent(totalTimeCache, mrk);
      }

      private void onPostLoadTrackedTime(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         onTrackedTimeManagerEvent(totalTimeCache, mrk);
      }

      private void onTrackedTimeManagerEvent(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         MergeRequestKey? currentMergeRequestKey = getMergeRequestKey(null);
         if (currentMergeRequestKey.HasValue && currentMergeRequestKey.Value.Equals(mrk))
         {
            foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
            {
               if (getDataCache(mode)?.TotalTimeCache == totalTimeCache)
               {
                  // This is helpful
                  // - when discussions parsed (asynchronously) and tracked time value is calculated
                  // - when time edited by ConnectionPage.EditTime()
                  CanTrackTimeChanged?.Invoke(this);
                  break;
               }
            }
         }

         // Update total time column in the table
         getListView(EDataCacheType.Live).Invalidate();
      }

      private void onPreLoadDiscussions(MergeRequestKey mrk)
      {
         onDiscussionManagerEvent();
      }

      private void onPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         onDiscussionManagerEvent();

         MergeRequestKey? currentMRK = getMergeRequestKey(null);
         MergeRequest currentMR = getMergeRequest(null);
         if (currentMRK.HasValue && currentMR != null)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey(currentMRK.Value.ProjectKey, currentMR);
            if (mrk.Equals(fmk))
            {
               descriptionSplitContainerSite.UpdateData(fmk, getDataCache(getCurrentTabDataCacheType()));
            }
         }
      }

      private void onDiscussionManagerEvent()
      {
         // Update Discussions column in the table
         getListView(EDataCacheType.Live).Invalidate();
      }

      private void onLiveMergeRequestEvent(UserEvents.MergeRequestEvent e) =>
         onMergeRequestEvent(e, EDataCacheType.Live);

      private void onRecentMergeRequestEvent(UserEvents.MergeRequestEvent e) =>
         onMergeRequestEvent(e, EDataCacheType.Recent);

      private void onSearchMergeRequestEvent(UserEvents.MergeRequestEvent e) =>
         onMergeRequestEvent(e, EDataCacheType.Search);

      private void notifyDiscussionForm(MergeRequestKey mrk)
      {
         bool doesMatchTag(object tag) => tag != null && ((MergeRequestKey)(tag)).Equals(mrk);
         Form formExisting = WinFormsHelpers.FindFormByTagAndName("DiscussionsForm", doesMatchTag);
         if (formExisting is DiscussionsForm existingDiscussionsForm)
         {
            existingDiscussionsForm.OnMergeRequestEvent();
         }
      }

      private void onMergeRequestEvent(UserEvents.MergeRequestEvent e, EDataCacheType type)
      {
         MergeRequestKey mrk = new MergeRequestKey(
            e.FullMergeRequestKey.ProjectKey, e.FullMergeRequestKey.MergeRequest.IId);

         if (e.AddedToCache || e.Commits)
         {
            requestCommitStorageUpdate(mrk.ProjectKey);
         }

         if (type == EDataCacheType.Live)
         {
            if (e.AddedToCache)
            {
               // some labels may appear within a small delay after new MR is detected
               requestUpdates(EDataCacheType.Live, mrk, new[] {
                  Program.Settings.OneShotUpdateOnNewMergeRequestFirstChanceDelayMs,
                  Program.Settings.OneShotUpdateOnNewMergeRequestSecondChanceDelayMs});
            }
            else if (e.RemovedFromCache)
            {
               if (CurrentUser != null && CurrentUser.Id == e.FullMergeRequestKey.MergeRequest.Author.Id)
               {
                  ensureMergeRequestInRecentDataCache(mrk);
               }
            }
            if (e.RemovedFromCache && isReviewedMergeRequest(mrk))
            {
               cleanupReviewedMergeRequests(new MergeRequestKey[] { mrk });
            }
         }
         if (e.RemovedFromCache && isMergeRequestHidden(type, e.FullMergeRequestKey.MergeRequest))
         {
            toggleMergeRequestExclusion(type, e.FullMergeRequestKey.MergeRequest);
         }

         updateMergeRequestList(type);
         if (e.Details || e.Commits || e.Labels)
         {
            notifyDiscussionForm(mrk);
         }

         FullMergeRequestKey? fmk = getListView(type).GetSelectedMergeRequest();
         if (!fmk.HasValue || !fmk.Value.Equals(e.FullMergeRequestKey) || getCurrentTabDataCacheType() != type)
         {
            return;
         }

         if (e.Details || e.Commits || e.Labels)
         {
            // Non-grid Details are updated here and Grid ones are updated in updateMergeRequestList() above
            Trace.TraceInformation("[ConnectionPage] Updating selected Merge Request ({0})",
               getDataCacheName(getDataCache(type)));
            onMergeRequestSelectionChanged(type);
         }
      }

      private void onMergeRequestRefreshed(MergeRequestKey mrk)
      {
         // update Refreshed column
         getListView(EDataCacheType.Live).Invalidate();

         if (Program.Settings.UpdateManagerExtendedLogging)
         {
            DataCache dataCache = getDataCache(EDataCacheType.Live);
            DateTime? refreshTimestamp = dataCache?.MergeRequestCache?.GetMergeRequestRefreshTime(mrk);
            Trace.TraceInformation(String.Format(
               "[ConnectionPage] Merge Request {0} refreshed at {1}",
               mrk.IId, refreshTimestamp.HasValue ? refreshTimestamp.Value.ToString() : "N/A"));
         }
      }

      private void onLiveMergeRequestListRefreshed()
      {
         LatestListRefreshTimestampChanged?.Invoke(this);

         // update Refreshed column
         getListView(EDataCacheType.Live).Invalidate();

         if (Program.Settings.UpdateManagerExtendedLogging)
         {
            DataCache dataCache = getDataCache(EDataCacheType.Live);
            DateTime? refreshTimestamp = dataCache?.MergeRequestCache?.GetListRefreshTime();
            Trace.TraceInformation(String.Format(
               "[ConnectionPage] Merge Request List refreshed at {0}",
               refreshTimestamp.HasValue ? refreshTimestamp.Value.ToString() : "N/A"));
         }
      }
   }
}

