using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createSearchWorkflow()
      {
         _searchWorkflowManager = new SearchWorkflowManager(Program.Settings);
      }

      private void subscribeToSearchWorkflow()
      {
         _searchWorkflowManager.PreLoadMergeRequest += onLoadSingleSearchMergeRequest;
         _searchWorkflowManager.PostLoadMergeRequest += onSingleSearchMergeRequestLoaded;
         _searchWorkflowManager.FailedLoadMergeRequest += onFailedLoadSingleSearchMergeRequest;

         _searchWorkflowManager.PreLoadComparableEntities += onLoadSearchComparableEntities;
         _searchWorkflowManager.PostLoadComparableEntities += onSearchComparableEntitiesLoaded;
         _searchWorkflowManager.FailedLoadComparableEntities +=  onFailedLoadSearchComparableEntities;
      }

      private void unsubscribeFromSearchWorkflow()
      {
         _searchWorkflowManager.PreLoadMergeRequest -= onLoadSingleSearchMergeRequest;
         _searchWorkflowManager.PostLoadMergeRequest -= onSingleSearchMergeRequestLoaded;
         _searchWorkflowManager.FailedLoadMergeRequest -= onFailedLoadSingleSearchMergeRequest;

         _searchWorkflowManager.PreLoadComparableEntities -= onLoadSearchComparableEntities;
         _searchWorkflowManager.PostLoadComparableEntities -= onSearchComparableEntitiesLoaded;
         _searchWorkflowManager.FailedLoadComparableEntities -=  onFailedLoadSearchComparableEntities;
      }

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
            if (ex is WorkflowException || ex is UnknownHostException)
            {
               disableAllSearchUIControls(true);
               ExceptionHandlers.Handle("Cannot perform merge request search", ex);
               string message = ex.Message;
               if (ex is WorkflowException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            throw;
         }

         if (!isSearchMode())
         {
            _suppressExternalConnections = false;
            return;
         }

         _suppressExternalConnections = _suppressExternalConnections
            && selectMergeRequest(listViewFoundMergeRequests, String.Empty, 0, false);
      }

      async private Task<bool> switchSearchMergeRequestByUserAsync(ProjectKey projectKey, int mergeRequestIId,
         bool showVersions)
      {
         Trace.TraceInformation(String.Format("[MainForm.Search] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         if (mergeRequestIId == 0)
         {
            onLoadSingleSearchMergeRequest(0);
            await _searchWorkflowManager.CancelAsync();
            return false;
         }

         await _searchWorkflowManager.CancelAsync();

         _suppressExternalConnections = true;
         try
         {
            return await _searchWorkflowManager.LoadMergeRequestAsync(
               projectKey.HostName, projectKey.ProjectName, mergeRequestIId,
               showVersions ? EComparableEntityType.Version : EComparableEntityType.Commit);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle("Cannot switch merge request", ex);
            MessageBox.Show(ex.UserMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            _suppressExternalConnections = false;
         }

         return false;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task<bool> startSearchWorkflowAsync(string hostname, object query, int? maxResults)
      {
         labelWorkflowStatus.Text = String.Empty;

         await _searchWorkflowManager.CancelAsync();
         if (String.IsNullOrWhiteSpace(hostname))
         {
            disableAllSearchUIControls(true);
            return false;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         disableAllSearchUIControls(true);
         return await loadAllSearchMergeRequests(hostname, query, maxResults);
      }

      async private Task<bool> loadAllSearchMergeRequests(string hostname, object query, int? maxResults)
      {
         onLoadAllSearchMergeRequests();

         Dictionary<Project, IEnumerable<MergeRequest>> projectMergeRequests =
            await _searchWorkflowManager.LoadAllMergeRequestsAsync(hostname, query, maxResults);
         if (projectMergeRequests == null)
         {
            return false;
         }

         foreach (KeyValuePair<Project, IEnumerable<MergeRequest>> keyValuePair in projectMergeRequests)
         {
            onProjectSearchMergeRequestsLoaded(hostname, keyValuePair.Key, keyValuePair.Value);
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

      private void onProjectSearchMergeRequestsLoaded(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         labelWorkflowStatus.Text = String.Format(
            "Search results for project {0} loaded", project.Path_With_Namespace);

         Debug.WriteLine(String.Format(
            "[MainForm.Search] Project {0} loaded. Loaded {1} merge requests",
           project.Path_With_Namespace, mergeRequests.Count()));

         createListViewGroupForProject(listViewFoundMergeRequests, hostname, project);
         fillListViewSearchMergeRequests(hostname, project, mergeRequests);
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

      private void onLoadSingleSearchMergeRequest(int mergeRequestIId)
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onLoadSingleMergeRequestCommon(mergeRequestIId);
      }

      private void onFailedLoadSingleSearchMergeRequest()
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onFailedLoadSingleMergeRequestCommon();
      }

      private void onSingleSearchMergeRequestLoaded(string hostname, string projectname, MergeRequest mergeRequest)
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onSingleMergeRequestLoadedCommon(hostname, projectname, mergeRequest);
      }

      private void onLoadSearchComparableEntities()
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onLoadComparableEntitiesCommon(listViewFoundMergeRequests);
      }

      private void onFailedLoadSearchComparableEntities()
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onFailedLoadComparableEntitiesCommon();
      }

      private void onSearchComparableEntitiesLoaded(string hostname, string projectname, MergeRequest mergeRequest,
         System.Collections.IEnumerable commits)
      {
         if (!isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(mergeRequest, commits, listViewFoundMergeRequests);
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

      private void fillListViewSearchMergeRequests(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         ProjectKey projectKey = new ProjectKey
         {
            HostName = hostname,
            ProjectName = project.Path_With_Namespace
         };

         listViewFoundMergeRequests.BeginUpdate();

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ListViewItem item = addListViewMergeRequestItem(listViewFoundMergeRequests, projectKey);
            setListViewItemTag(item, projectKey, mergeRequest);
         }

         int maxLineCount = 2;
         setListViewRowHeight(listViewFoundMergeRequests, listViewFoundMergeRequests.Font.Height * maxLineCount + 2);

         listViewFoundMergeRequests.EndUpdate();
      }
   }
}

