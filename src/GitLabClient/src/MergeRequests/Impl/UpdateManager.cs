using System;
using System.ComponentModel;
using System.Collections.Generic;
using GitLabSharp.Entities;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Common;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Constants;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Manages updates
   /// </summary>
   internal class UpdateManager : IDisposable
   {
      internal event Action<IEnumerable<UserEvents.MergeRequestEvent>> OnUpdate;

      internal UpdateManager(ISynchronizeInvoke synchronizeInvoke, UpdateOperator updateOperator,
         string hostname, IEnumerable<Project> projects, WorkflowDetailsCache cache, int autoUpdatePeriodMs)
      {
         _operator = updateOperator;
         _hostname = hostname;
         _projects = projects.ToArray();
         _cache = cache;

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;

         foreach (System.Timers.Timer timer in _oneShotTimers)
         {
            timer.Stop();
            timer.Dispose();
         }
         _oneShotTimers.Clear();
      }

      public void RequestOneShotUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         foreach (int interval in intervals)
         {
            enqueueOneShotTimer(mrk, interval, onUpdateFinished);
         }
      }

      private void enqueueOneShotTimer(MergeRequestKey? mrk, int interval, Action onUpdateFinished)
      {
         if (interval < 1)
         {
            return;
         }

         System.Timers.Timer timer = new System.Timers.Timer
         {
            Interval = interval,
            AutoReset = false,
            SynchronizingObject = _timer?.SynchronizingObject
         };

         timer.Elapsed +=
            async (s, e) =>
         {
            if (String.IsNullOrEmpty(_hostname) || _projects == null)
            {
               Debug.Assert(false);
               Trace.TraceWarning("[UpdateManager] One-Shot Timer update is cancelled");
               return;
            }

            IEnumerable<UserEvents.MergeRequestEvent> updates =
               await (mrk.HasValue ? updateOneOnTimer(mrk.Value) : updateAllOnTimer());

            if (updates != null)
            {
               OnUpdate?.Invoke(updates);
            }

            onUpdateFinished?.Invoke();
            _timer?.Start();
         };
         _timer?.Stop();
         timer.Start();

         _oneShotTimers.Add(timer);
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         if (String.IsNullOrEmpty(_hostname) || _projects == null)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[UpdateManager] Auto-update is cancelled");
            return;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = await updateAllOnTimer();

         if (updates != null)
         {
            OnUpdate?.Invoke(updates);
         }
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateOneOnTimer(MergeRequestKey mrk)
      {
         if (_updating)
         {
            return null;
         }

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         try
         {
            _updating = true;
            await loadDataAndUpdateCacheAsync(mrk);
         }
         catch (UpdateException ex)
         {
            ExceptionHandlers.Handle("Cannot perform a one-shot update", ex);
            return null;
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(_hostname, _projects,
            oldDetails, _cache.Details);

         int legalUpdates = updates.Count(x => x.Labels);
         Debug.Assert(legalUpdates == 0 || legalUpdates == 1);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
               legalUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));

         return updates;
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateAllOnTimer()
      {
         if (_updating)
         {
            return null;
         }

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         try
         {
            _updating = true;

            async Task loadProject(Project project)
            {
               try
               {
                  await loadDataAndUpdateCacheAsync(_hostname, project);
               }
               catch (UpdateException ex)
               {
                  ExceptionHandlers.Handle(String.Format(
                     "Cannot update project {0}", project.Path_With_Namespace), ex);
               }
            }

            await TaskUtils.RunConcurrentFunctionsAsync(_projects, x => loadProject(x),
               Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, null);
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(_hostname, _projects,
            oldDetails, _cache.Details);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Merge Request Updates: New {0}, Updated commits {1}, Updated labels {2}, Closed {3}",
               updates.Count(x => x.New),
               updates.Count(x => x.Commits),
               updates.Count(x => x.Labels),
               updates.Count(x => x.Closed)));

         return updates;
      }

      async private Task loadDataAndUpdateCacheAsync(string hostname, Project project)
      {
         IEnumerable<MergeRequest> mergeRequests =
            await loadMergeRequestsAsync(hostname, project.Path_With_Namespace);

         Dictionary<MergeRequestKey, Version> latestVersions = new Dictionary<MergeRequestKey, Version>();
         async Task loadVersion(MergeRequest x)
         {
            ProjectKey projectKey = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
            MergeRequestKey mrk = new MergeRequestKey
            {
               ProjectKey = projectKey,
               IId = x.IId
            };

            latestVersions[mrk] = await loadLatestVersionAsync(mrk);
         }

         await TaskUtils.RunConcurrentFunctionsAsync(mergeRequests, x => loadVersion(x),
            Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, null);

         _cache.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);
         foreach (KeyValuePair<MergeRequestKey, Version> latestVersion in latestVersions)
         {
            _cache.UpdateLatestVersion(latestVersion.Key, latestVersion.Value);
         }
      }

      async private Task loadDataAndUpdateCacheAsync(MergeRequestKey mrk)
      {
         try
         {
            _cache.UpdateMergeRequest(mrk, await _operator.GetMergeRequestAsync(mrk));
         }
         catch (OperatorException ex)
         {
            throw new UpdateException(String.Format(
               "[UpdateManager] Cannot load merge request. MRK: HostName={0}, ProjectName={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId), ex);
         }
      }

      async private Task<IEnumerable<MergeRequest>> loadMergeRequestsAsync(string hostname, string projectname)
      {
         try
         {
            return await _operator.GetMergeRequestsAsync(hostname, projectname);
         }
         catch (OperatorException ex)
         {
            throw new UpdateException(String.Format(
               "Cannot load merge requests. HostName={0}, ProjectName={1}", hostname, projectname), ex);
         }
      }

      async private Task<Version> loadLatestVersionAsync(MergeRequestKey mrk)
      {
         try
         {
            return await _operator.GetLatestVersionAsync(mrk);
         }
         catch (OperatorException ex)
         {
            throw new UpdateException(String.Format(
               "[UpdateManager] Cannot load latest version. MRK: HostName={0}, ProjectName={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId), ex);
         }
      }

      private class UpdateException : ExceptionEx
      {
         internal UpdateException(string message, Exception innerException)
            : base(message, innerException)
         {
         }
      }

      private System.Timers.Timer _timer;
      private List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly WorkflowDetailsCache _cache;
      private readonly WorkflowDetailsChecker _checker = new WorkflowDetailsChecker();
      private readonly UpdateOperator _operator;

      private readonly string _hostname;
      private readonly IEnumerable<Project> _projects;

      private bool _updating; /// prevents re-entrance in timer updates
   }
}

