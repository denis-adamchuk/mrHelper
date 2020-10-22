using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private bool startSearchWorkflowDefaultExceptionHandler(Exception ex)
      {
         if (ex is DataCacheException || ex is UnknownHostException)
         {
            if (!(ex is DataCacheConnectionCancelledException))
            {
               disableAllSearchUIControls(true);
               ExceptionHandlers.Handle("Cannot perform merge request search", ex);
               string message = ex.Message;
               if (ex is DataCacheException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
         }
         return false;
      }

      private void searchMergeRequests(SearchQueryCollection queryCollection,
         Func<Exception, bool> exceptionHandler = null)
      {
         BeginInvoke(new Action(async () => await searchMergeRequestsAsync(queryCollection, exceptionHandler)), null);
      }

      async private Task searchMergeRequestsAsync(SearchQueryCollection queryCollection,
         Func<Exception, bool> exceptionHandler = null)
      {
         try
         {
            await startSearchWorkflowAsync(getHostName(), queryCollection);
         }
         catch (Exception ex)
         {
            if (exceptionHandler == null)
            {
               exceptionHandler = new Func<Exception, bool>((e) => startSearchWorkflowDefaultExceptionHandler(e));
            }
            if (!exceptionHandler(ex))
            {
               throw;
            }
         }
      }

      private void onSearchMergeRequestSelectionChanged(FullMergeRequestKey fmk)
      {
         Debug.Assert(fmk.MergeRequest != null && fmk.MergeRequest.IId != 0);

         Trace.TraceInformation(String.Format("[MainForm.Search] User requested to change merge request to IId {0}",
            fmk.MergeRequest.IId.ToString()));

         onSingleSearchMergeRequestLoaded(fmk);

         IMergeRequestCache cache = getDataCache(ECurrentMode.Search).MergeRequestCache;
         if (cache != null)
         {
            MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            onSearchComparableEntitiesLoaded(latestVersion, fmk.MergeRequest,
               cache.GetCommits(mrk), cache.GetVersions(mrk));
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      /// <summary>
      /// Connects Search DataCache to GitLab
      /// </summary>
      async private Task startSearchWorkflowAsync(string hostname, SearchQueryCollection queryCollection)
      {
         labelWorkflowStatus.Text = String.Empty;
         disableAllSearchUIControls(true);

         if (String.IsNullOrWhiteSpace(hostname))
         {
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         await loadAllSearchMergeRequests(hostname, queryCollection);
      }

      async private Task loadAllSearchMergeRequests(string hostname, SearchQueryCollection queryCollection)
      {
         // TODO Not First
         onLoadAllSearchMergeRequests(queryCollection, hostname);

         DataCacheConnectionContext sessionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(null, null),
            new DataCacheUpdateRules(null, null),
            queryCollection);

         DataCache dataCache = getDataCache(ECurrentMode.Search);
         await dataCache.Connect(new GitLabInstance(hostname, Program.Settings), sessionContext);

         foreach (ProjectKey projectKey in dataCache.MergeRequestCache.GetProjects())
         {
            onProjectSearchMergeRequestsLoaded(projectKey, dataCache.MergeRequestCache.GetMergeRequests(projectKey));
         }

         onAllSearchMergeRequestsLoaded();
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllSearchMergeRequests(SearchQueryCollection queryCollection, string hostname)
      {
         listViewFoundMergeRequests.Items.Clear();
         labelWorkflowStatus.Text = String.Format("Searching by criteria: {0} at {1}...",
            queryCollection.ToString(), hostname);
      }

      private void onProjectSearchMergeRequestsLoaded(ProjectKey projectKey,
         IEnumerable<MergeRequest> mergeRequests)
      {
         createListViewGroupForProject(listViewFoundMergeRequests, projectKey, true);
         fillListViewSearchMergeRequests(projectKey, mergeRequests);
      }

      private void onAllSearchMergeRequestsLoaded()
      {
         if (listViewFoundMergeRequests.Items.Count > 0)
         {
            enableListView(listViewFoundMergeRequests);
            labelWorkflowStatus.Text = String.Empty;
         }
         else
         {
            labelWorkflowStatus.Text = "Nothing found. Try more specific search query.";
         }

         if (getMode() == ECurrentMode.Search)
         {
            selectMergeRequest(listViewFoundMergeRequests, new MergeRequestKey?(), false);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSingleSearchMergeRequestLoaded(FullMergeRequestKey fmk)
      {
         if (getMode() != ECurrentMode.Search)
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         DataCache dataCache = getDataCache(ECurrentMode.Search);
         onSingleMergeRequestLoadedCommon(fmk, dataCache);
      }

      private void onSearchComparableEntitiesLoaded(GitLabSharp.Entities.Version latestVersion,
         MergeRequest mergeRequest, IEnumerable<Commit> commits, IEnumerable<GitLabSharp.Entities.Version> versions)
      {
         if (getMode() != ECurrentMode.Search)
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(latestVersion, mergeRequest, commits, versions, listViewFoundMergeRequests);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllSearchUIControls(bool clearListView)
      {
         disableListView(listViewFoundMergeRequests, clearListView);

         if (getMode() == ECurrentMode.Search)
         {
            // to avoid touching controls shared between Live and Search tabs
            disableCommonUIControls();
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void fillListViewSearchMergeRequests(ProjectKey projectKey, IEnumerable<MergeRequest> mergeRequests)
      {
         listViewFoundMergeRequests.BeginUpdate();

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey(projectKey, mergeRequest);
            ListViewItem item = createListViewMergeRequestItem(listViewFoundMergeRequests, fmk);
            listViewFoundMergeRequests.Items.Add(item);
            setListViewSubItemsTags(item, fmk);
         }

         recalcRowHeightForMergeRequestListView(listViewFoundMergeRequests);

         listViewFoundMergeRequests.EndUpdate();
      }
   }
}

