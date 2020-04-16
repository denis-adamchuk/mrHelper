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
using mrHelper.GitClient;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Discussions;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache to speed up Discussions view rendering
   /// </summary>
   internal class GitDataUpdater : IDisposable
   {
      internal GitDataUpdater(IWorkflowEventNotifier workflowEventNotifier, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties hostProperties, ILocalGitRepositoryFactoryAccessor factoryAccessor,
         ICachedMergeRequestProvider mergeRequestProvider, IProjectCheckerFactory projectCheckerFactory,
         DiscussionManager discussionManager, bool createMissingCommits, int autoUpdatePeriodMs,
         MergeRequestFilter mergeRequestFilter)
      {
         if (autoUpdatePeriodMs < 1)
         {
            throw new ArgumentException("Bad auto-update period specified");
         }

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.LoadedProjects += onLoadedProjects;

         _factoryAccessor = factoryAccessor;
         _hostProperties = hostProperties;
         _mergeRequestProvider = mergeRequestProvider;
         _projectCheckerFactory = projectCheckerFactory;
         _discussionManager = discussionManager;
         _createMissingCommits = createMissingCommits;
         _mergeRequestFilter = mergeRequestFilter;

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _workflowEventNotifier.LoadedProjects -= onLoadedProjects;

         _timer?.Stop();
         _timer?.Dispose();

         unsubscribeFromAll();
      }

      private void onLocalGitRepositoryUpdated(ILocalGitRepository repo)
      {
         Trace.TraceInformation(String.Format(
            "[GitDataUpdater] Scheduling update of {0} project repository", repo.ProjectKey.ProjectName));

         scheduleUpdate(repo);
      }

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         updateAll();
      }

      private void updateAll()
      {
         Trace.TraceInformation("[GitDataUpdater] Scheduling update of all repositories (on timer)");

         foreach (ILocalGitRepository repo in _connected)
         {
            scheduleUpdate(repo);
         }
      }

      private void scheduleUpdate(ILocalGitRepository repo)
      {
         _timer.SynchronizingObject.BeginInvoke(new Action(async () => await doUpdateGitRepository(repo)), null);
      }

      async private Task doUpdateGitRepository(ILocalGitRepository repo)
      {
         Debug.Assert(isConsistentState(repo));

         ILocalGitRepositoryData data = repo.Data;
         if (data == null)
         {
            Trace.TraceWarning(String.Format(
               "[GitDataUpdater] Update failed. LocalGitRepositoryData is not ready (Host={0}, Project={1})",
               repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
            return;
         }

         if (!_updating.Add(repo))
         {
            return;
         }

         try
         {
            IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestProvider.GetMergeRequests(repo.ProjectKey)
               .Where(x => _mergeRequestFilter.DoesMatchFilter(x))
               .Select(x => new MergeRequestKey
               {
                  ProjectKey = repo.ProjectKey,
                  IId = x.IId
               });

            foreach (MergeRequestKey mrk in mergeRequestKeys)
            {
               await updateGitDataForSingleMergeRequest(mrk, repo);
               if (!isConnected(repo))
               {
                  // LocalGitRepository was removed from collection while we were caching data for this MR
                  break;
               }
            }
         }
         finally
         {
            _updating.Remove(repo);
            Debug.Assert(isConsistentState(repo));
         }
      }

      async private Task updateGitDataForSingleMergeRequest(MergeRequestKey mrk, ILocalGitRepository repo)
      {
         DateTime prevLatestChange = getLatestChange(mrk);

         IEnumerable<Discussion> newDiscussions =
            await loadNewDiscussionsAsync(mrk, prevLatestChange);
         if (!isConnected(repo))
         {
            // LocalGitRepository was removed from collection while we were loading discussions
            return;
         }

         int totalCount = newDiscussions?.Count() ?? 0;
         if (totalCount == 0)
         {
            return;
         }

         newDiscussions = newDiscussions.Take(MaxDiscussionsInMergeRequest);
         if (_createMissingCommits)
         {
            await createMissingCommits(newDiscussions, repo);
            if (!isConnected(repo))
            {
               // LocalGitRepository was removed from collection while we were restoring commits
               return;
            }
         }

         DateTime latestChange =
            newDiscussions
            .OrderByDescending(x => x.Notes.First().Updated_At)
            .First().Notes.First().Updated_At;

         Trace.TraceInformation(String.Format(
            "[GitDataUpdater] Start processing of merge request: "
          + "Host={0}, Project={1}, IId={2}. New Discussions: {3}. LatestChange = {4}",
            mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId,
            totalCount, latestChange.ToLocalTime().ToString()));
         if (newDiscussions.Count() > totalCount)
         {
            Trace.TraceWarning("[GitDataUpdater] Number of discussions exceeds the limit");
         }

         gatherArguments(newDiscussions,
            out HashSet<GitDiffArguments> diffArgs,
            out HashSet<GitShowRevisionArguments> revisionArgs);

         await doCacheAsync(repo, diffArgs, revisionArgs);

         Trace.TraceInformation(String.Format(
            "[GitDataUpdater] Finished processing of merge request with IId={0}. "
          + "Cached git results: {1} git diff, {2} git show",
            mrk.IId, diffArgs.Count, revisionArgs.Count));
         if (!isConnected(repo))
         {
            // LocalGitRepository was removed from collection while we were caching data
            return;
         }

         setLatestChange(mrk, latestChange);
      }

      async private Task<IEnumerable<Discussion>> loadNewDiscussionsAsync(MergeRequestKey mrk,
         DateTime prevLatestChange)
      {
         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await _discussionManager.GetDiscussionsAsync(mrk);
         }
         catch (DiscussionManagerException ex)
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
                       && x.Notes.First().Updated_At > prevLatestChange)
               .ToArray();
      }

      private void onLocalGitRepositoryDisposed(ILocalGitRepository repo)
      {
         unsubscribeFromOne(repo);
      }

      async private Task createMissingCommits(IEnumerable<Discussion> discussions, ILocalGitRepository repo)
      {
         if (discussions == null || repo == null)
         {
            return;
         }

         IEnumerable<string> headShaFromDiscussions = discussions
            .Select(x => x.Notes.First().Position.Head_SHA).Distinct();
         if (headShaFromDiscussions.Any())
         {
            CommitChainCreator commitChainCreator = new CommitChainCreator(
               _hostProperties, null, null, null, _timer.SynchronizingObject, repo, headShaFromDiscussions);
            await commitChainCreator.CreateChainAsync();
         }
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

            diffArgs.Add(new GitDiffArguments
            {
               Mode = GitDiffArguments.DiffMode.Context,
               CommonArgs = new GitDiffArguments.CommonArguments
               {
                  Sha1 = position.Refs.LeftSHA,
                  Sha2 = position.Refs.RightSHA,
                  Filename1 = position.LeftPath,
                  Filename2 = position.RightPath,
               },
               SpecialArgs = new GitDiffArguments.DiffContextArguments
               {
                  Context = 0
               }
            });

            diffArgs.Add(new GitDiffArguments
            {
               Mode = GitDiffArguments.DiffMode.Context,
               CommonArgs = new GitDiffArguments.CommonArguments
               {
                  Sha1 = position.Refs.LeftSHA,
                  Sha2 = position.Refs.RightSHA,
                  Filename1 = position.LeftPath,
                  Filename2 = position.RightPath,
               },
               SpecialArgs = new GitDiffArguments.DiffContextArguments
               {
                  Context = Constants.FullContextSize
               }
            });

            // the same condition as in EnhancedContextMaker and SimpleContextMaker,
            // which are consumers of the cache
            if (position.RightLine != null)
            {
               revisionArgs.Add(new GitShowRevisionArguments
               {
                  Filename = position.RightPath,
                  Sha = position.Refs.RightSHA
               });
            }
            else
            {
               revisionArgs.Add(new GitShowRevisionArguments
               {
                  Filename = position.LeftPath,
                  Sha = position.Refs.LeftSHA
               });
            }
         }
      }

      async private static Task doCacheAsync(ILocalGitRepository repo,
         HashSet<GitDiffArguments> diffArgs, HashSet<GitShowRevisionArguments> revisionArgs)
      {
         await TaskUtils.RunConcurrentFunctionsAsync(diffArgs, x => repo.Data?.LoadFromDisk(x),
            Constants.GitInstancesInBatch, Constants.GitInstancesInterBatchDelay, null);
         await TaskUtils.RunConcurrentFunctionsAsync(revisionArgs, x => repo.Data?.LoadFromDisk(x),
            Constants.GitInstancesInBatch, Constants.GitInstancesInterBatchDelay, null);
      }

      private void onLoadedProjects(string hostname, IEnumerable<Project> projects)
      {
         _timer.SynchronizingObject.BeginInvoke(new Action(
            async () =>
         {
            foreach (Project project in projects)
            {
               ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
               ILocalGitRepository repo =
                  (await _factoryAccessor.GetFactory())?.GetRepository(key.HostName, key.ProjectName);
               if (repo != null && !isConnected(repo))
               {
                  _connected.Add(repo);

                  Trace.TraceInformation(String.Format("[GitDataUpdater] Subscribing to Git Repo {0}/{1}",
                     repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
                  repo.Updated += onLocalGitRepositoryUpdated;
                  repo.Disposed += onLocalGitRepositoryDisposed;
               }
            }
         }), null);
      }

      private void unsubscribeFromOne(ILocalGitRepository repo)
      {
         repo.Disposed -= onLocalGitRepositoryDisposed;
         repo.Updated -= onLocalGitRepositoryUpdated;
         _connected.Remove(repo);

         IEnumerable<MergeRequestKey> toRemove = _latestChanges.Keys.Where(x => x.ProjectKey.Equals(repo.ProjectKey));
         foreach (MergeRequestKey key in toRemove.ToArray())
         {
            _latestChanges.Remove(key);
         }

         Debug.Assert(isConsistentState(repo));
      }

      private void unsubscribeFromAll()
      {
         foreach (ILocalGitRepository repo in _connected)
         {
            repo.Updated -= onLocalGitRepositoryUpdated;
            repo.Disposed -= onLocalGitRepositoryDisposed;
         }
         _connected.Clear();
         _latestChanges.Clear();
      }

      private bool isConnected(ILocalGitRepository repo)
      {
         return _connected.Contains(repo);
      }

      private bool isConsistentState(ILocalGitRepository repo)
      {
         // We expect that if a repository is not connected, we no longer store timestamps of its MRs
         return _connected.Contains(repo) || !_latestChanges.Any(x => x.Key.ProjectKey.Equals(repo.ProjectKey));
      }

      private DateTime getLatestChange(MergeRequestKey mrk)
      {
         return _latestChanges.ContainsKey(mrk) ? _latestChanges[mrk] : DateTime.MinValue;
      }

      private void setLatestChange(MergeRequestKey mrk, DateTime dateTime)
      {
         _latestChanges[mrk] = dateTime;
      }

      private readonly HashSet<ILocalGitRepository> _updating = new HashSet<ILocalGitRepository>();
      private readonly HashSet<ILocalGitRepository> _connected = new HashSet<ILocalGitRepository>();
      private readonly Dictionary<MergeRequestKey, DateTime> _latestChanges =
         new Dictionary<MergeRequestKey, DateTime>();

      private readonly IHostProperties _hostProperties;
      private readonly bool _createMissingCommits;

      private readonly IProjectCheckerFactory _projectCheckerFactory;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
      private readonly ILocalGitRepositoryFactoryAccessor _factoryAccessor;
      private readonly DiscussionManager _discussionManager;

      private readonly ICachedMergeRequestProvider _mergeRequestProvider;
      private readonly MergeRequestFilter _mergeRequestFilter;

      private readonly System.Timers.Timer _timer;

      private static readonly int MaxDiscussionsInMergeRequest = 400;
   }
}

