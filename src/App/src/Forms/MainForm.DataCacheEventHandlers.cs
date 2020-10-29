using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
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
            MergeRequest currentMergeRequest = getMergeRequest(null);
            if (currentMergeRequest != null)
            {
               // change control enabled state
               updateTotalTime(currentMergeRequestKey,
                  currentMergeRequest.Author, currentMergeRequestKey.Value.ProjectKey.HostName, totalTimeCache);
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
      }

      private void onDiscussionManagerEvent()
      {
         // Update Discussions column in the table
         getListView(EDataCacheType.Live).Invalidate();
      }

      private void onMergeRequestEvent(UserEvents.MergeRequestEvent e)
      {
         if (e.New || e.Commits)
         {
            requestCommitStorageUpdate(e.FullMergeRequestKey.ProjectKey);
         }

         MergeRequestKey mrk = new MergeRequestKey(
            e.FullMergeRequestKey.ProjectKey, e.FullMergeRequestKey.MergeRequest.IId);

         if (e.Closed && isReviewedMergeRequest(mrk))
         {
            MergeRequestKey[] closedMergeRequests = new MergeRequestKey[] { mrk };
            cleanupReviewedMergeRequests(closedMergeRequests);
            addRecentMergeRequestKeys(closedMergeRequests);
            reloadRecentMergeRequests(getHostName());
         }

         updateMergeRequestList(EDataCacheType.Live);

         if (e.New)
         {
            requestUpdates(mrk, new[] {
               Program.Settings.OneShotUpdateOnNewMergeRequestFirstChanceDelayMs,
               Program.Settings.OneShotUpdateOnNewMergeRequestSecondChanceDelayMs});
         }

         FullMergeRequestKey? fmk = getListView(EDataCacheType.Live).GetSelectedMergeRequest();
         if (!fmk.HasValue || !fmk.Value.Equals(e.FullMergeRequestKey) || getCurrentTabDataCacheType() != EDataCacheType.Live)
         {
            return;
         }

         Trace.TraceInformation("[MainForm] Updating selected Merge Request");

         if (e.Details)
         {
            // Non-grid Details are updated here and Grid ones are updated in updateMergeRequestList() above
            updateMergeRequestDetails(fmk.Value);
         }

         if (e.Commits)
         {
            onMergeRequestSelectionChanged(EDataCacheType.Live);
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
               "[MainForm] Merge Request {0} refreshed at {1}",
               mrk.IId, refreshTimestamp.HasValue ? refreshTimestamp.Value.ToString() : "N/A"));
         }
      }

      private void onLiveMergeRequestListRefreshed()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         DateTime? refreshTimestamp = dataCache?.MergeRequestCache?.GetListRefreshTime();
         string refreshedAt = refreshTimestamp.HasValue
            ? String.Format("Refreshed at {0}",
               refreshTimestamp.Value.ToLocalTime().ToString(Constants.TimeStampFormat))
            : String.Empty;
         toolTip.SetToolTip(this.buttonReloadList, String.Format("{0}{1}{2}",
            RefreshButtonTooltip, refreshedAt == String.Empty ? String.Empty : "\r\n", refreshedAt));

         // update Refreshed column
         getListView(EDataCacheType.Live).Invalidate();

         if (Program.Settings.UpdateManagerExtendedLogging)
         {
            Trace.TraceInformation(String.Format(
               "[MainForm] Merge Request List refreshed at {0}",
               refreshTimestamp.HasValue ? refreshTimestamp.Value.ToString() : "N/A"));
         }
      }
   }
}

