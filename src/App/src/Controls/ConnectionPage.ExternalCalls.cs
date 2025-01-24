using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      private string getDiffTempFolder(Snapshot snapshot)
      {
         if (ConfigurationHelper.GetPreferredStorageType(Program.Settings) == LocalCommitStorageType.FileStorage)
         {
            return snapshot.TempFolder;
         }
         return PathFinder.SnapshotStorage;
      }

      private void createMergeRequestFromUrl(ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
      {
         if (!checkIfMergeRequestCanBeCreated())
         {
            return;
         }

         NewMergeRequestProperties defaultProperties = getDefaultNewMergeRequestProperties(
            HostName, CurrentUser, null);
         NewMergeRequestProperties initialProperties = new NewMergeRequestProperties(
            parsedNewMergeRequestUrl.ProjectKey.ProjectName, parsedNewMergeRequestUrl.SourceBranch,
            parsedNewMergeRequestUrl.TargetBranchCandidates, defaultProperties.AssigneeUsername,
            defaultProperties.IsSquashNeeded, defaultProperties.IsBranchDeletionNeeded,
            defaultProperties.FavoriteProjects);
         IEnumerable<User> fullUserList = getUsers();
         if (!fullUserList.Any())
         {
            Trace.TraceInformation("[ConnectionPage] User list is not ready at the moment of creating a MR from URL");
         }

         IEnumerable<Project> fullProjectList = getProjects();
         createNewMergeRequest(HostName, CurrentUser, initialProperties, fullProjectList, fullUserList, false);
      }

      async private Task connectToUrlAsyncInternal<T>(string url, T parsedUrl)
      {
         MergeRequestKey mrk = parseUrlIntoMergeRequestKey(parsedUrl);
         if (parsedUrl is UrlParser.ParsedNoteUrl noteUrl)
         {
            await connectToNoteUrlAsync(mrk, noteUrl);
            return;
         }

         // First, try to select a MR from lists of visible MRs
         bool tryOpenAtLiveTab = true;
         switch (trySelectMergeRequest(mrk))
         {
            case SelectionResult.NotFound:
               break;
            case SelectionResult.Selected:
               addOperationRecord("Merge Request was found in cache and selected");
               return;
            case SelectionResult.Hidden:
               tryOpenAtLiveTab = false;
               break;
         }

         Debug.Assert(getDataCache(EDataCacheType.Live)?.ConnectionContext != null);

         // If MR is not found at the Live tab at all or user rejected to unhide it,
         // don't try to open it at the Live tab.
         // Otherwise, check if requested MR match workflow filters.
         tryOpenAtLiveTab = tryOpenAtLiveTab && (await checkLiveDataCacheFilterAsync(mrk, url));
         if (!tryOpenAtLiveTab || !await openUrlAtLiveTabAsync(mrk, url))
         {
            await openUrlAtSearchTabAsync(mrk);
         }
      }

      private async Task connectToNoteUrlAsync(MergeRequestKey mrk, UrlParser.ParsedNoteUrl noteUrl)
      {
         MergeRequest mergeRequest = null;
         foreach (EDataCacheType mode in getOrderedCacheTypes())
         {
            if (isCached(mode, mrk))
            {
               mergeRequest = getDataCache(mode).MergeRequestCache.GetMergeRequest(mrk);
               Trace.TraceInformation(String.Format(
                  "[ConnectionPage] Merge Request is found in cache {0}", mode.ToString()));
               break;
            }
         }

         if (mergeRequest == null)
         {
            Trace.TraceInformation("[ConnectionPage] Merge Request not found in caches. Trying to find at GitLab...");

            await searchMergeRequestsSafeAsync(
               new SearchQueryCollection(new GitLabClient.SearchQuery
               {
                  IId = mrk.IId,
                  ProjectName = mrk.ProjectKey.ProjectName,
                  MaxResults = 1
               }),
               EDataCacheType.Search,
               new Func<Exception, bool>(x =>
                  throw new UrlConnectionException("Failed to open a note for a merge request. ", x)));

            mergeRequest = getDataCache(EDataCacheType.Search).MergeRequestCache.GetMergeRequest(mrk);
         }

         if (mergeRequest != null)
         {
            showDiscussionsForMergeRequest(mergeRequest, mrk, noteUrl.NoteId);
         }
      }

      private enum SelectionResult
      {
         NotFound,
         Hidden,
         Selected,
      }

      private static EDataCacheType[] getOrderedCacheTypes()
      {
         // We want to check lists in specific order:
         return new EDataCacheType[]
         {
            EDataCacheType.Live,
            EDataCacheType.Recent,
            EDataCacheType.Search
         };
      }

      private SelectionResult trySelectMergeRequest(MergeRequestKey mrk)
      {
         // We want to check lists in specific order:
         EDataCacheType[] modes = getOrderedCacheTypes();

         // Check if requested MR is cached
         if (modes.All(mode => !isCached(mode, mrk)))
         {
            return SelectionResult.NotFound;
         }

         // Try selecting an item which is not hidden by filters
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode, mrk) && trySelectMergeRequest(mode, mrk))
            {
               return SelectionResult.Selected;
            }
         }

         // If we are here, requested MR is hidden on each tab where it is cached
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode, mrk))
            {
               if (unhideFilteredMergeRequest(mode))
               {
                  if (trySelectMergeRequest(mode, mrk))
                  {
                     return SelectionResult.Selected;
                  }
                  Debug.Assert(false);
               }
               else
               {
                  break; // don't ask more than once
               }
            }
         }

         return SelectionResult.Hidden;
      }

      async private Task<MergeRequest> searchMergeRequestAsync(MergeRequestKey mrk)
      {
         try
         {
            MergeRequest mergeRequest = await _shortcuts
               .GetMergeRequestAccessor(mrk.ProjectKey)
               .SearchMergeRequestAsync(mrk.IId, false);
            if (mergeRequest == null)
            {
               throw new UrlConnectionException("Merge request does not exist. ");
            }
            return mergeRequest;
         }
         catch (MergeRequestAccessorException ex)
         {
            throw new UrlConnectionException("Failed to check if merge request exists at GitLab. ", ex);
         }
      }

      async private Task<bool> openUrlAtLiveTabAsync(MergeRequestKey mrk, string url)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache?.MergeRequestCache == null)
         {
            throw new UrlConnectionException("Merge request loading was cancelled due to reconnect. ");
         }

         if (!dataCache.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
         {
            // We need to update the MR list here because cached one is possible outdated
            addOperationRecord(String.Format(
               "Merge Request with IId {0} is not found in the cache. List update has started.", mrk.IId));
            await checkForUpdatesAsync(getDataCache(EDataCacheType.Live), null);
            addOperationRecord("Merge request list update has completed");
            if (dataCache.MergeRequestCache == null)
            {
               throw new UrlConnectionException("Merge request loading was cancelled due reconnect. ");
            }
         }

         if (!trySelectMergeRequest(EDataCacheType.Live, mrk) && getListView(EDataCacheType.Live).Enabled)
         {
            // We could not select MR, but let's check if it is cached or not.
            if (dataCache.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
            {
               // If it is cached, it is probably hidden by filters and user might want to un-hide it.
               if (!unhideFilteredMergeRequest(EDataCacheType.Live))
               {
                  return false; // user decided to not un-hide merge request
               }

               if (!trySelectMergeRequest(EDataCacheType.Live, mrk))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[ConnectionPage] Cannot open URL {0}, although MR is cached", url));
                  throw new UrlConnectionException("Something went wrong. ");
               }
            }
            else
            {
               return false;
            }
         }

         return true;
      }

      async private Task openUrlAtSearchTabAsync(MergeRequestKey mrk)
      {
         await searchMergeRequestsSafeAsync(
            new SearchQueryCollection(new GitLabClient.SearchQuery
            {
               IId = mrk.IId,
               ProjectName = mrk.ProjectKey.ProjectName,
               MaxResults = 1
            }),
            EDataCacheType.Search,
            new Func<Exception, bool>(x =>
               throw new UrlConnectionException("Failed to open merge request at Search tab. ", x)));
         trySelectMergeRequest(EDataCacheType.Search, mrk);
      }

      private bool unhideFilteredMergeRequest(EDataCacheType dataCacheType)
      {
         Trace.TraceInformation("[ConnectionPage] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters and cannot be opened. Do you want to switch off Filter?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[ConnectionPage] User decided not to reset filters");
            return false;
         }

         setFilterStateUI(dataCacheType, FilterState.Disabled); // does not fire SelectionChangeCommited event
         onCheckBoxDisplayFilterUpdate(dataCacheType, FilterState.Disabled);
         return true;
      }

      async private Task<bool> checkLiveDataCacheFilterAsync(MergeRequestKey mrk, string url)
      {
         addOperationRecord(String.Format("Checking merge request at {0} started", url));
         MergeRequest mergeRequest = await searchMergeRequestAsync(mrk);
         Debug.Assert(mergeRequest != null);
         addOperationRecord(String.Format("Checking merge request at {0} has completed", url));

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache == null)
         {
            return false;
         }

         Debug.Assert(dataCache.ConnectionContext != null);
         SearchQueryCollection queries = dataCache.ConnectionContext.QueryCollection;
         return GitLabClient.Helpers.DoesMatchSearchQuery(queries, mergeRequest, mrk.ProjectKey);
      }

      private MergeRequestKey parseUrlIntoMergeRequestKey(dynamic parsedUrl)
      {
         return new MergeRequestKey(new ProjectKey(parsedUrl.Host, parsedUrl.Project), parsedUrl.IId);
      }
   }
}

