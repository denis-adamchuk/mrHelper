﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;
using static mrHelper.App.Helpers.ConfigurationHelper;

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
            defaultProperties.IsSquashNeeded, defaultProperties.IsBranchDeletionNeeded);
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         var fullProjectList = dataCache?.ProjectCache?.GetProjects() ?? Array.Empty<Project>();
         var fullUserList = dataCache?.UserCache?.GetUsers() ?? Array.Empty<User>();
         if (!fullUserList.Any())
         {
            Trace.TraceInformation("[ConnectionPage] User list is not ready at the moment of creating a MR from URL");
         }

         createNewMergeRequest(HostName, CurrentUser, initialProperties, fullProjectList, fullUserList);
      }

      async private Task connectToUrlAsyncInternal(string url, UrlParser.ParsedMergeRequestUrl parsedUrl)
      {
         MergeRequestKey mrk = parseUrlIntoMergeRequestKey(parsedUrl);

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

      private enum SelectionResult
      {
         NotFound,
         Hidden,
         Selected,
      }

      private SelectionResult trySelectMergeRequest(MergeRequestKey mrk)
      {
         bool isCached(EDataCacheType mode) => getDataCache(mode)?.MergeRequestCache?.GetMergeRequest(mrk) != null;

         // We want to check lists in specific order:
         EDataCacheType[] modes = new EDataCacheType[]
         {
            EDataCacheType.Live,
            EDataCacheType.Recent,
            EDataCacheType.Search
         };

         // Check if requested MR is cached
         if (modes.All(mode => !isCached(mode)))
         {
            return SelectionResult.NotFound;
         }

         // Try selecting an item which is not hidden by filters
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode) && switchTabAndSelectMergeRequest(mode, mrk))
            {
               return SelectionResult.Selected;
            }
         }

         // If we are here, requested MR is hidden on each tab where it is cached
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode))
            {
               if (unhideFilteredMergeRequest(mode))
               {
                  if (switchTabAndSelectMergeRequest(mode, mrk))
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

      async private Task reconnect()
      {
         await connect(new Func<Exception, bool>(x =>
            throw new UrlConnectionException("Failed to connect to GitLab. ", x)));
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

            if (!checkProjectWorkflowFilters(mrk))
            {
               // this may happen if project list changed while we were in 'await'
               return false;
            }
         }

         if (!switchTabAndSelectMergeRequest(EDataCacheType.Live, mrk) && getListView(EDataCacheType.Live).Enabled)
         {
            // We could not select MR, but let's check if it is cached or not.
            if (dataCache.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
            {
               // If it is cached, it is probably hidden by filters and user might want to un-hide it.
               if (!unhideFilteredMergeRequest(EDataCacheType.Live))
               {
                  return false; // user decided to not un-hide merge request
               }

               if (!switchTabAndSelectMergeRequest(EDataCacheType.Live, mrk))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[ConnectionPage] Cannot open URL {0}, although MR is cached", url));
                  throw new UrlConnectionException("Something went wrong. ");
               }
            }
            else
            {
               if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[ConnectionPage] Cannot open URL {0} by unknown reason", url));
                  throw new UrlConnectionException("Something went wrong. ");
               }
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
         switchTabAndSelectMergeRequest(EDataCacheType.Search, mrk);
      }

      private bool unhideFilteredMergeRequest(EDataCacheType dataCacheType)
      {
         Trace.TraceInformation("[ConnectionPage] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters and cannot be opened. Do you want to reset filters?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[ConnectionPage] User decided not to reset filters");
            return false;
         }

         if (dataCacheType == EDataCacheType.Live)
         {
            checkBoxDisplayFilter.Checked = false;
         }
         return true;
      }

      private bool addMissingProject(ProjectKey projectKey)
      {
         Trace.TraceInformation("[ConnectionPage] Notify that selected project is not in the list");

         if (MessageBox.Show("Selected project is not in the list of projects. Do you want to add it? "
               + "Selecting 'Yes' will cause reload of all projects. "
               + "Selecting 'No' will open the merge request at Search tab. ",
               "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[ConnectionPage] User decided not to add project");
            return false;
         }

         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            projectKey.HostName, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(!projects.ContainsKey(projectKey.ProjectName));
         projects.Add(projectKey.ProjectName, true);

         ConfigurationHelper.SetProjectsForHost(
            projectKey.HostName,
            new StringToBooleanCollection(Enumerable.Zip(
               projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y))),
            Program.Settings);
         return true;
      }

      private bool enableDisabledProject(ProjectKey projectKey)
      {
         Trace.TraceInformation("[ConnectionPage] Notify that selected project is disabled");

         if (MessageBox.Show("Selected project is not enabled. Do you want to enable it? "
               + "Selecting 'Yes' will cause reload of all projects. "
               + "Selecting 'No' will open the merge request at Search tab. ",
               "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[ConnectionPage] User decided not to enable project");
            return false;
         }

         changeProjectEnabledState(projectKey, true);
         return true;
      }

      private bool checkProjectWorkflowFilters(MergeRequestKey mrk)
      {
         if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            return true;
         }

         return isProjectInTheList(mrk.ProjectKey) && isEnabledProject(mrk.ProjectKey);
      }

      async private Task<bool> fixProjectWorkflowFiltersAsync(MergeRequestKey mrk)
      {
         if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            return true;
         }

         if (!isProjectInTheList(mrk.ProjectKey))
         {
            if (addMissingProject(mrk.ProjectKey))
            {
               await reconnect();
               return true;
            }
            return false;
         }

         if (!isEnabledProject(mrk.ProjectKey))
         {
            if (enableDisabledProject(mrk.ProjectKey))
            {
               await reconnect();
               return true;
            }
            return false;
         }

         return true;
      }

      async private Task<bool> checkLiveDataCacheFilterAsync(MergeRequestKey mrk, string url)
      {
         if (!await fixProjectWorkflowFiltersAsync(mrk))
         {
            return false;
         }

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

      private MergeRequestKey parseUrlIntoMergeRequestKey(UrlParser.ParsedMergeRequestUrl parsedUrl)
      {
         return new MergeRequestKey(new ProjectKey(parsedUrl.Host, parsedUrl.Project), parsedUrl.IId);
      }

      private static bool isProjectInTheList(ProjectKey projectKey)
      {
         StringToBooleanCollection projects =
            ConfigurationHelper.GetProjectsForHost(projectKey.HostName, Program.Settings);
         return projects.Any(x => projectKey.MatchProject(x.Item1));
      }

      private static bool isEnabledProject(ProjectKey projectKey)
      {
         StringToBooleanCollection projects =
            ConfigurationHelper.GetProjectsForHost(projectKey.HostName, Program.Settings);
         StringToBooleanCollection enabled =
            new StringToBooleanCollection(projects.Where(x => projectKey.MatchProject(x.Item1)));
         return enabled.Any() && enabled.First().Item2;
      }

      private bool isCustomActionEnabled(IEnumerable<User> approvedBy,
         IEnumerable<string> labels, User author, string dependency)
      {
         if (String.IsNullOrEmpty(dependency))
         {
            return true;
         }

         string excludePrefix = "NOT ";
         bool isExpected = !dependency.StartsWith(excludePrefix);
         dependency = isExpected ? dependency : dependency.Substring(excludePrefix.Length);
         if (isExpected)
         {
            return labels.Any(x => StringUtils.DoesMatchPattern(dependency, "{{Label:{0}}}", x))
                || StringUtils.DoesMatchPattern(dependency, "{{Author:{0}}}", author.Username)
                || approvedBy.Any(x => StringUtils.DoesMatchPattern(dependency, "{{Approved_By:{0}}}", x.Username));
         }
         else
         {
            return labels.All(x => !StringUtils.DoesMatchPattern(dependency, "{{Label:{0}}}", x))
                && !StringUtils.DoesMatchPattern(dependency, "{{Author:{0}}}", author.Username)
                && approvedBy.All(x => !StringUtils.DoesMatchPattern(dependency, "{{Approved_By:{0}}}", x.Username));
         }
      }

   }
}
