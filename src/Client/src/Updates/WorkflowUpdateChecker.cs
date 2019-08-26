using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using System.ComponentModel;
using System.Diagnostics;

namespace mrHelper.Client.Updates
{
   public struct MergeRequestUpdates
   {
      public List<MergeRequest> NewMergeRequests;
      public List<MergeRequest> UpdatedMergeRequests;
      public List<MergeRequest> ClosedMergeRequests;
   }

   /// <summary>
   /// Implements periodic checks for updates of Merge Requests and their Commits
   /// </summary>
   public class WorkflowUpdateChecker : IProjectWatcher
   {
      internal WorkflowUpdateChecker(UserDefinedSettings settings, UpdateOperator updateOperator,
         Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke)
      {
         Settings = settings;
         Settings.PropertyChanged += (sender, property) =>
         {
            if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
               Trace.TraceInformation(String.Format("[WorkflowUpdateChecker] Updated cached Labels to {0}",
                  Settings.LastUsedLabels));
               Trace.TraceInformation("[WorkflowUpdateChecker] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));
            }
         };

         _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
         Trace.TraceInformation(String.Format("[WorkflowUpdateChecker] Initially cached Labels {0}",
            Settings.LastUsedLabels));
         Trace.TraceInformation("[WorkflowUpdateChecker] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();

         UpdateOperator = updateOperator;
         Workflow = workflow;
         Workflow.PostSwitchProject += async (state, _) =>
         {
            // On initial update we need to create caches.
            // When files are not listed in file, we updates only selected project and also might need to create caches.
            if (isInitialUpdate() || !areProjectsListedInFile(state))
            {
               await doUpdate();
            }
         };
      }

      public event Action<MergeRequestUpdates> OnUpdate;
      public event Action<List<ProjectUpdate>> OnProjectUpdate;

      private struct TwoListDifference<T>
      {
         public List<T> FirstOnly;
         public List<T> SecondOnly;
         public List<T> Common;
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         await doUpdate();
      }

      async public Task doUpdate()
      {
         MergeRequestUpdates updates;

         // Save current state because it may be changed while we're awaiting things
         WorkflowState state = Workflow.State;
         try
         {
            TwoListDifference<MergeRequest> diff = await getMergeRequestDiffAsync(state);
            updates = await getMergeRequestUpdatesAsync(state, diff);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
            return;
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Found: New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         // Need to gather projects before we filter out some MRs
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();
         projectUpdates.AddRange(getProjectUpdates(state, updates.NewMergeRequests));
         projectUpdates.AddRange(getProjectUpdates(state, updates.UpdatedMergeRequests));

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering New MR");
         applyLabelFilter(state, updates.NewMergeRequests);
         traceUpdates(updates.NewMergeRequests, "Filtered New");

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering Updated MR");
         applyLabelFilter(state, updates.UpdatedMergeRequests);
         traceUpdates(updates.UpdatedMergeRequests, "Filtered Updated");

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering Closed MR");
         applyLabelFilter(state, updates.ClosedMergeRequests);
         traceUpdates(updates.ClosedMergeRequests, "Filtered Closed");

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Filtered : New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         if (updates.NewMergeRequests.Count > 0
          || updates.UpdatedMergeRequests.Count > 0
          || updates.ClosedMergeRequests.Count > 0)
         {
            OnUpdate?.Invoke(updates);
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Updating {0} projects", projectUpdates.Count));
         if (projectUpdates.Count > 0)
         {
            OnProjectUpdate?.Invoke(projectUpdates);
         }
      }

      /// <summary>
      /// Remove merge requests that don't match Label Filter from the passed list
      /// </summary>
      private void applyLabelFilter(WorkflowState state, List<MergeRequest> mergeRequests)
      {
         if (!Settings.CheckedLabelsFilter)
         {
            Debug.WriteLine("[WorkflowUpdateChecker] Label Filter is off");
            return;
         }

         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            MergeRequest mergeRequest = mergeRequests[iMergeRequest];
            if (_cachedLabels.Intersect(mergeRequest.Labels).Count() == 0)
            {
               Debug.WriteLine(String.Format(
                  "[WorkflowUpdateChecker] Merge request {0} from project {1} does not match labels",
                     mergeRequest.Title, getMergeRequestProjectName(mergeRequest)));

               mergeRequests.RemoveAt(iMergeRequest);
            }
         }
      }

      /// <summary>
      /// Calculate difference between current list of merge requests at GitLab and current list in the Workflow
      /// </summary>
      async private Task<TwoListDifference<MergeRequest>> getMergeRequestDiffAsync(WorkflowState state)
      {
         TwoListDifference<MergeRequest> diff = new TwoListDifference<MergeRequest>
         {
            FirstOnly = new List<MergeRequest>(),
            SecondOnly = new List<MergeRequest>(),
            Common = new List<MergeRequest>()
         };

         if (state.HostName == null)
         {
            Debug.WriteLine("[WorkflowUpdateChecker] Host name is null");
            return diff;
         }

         List<Project> projectsToCheck = Tools.Tools.LoadProjectsFromFile(state.HostName);
         if (projectsToCheck == null && state.Project.Path_With_Namespace != null)
         {
            projectsToCheck = new List<Project>
            {
               state.Project
            };
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking {0} projects", (projectsToCheck?.Count ?? 0)));

         if (projectsToCheck == null)
         {
            return diff;
         }

         foreach (var project in projectsToCheck)
         {
            _cachedProjectNames[project.Id] = project.Path_With_Namespace;

            List<MergeRequest> previouslyCachedMergeRequests =
               _cachedMergeRequests.ContainsKey(project.Id) ? _cachedMergeRequests[project.Id] : null;

            try
            {
               await cacheMergeRequestsAsync(state, project.Id);
            }
            catch (OperatorException ex)
            {
               ExceptionHandlers.Handle(ex, String.Format(
                  "Cannot load merge requests for project {0}, skipping it", project.Path_With_Namespace));
               continue;
            }

            Debug.Assert(_cachedMergeRequests.ContainsKey(project.Id));

            if (previouslyCachedMergeRequests != null)
            {
               List<MergeRequest> newCachedMergeRequests = _cachedMergeRequests[project.Id];
               diff.FirstOnly.AddRange(previouslyCachedMergeRequests.Except(newCachedMergeRequests).ToList());
               diff.SecondOnly.AddRange(newCachedMergeRequests.Except(previouslyCachedMergeRequests).ToList());
               diff.Common.AddRange(previouslyCachedMergeRequests.Intersect(newCachedMergeRequests).ToList());
            }
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      async private Task<MergeRequestUpdates> getMergeRequestUpdatesAsync(WorkflowState state,
         TwoListDifference<MergeRequest> diff)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<MergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         foreach (MergeRequest mergeRequest in updates.NewMergeRequests)
         {
            await cacheCommitsAsync(state, mergeRequest);
         }

         foreach (MergeRequest mergeRequest in diff.Common)
         {
            DateTime? previouslyCachedCommitTimestamp = _cachedCommits.ContainsKey(mergeRequest.Id) ?
               _cachedCommits[mergeRequest.Id] : new Nullable<DateTime>();

            await cacheCommitsAsync(state, mergeRequest);

            Debug.Assert(_cachedCommits.ContainsKey(mergeRequest.Id));

            if (previouslyCachedCommitTimestamp != null)
            {
               DateTime newCachedCommitTimestamp = _cachedCommits[mergeRequest.Id];
               if (newCachedCommitTimestamp > previouslyCachedCommitTimestamp)
               {
                  updates.UpdatedMergeRequests.Add(mergeRequest);
               }
            }
         }

         foreach (MergeRequest mergeRequest in updates.ClosedMergeRequests)
         {
            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] Removing commits of merge request {0} (project {1}) from cache",
                  mergeRequest.IId, getMergeRequestProjectName(mergeRequest)));

            _cachedCommits.Remove(mergeRequest.Id);
         }

         return updates;
      }

      /// <summary>
      /// Load merge requests from GitLab and cache them
      /// </summary>
      async private Task cacheMergeRequestsAsync(WorkflowState state, int projectId)
      {
         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking merge requests for project {0} (id {1})",
            _cachedProjectNames[projectId], projectId));

         List<MergeRequest> mergeRequests =
            await UpdateOperator.GetMergeRequestsAsync(state.HostName, _cachedProjectNames[projectId]);

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] This project has {0} merge requests at GitLab",
            mergeRequests.Count));

         if (_cachedMergeRequests.ContainsKey(projectId))
         {
            List<MergeRequest> previouslyCachedMergeRequests = _cachedMergeRequests[projectId];

            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] {0} merge requests for this project were cached before",
                  previouslyCachedMergeRequests.Count));

            Debug.WriteLine("[WorkflowUpdateChecker] Updating cached merge requests for this project");

            _cachedMergeRequests[projectId] = mergeRequests;
         }
         else
         {
            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] Merge requests for this project were not cached before"));
            Debug.WriteLine("[WorkflowUpdateChecker] Caching them now");

            _cachedMergeRequests[projectId] = mergeRequests;

            foreach (MergeRequest mergeRequest in mergeRequests)
            {
               await cacheCommitsAsync(state, mergeRequest);
            }
         }
      }

      /// <summary>
      /// Load commits from GitLab and cache them
      /// </summary>
      async private Task cacheCommitsAsync(WorkflowState state, MergeRequest mergeRequest)
      {
         Debug.WriteLine(String.Format(
            "[WorkflowUpdateChecker] Checking commits for merge request {0} from project {1}",
               mergeRequest.IId, getMergeRequestProjectName(mergeRequest)));

         MergeRequestDescriptor mrd = new MergeRequestDescriptor
            {
               HostName = state.HostName,
               ProjectName = getMergeRequestProjectName(mergeRequest),
               IId = mergeRequest.IId
            };

         Commit latestCommit = await UpdateOperator.GetLatestCommitAsync(mrd);

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] This merge request has commit with Created_At={0}",
            latestCommit.Created_At));

         if (_cachedCommits.ContainsKey(mergeRequest.Id))
         {
            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] Previously cached commit timestamp for this merge request is {0}",
                  _cachedCommits[mergeRequest.Id]));

            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Updating cached commits for this merge request"));

            _cachedCommits[mergeRequest.Id] = latestCommit.Created_At;
         }
         else
         {
            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] Commits for this merge request were not cached before"));
            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Caching them now"));

            _cachedCommits[mergeRequest.Id] = latestCommit.Created_At;
         }
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates(WorkflowState state, List<MergeRequest> mergeRequests)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         DateTime latestChange = DateTime.MinValue;
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            string projectName = getMergeRequestProjectName(mergeRequest);
            for (int iUpdate = projectUpdates.Count - 1; iUpdate >= 0; --iUpdate)
            {
               if (projectUpdates[iUpdate].ProjectName == projectName)
               {
                  projectUpdates.RemoveAt(iUpdate);
               }
            }

            latestChange = _cachedCommits[mergeRequest.Id] > latestChange ?
               _cachedCommits[mergeRequest.Id] : latestChange;

            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = state.HostName,
                  ProjectName = getMergeRequestProjectName(mergeRequest),
                  LatestChange = latestChange
               });
         }

         return projectUpdates;
      }

      /// <summary>
      /// Find a project name for a passed merge request
      /// </summary>
      private string getMergeRequestProjectName(MergeRequest mergeRequest)
      {
         if (_cachedProjectNames.ContainsKey(mergeRequest.Project_Id))
         {
            return _cachedProjectNames[mergeRequest.Project_Id];
         }

         return String.Empty;
      }

      /// <summary>
      /// Debug trace
      /// </summary>
      private void traceUpdates(List<MergeRequest> mergeRequests, string name)
      {
         if (mergeRequests.Count == 0)
         {
            return;
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] {0} Merge Requests:", name));

         foreach (MergeRequest mr in mergeRequests)
         {
            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] IId: {0}, Title: {1}", mr.IId, mr.Title));
         }
      }

      private bool isInitialUpdate()
      {
         return _cachedMergeRequests.Count == 0 && _cachedCommits.Count == 0;
      }

      private bool areProjectsListedInFile(WorkflowState state)
      {
         return state.HostName != null && Tools.Tools.LoadProjectsFromFile(state.HostName) != null;
      }

      private Workflow.Workflow Workflow { get; }
      private UpdateOperator UpdateOperator { get; }
      private UserDefinedSettings Settings { get; }

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 60000 // ms
         };

      private List<string> _cachedLabels;

      // maps unique project id to project's Path with Namespace property
      private readonly Dictionary<int, string> _cachedProjectNames = new Dictionary<int, string>();

      // maps unique project id to list of merge requests
      private readonly Dictionary<int, List<MergeRequest>> _cachedMergeRequests =
         new Dictionary<int, List<MergeRequest>>();

      // maps unique Merge Request Id (not IId) to a timestamp of its latest commit
      private readonly Dictionary<int, DateTime> _cachedCommits = new Dictionary<int, DateTime>();
   }
}

