using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Pre-loads file revisions into from disk file storage into memory to speed up Discussions view rendering
   /// </summary>
   internal class GitDataUpdater : BaseGitHelper, IDisposable
   {
      internal GitDataUpdater(
         IMergeRequestCache mergeRequestCache,
         IDiscussionCache discussionCache,
         ISynchronizeInvoke synchronizeInvoke,
         ILocalCommitStorageFactory gitFactory,
         int autoUpdatePeriodMs,
         MergeRequestFilter mergeRequestFilter,
         bool isInitialUpdateNeeded)
         : base(mergeRequestCache, discussionCache, synchronizeInvoke, gitFactory)
      {
         if (autoUpdatePeriodMs < 1)
         {
            throw new ArgumentException("Bad auto-update period specified");
         }
         _mergeRequestFilter = mergeRequestFilter;

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();

         if (isInitialUpdateNeeded)
         {
            scheduleAllProjectsUpdate();
         }
      }

      public new void Dispose()
      {
         base.Dispose();

         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;
      }

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
#if DEBUG
         Trace.TraceInformation("[GitDataUpdater] Scheduling update of all repositories (on timer)");
#endif
         scheduleAllProjectsUpdate();
      }

      protected override void preUpdate(ILocalCommitStorage repo) {}

      async protected override Task doUpdate(ILocalCommitStorage repo)
      {
         IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestCache.GetMergeRequests(repo.ProjectKey)
            .Select(x => new FullMergeRequestKey(repo.ProjectKey, x))
            .Where(x => _mergeRequestFilter.DoesMatchFilter(x))
            .Select(x => new MergeRequestKey(repo.ProjectKey, x.MergeRequest.IId));

         foreach (MergeRequestKey mrk in mergeRequestKeys)
         {
            await updateGitDataForSingleMergeRequest(mrk, repo);
         }
      }

      protected override void onProjectUpdate(ProjectKey projectKey) {}

      async private Task updateGitDataForSingleMergeRequest(MergeRequestKey mrk, ILocalCommitStorage repo)
      {
         if (repo.Git == null)
         {
            return;
         }

         DateTime prevLatestChange = getLatestChange(mrk);
         IEnumerable<Discussion> newDiscussions = await loadNewDiscussionsAsync(mrk, prevLatestChange);

         int totalCount = newDiscussions?.Count() ?? 0;
         if (totalCount == 0 || repo.Git == null)
         {
            return;
         }

         newDiscussions = newDiscussions.Take(MaxDiscussionsInMergeRequest);

         DateTime latestChange =
            newDiscussions
            .OrderByDescending(x => x.Notes.First().Updated_At)
            .First().Notes.First().Updated_At;

#if DEBUG
         Trace.TraceInformation(String.Format(
            "[GitDataUpdater] Start processing merge request: "
          + "Host={0}, Project={1}, IId={2}. New Discussions: {3}. LatestChange = {4}",
            mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId,
            totalCount, TimeUtils.DateTimeToString(latestChange)));
#endif
         if (newDiscussions.Count() > totalCount)
         {
            Trace.TraceWarning("[GitDataUpdater] Number of discussions exceeds the limit");
         }

         gatherArguments(newDiscussions,
            out HashSet<GitDiffArguments> diffArgs,
            out HashSet<GitShowRevisionArguments> revisionArgs);
         await fetchMissingData(repo, newDiscussions);
         await doCacheAsync(repo, diffArgs, revisionArgs);

#if DEBUG
         Trace.TraceInformation(String.Format(
            "[GitDataUpdater] Finished processing merge request with IId={0}. "
          + "Cached git results: {1} git diff, {2} git show",
            mrk.IId, diffArgs.Count, revisionArgs.Count));
#endif
         setLatestChange(mrk, latestChange);
      }

      async private Task<IEnumerable<Discussion>> loadNewDiscussionsAsync(MergeRequestKey mrk,
         DateTime prevLatestChange)
      {
         if (_discussionCache == null)
         {
            return null;
         }

         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await _discussionCache.LoadDiscussions(mrk);
         }
         catch (DiscussionCacheException ex)
         {
            ExceptionHandlers.Handle("Cannot load discussions from GitLab", ex);
            return null;
         }

         if (discussions == null)
         {
            return null;
         }

         return discussions
               .Where(x => x.Notes.Any()
                       && !x.Notes.First().System
                       && x.Notes.First().Type == "DiffNote"
                       && x.Notes.First().Updated_At > prevLatestChange);
      }

      private void gatherArguments(IEnumerable<Discussion> discussions,
         out HashSet<GitDiffArguments> diffArgs, out HashSet<GitShowRevisionArguments> revisionArgs)
      {
         diffArgs = new HashSet<GitDiffArguments>();
         revisionArgs = new HashSet<GitShowRevisionArguments>();

         foreach (Discussion discussion in discussions)
         {
            Debug.Assert(discussion.Notes != null
                     &&  discussion.Notes.Any()
                     && !discussion.Notes.First().System
                     &&  discussion.Notes.First().Type == "DiffNote");

            Core.Matching.DiffPosition position =
               PositionConverter.Convert(discussion.Notes.First().Position);
            if (!Core.Context.Helpers.IsValidPosition(position))
            {
               continue;
            }

            diffArgs.Add(new GitDiffArguments
            (
               GitDiffArguments.DiffMode.Context,
               new GitDiffArguments.CommonArguments
               (
                  position.Refs.LeftSHA,
                  position.Refs.RightSHA,
                  position.LeftPath,
                  position.RightPath,
                  null
               ),
               new GitDiffArguments.DiffContextArguments(0)
            ));

            diffArgs.Add(new GitDiffArguments
            (
               GitDiffArguments.DiffMode.Context,
               new GitDiffArguments.CommonArguments
               (
                  position.Refs.LeftSHA,
                  position.Refs.RightSHA,
                  position.LeftPath,
                  position.RightPath,
                  null
               ),
               new GitDiffArguments.DiffContextArguments(Constants.FullContextSize)
            ));

            if (Core.Context.Helpers.IsRightSidePosition(position))
            {
               revisionArgs.Add(new GitShowRevisionArguments(position.RightPath, position.Refs.RightSHA));
            }
            else
            {
               Debug.Assert(Core.Context.Helpers.IsLeftSidePosition(position));
               revisionArgs.Add(new GitShowRevisionArguments(position.LeftPath, position.Refs.LeftSHA));
            }
         }
      }

      async private static Task fetchMissingData(ILocalCommitStorage repo, IEnumerable<Discussion> discussions)
      {
         // On timer update we may got into situation when not all SHA are already fetched.
         // For example, if we just cloned the repository and still in progress of initial
         // fetching. A simple solution is to request updates using CommitBasedContext.
         // Update() call will return from `await` only when all ongoing updates within
         // the project are finished.
         bool finished = repo?.Updater == null;
         repo?.Updater?.RequestUpdate(new DiscussionBasedContextProvider(discussions), () => finished = true);
         await TaskUtils.WhileAsync(() => !finished);
      }

      async private static Task doCacheAsync(ILocalCommitStorage repo,
         HashSet<GitDiffArguments> diffArgs, HashSet<GitShowRevisionArguments> revisionArgs)
      {
         await TaskUtils.RunConcurrentFunctionsAsync(diffArgs,
            async x =>
            {
               if (repo.Git != null)
               {
                  await repo.Git.FetchAsync(x);
               }
            },
            () => Constants.GitDataUpdaterBatchLimits, () => repo.Git == null);

         await TaskUtils.RunConcurrentFunctionsAsync(revisionArgs,
            async x =>
            {
               if (repo.Git != null)
               {
                  await repo.Git.FetchAsync(x);
               }
            },
            () => Constants.GitDataUpdaterBatchLimits, () => repo.Git == null);
      }

      private DateTime getLatestChange(MergeRequestKey mrk)
      {
         return _latestChanges.ContainsKey(mrk) ? _latestChanges[mrk] : DateTime.MinValue;
      }

      private void setLatestChange(MergeRequestKey mrk, DateTime dateTime)
      {
         _latestChanges[mrk] = dateTime;
      }

      private readonly Dictionary<MergeRequestKey, DateTime> _latestChanges =
         new Dictionary<MergeRequestKey, DateTime>();

      private readonly MergeRequestFilter _mergeRequestFilter;
      private System.Timers.Timer _timer;

      private static readonly int MaxDiscussionsInMergeRequest = 400;
   }
}

