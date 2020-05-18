using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.MergeRequests;
using System.Collections;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task searchMergeRequests(object query, int? maxResults)
      {
         _suppressExternalConnections = true;
         try
         {
            _suppressExternalConnections = await startSearchWorkflowAsync(getHostName(), query, maxResults);
         }
         catch (Exception ex)
         {
            _suppressExternalConnections = false;
            if (ex is SessionException || ex is UnknownHostException)
            {
               disableAllSearchUIControls(true);
               ExceptionHandlers.Handle("Cannot perform merge request search", ex);
               string message = ex.Message;
               if (ex is SessionException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            throw;
         }
         _suppressExternalConnections = false;
         if (isSearchMode())
         {
            selectMergeRequest(listViewFoundMergeRequests, String.Empty, 0, false);
         }
      }

      private void switchSearchMergeRequestByUser(FullMergeRequestKey fmk, bool showVersions)
      {
         Debug.Assert(fmk.MergeRequest != null && fmk.MergeRequest.IId != 0);

         Trace.TraceInformation(String.Format("[MainForm.Search] User requested to change merge request to IId {0}",
            fmk.MergeRequest.IId.ToString()));

         _suppressExternalConnections = true;
         try
         {
            onSingleSearchMergeRequestLoaded(fmk);

            IMergeRequestCache cache = _searchSession.MergeRequestCache;
            MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            onSearchComparableEntitiesLoaded(latestVersion, fmk.MergeRequest,
               showVersions ? (IEnumerable)cache.GetVersions(mrk) : (IEnumerable)cache.GetCommits(mrk));
         }
         finally
         {
            _suppressExternalConnections = false;
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task<bool> startSearchWorkflowAsync(string hostname, object query, int? maxResults)
      {
         labelWorkflowStatus.Text = String.Empty;

         await _searchSession.Stop();
         disableAllSearchUIControls(true);

         if (String.IsNullOrWhiteSpace(hostname))
         {
            return false;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         return await loadAllSearchMergeRequests(hostname, query, maxResults);
      }

      async private Task<bool> loadAllSearchMergeRequests(string hostname, object query, int? maxResults)
      {
         onLoadAllSearchMergeRequests();

         SearchCriteria searchCriteria = new SearchCriteria(new object[] { query });
         SessionContext sessionContext = new SessionContext(
            new SessionCallbacks(null, null),
            new SessionUpdateRules(false, false),
            new SearchBasedContext(searchCriteria, maxResults, false));

         if (!await _searchSession.Start(hostname, sessionContext))
         {
            return false;
         }

         foreach (ProjectKey projectKey in _searchSession.MergeRequestCache.GetProjects())
         {
            onProjectSearchMergeRequestsLoaded(projectKey,
               _searchSession.MergeRequestCache.GetMergeRequests(projectKey));
         }

         onAllSearchMergeRequestsLoaded();
         return true;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllSearchMergeRequests()
      {
         listViewFoundMergeRequests.Items.Clear();
         labelWorkflowStatus.Text = "Search in progress";
      }

      private void onProjectSearchMergeRequestsLoaded(ProjectKey projectKey,
         IEnumerable<MergeRequest> mergeRequests)
      {
         labelWorkflowStatus.Text = String.Format(
            "Search results for project {0} loaded", projectKey.ProjectName);

         Debug.WriteLine(String.Format(
            "[MainForm.Search] Project {0} loaded. Loaded {1} merge requests",
           projectKey.ProjectName, mergeRequests.Count()));

         createListViewGroupForProject(listViewFoundMergeRequests, projectKey);
         fillListViewSearchMergeRequests(projectKey, mergeRequests);
      }

      private void onAllSearchMergeRequestsLoaded()
      {
         if (listViewFoundMergeRequests.Items.Count > 0)
         {
            enableListView(listViewFoundMergeRequests);
         }
         else
         {
            labelWorkflowStatus.Text = "Nothing found. Try more specific search query.";
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSingleSearchMergeRequestLoaded(FullMergeRequestKey fmk)
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onSingleMergeRequestLoadedCommon(fmk, _searchSession);
      }

      private void onSearchComparableEntitiesLoaded(GitLabSharp.Entities.Version latestVersion,
         MergeRequest mergeRequest, IEnumerable commits)
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(latestVersion, mergeRequest, commits, listViewFoundMergeRequests);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllSearchUIControls(bool clearListView)
      {
         disableListView(listViewFoundMergeRequests, clearListView);

         if (!isSearchMode())
         {
            // to avoid touching controls shared between Live and Search tabs
            return;
         }

         disableCommonUIControls();
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void fillListViewSearchMergeRequests(ProjectKey projectKey, IEnumerable<MergeRequest> mergeRequests)
      {
         listViewFoundMergeRequests.BeginUpdate();

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ListViewItem item = addListViewMergeRequestItem(listViewFoundMergeRequests, projectKey);
            setListViewItemTag(item, new FullMergeRequestKey(projectKey, mergeRequest));
         }

         int maxLineCount = 2;
         setListViewRowHeight(listViewFoundMergeRequests, listViewFoundMergeRequests.Font.Height * maxLineCount + 2);

         listViewFoundMergeRequests.EndUpdate();
      }
   }
}

