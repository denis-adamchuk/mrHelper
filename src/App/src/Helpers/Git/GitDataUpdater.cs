using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using mrHelper.GitClient;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   internal class GitDataUpdater
   {
      internal GitDataUpdater(Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties hostProperties, ILocalGitRepositoryFactoryAccessor factoryAccessor,
         ICachedMergeRequestProvider mergeRequestProvider, IProjectCheckerFactory projectCheckerFactory)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            synchronizeInvoke.BeginInvoke(new Action(
               () =>
            {
               if (_latestChanges.Count > 0 && _latestChanges.First().Key.ProjectKey.HostName != hostname)
               {
                  // TODO Current version supports updates of projects of the most recent loaded host
                  Trace.TraceInformation(String.Format(
                     "[GitDataUpdater] Unsubscribing from {0} Git Repos", _latestChanges.Count()));

                  _latestChanges.Keys.ToList().ForEach(x => x.Updated -= onLocalGitRepositoryUpdated);
                  _latestChanges.Keys.ToList().ForEach(x => x.Disposed -= onLocalGitRepositoryDisposed);
                  _latestChanges.Clear();
               }

               foreach (Project project in projects)
               {
                  ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
                  ILocalGitRepository repo = factoryAccessor.GetFactory()?.GetRepository(key.HostName, key.ProjectName);
                  if (repo != null && !_latestChanges.ContainsKey(repo))
                  {
                     _latestChanges.Add(repo, DateTime.MinValue);

                     Trace.TraceInformation(String.Format("[GitDataUpdater] Subscribing to Git Repo {0}/{1}",
                        repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
                     repo.Updated += onLocalGitRepositoryUpdated;
                     repo.Disposed += onLocalGitRepositoryDisposed;
                  }
               }
            }), null);
         };

         _synchronizeInvoke = synchronizeInvoke;
         _versionManager = new VersionManager(hostProperties);
         _mergeRequestProvider = mergeRequestProvider;
         _projectCheckerFactory = projectCheckerFactory;
      }

      private void onLocalGitRepositoryUpdated(ILocalGitRepository repo)
      {
         if (_latestChanges == null || !_latestChanges.ContainsKey(repo))
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
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

               DateTime prevLatestChange = _latestChanges[repo];

               // Use local project checker for the whole Project to filter out MR versions that exist at GitLab but
               // not processed by local-side so far. We don't need to update git data for them until they come.
               // When they arrive, git repository will be updated and this callback called again.
               DateTime latestChange = await _projectCheckerFactory.GetLocalProjectChecker(repo.ProjectKey).
                  GetLatestChangeTimestamp();

               foreach (MergeRequest mergeRequest in _mergeRequestProvider.GetMergeRequests(repo.ProjectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = repo.ProjectKey, IId = mergeRequest.IId };
                  try
                  {
                     IEnumerable<Version> allVersions  = await _versionManager.GetVersions(mrk);
                     IEnumerable<Version> newVersions = allVersions
                        .Where(x => x.Created_At > prevLatestChange && x.Created_At <= latestChange);

                     List<Version> newVersionsDetailed = new List<Version>();
                     foreach (Version version in newVersions)
                     {
                        Version newVersionDetailed = await _versionManager.GetVersion(version, mrk);
                        Trace.TraceInformation(String.Format(
                           "[GitDataUpdater] Found new version of MR with IId={0} (created at {1}). "
                         + "PrevLatestChange={2}, LatestChange={3}",
                           mrk.IId,
                           newVersionDetailed.Created_At.ToLocalTime().ToString(),
                           prevLatestChange.ToLocalTime().ToString(),
                           latestChange.ToLocalTime().ToString()));
                        newVersionsDetailed.Add(newVersionDetailed);
                     }

                     if (!_latestChanges.ContainsKey(repo))
                     {
                        // LocalGitRepository was removed from collection while we were caching current MR
                        break;
                     }

                     if (newVersionsDetailed.Count > 0)
                     {
                        Trace.TraceInformation(String.Format(
                           "[GitDataUpdater] Start processing of merge request: "
                         + "Host={0}, Project={1}, IId={2}. Versions: {3}",
                           mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId, newVersionsDetailed.Count));

                        gatherArguments(newVersionsDetailed,
                           out HashSet<GitDiffArguments> diffArgs,
                           out HashSet<GitShowRevisionArguments> revisionArgs);

                        await doCacheAsync(repo, diffArgs, revisionArgs);

                        Trace.TraceInformation(String.Format(
                           "[GitDataUpdater] Finished processing of merge request with IId={0}. "
                         + "Cached git results: {1} git diff, {2} git show",
                           mrk.IId, diffArgs.Count, revisionArgs.Count));
                     }
                  }
                  catch (VersionManagerException)
                  {
                     // already handled
                  }

                  if (!_latestChanges.ContainsKey(repo))
                  {
                     // LocalGitRepository was removed from collection while we were caching current MR
                     break;
                  }
               }

               if (_latestChanges.ContainsKey(repo))
               {
                  _latestChanges[repo] = latestChange;
               }

               _updating.Remove(repo);
            }), null);
      }

      private void onLocalGitRepositoryDisposed(ILocalGitRepository repo)
      {
         repo.Disposed -= onLocalGitRepositoryDisposed;
         repo.Updated -= onLocalGitRepositoryUpdated;
         _latestChanges.Remove(repo);
      }

      private void gatherArguments(IEnumerable<Version> versions,
         out HashSet<GitDiffArguments> diffArgs, out HashSet<GitShowRevisionArguments> revisionArgs)
      {
         diffArgs = new HashSet<GitDiffArguments>();
         revisionArgs = new HashSet<GitShowRevisionArguments>();

         foreach (Version version in versions)
         {
            if (version.Diffs.Count() > MaxDiffsInVersion)
            {
               Trace.TraceWarning(String.Format(
                  "[GitDataUpdater] Number of diffs in version {0} is {1}. "
                + "It exceeds {2} and will be truncated", version.Id, version.Diffs.Count(), MaxDiffsInVersion));
            }

            foreach (Diff diff in version.Diffs.Take(MaxDiffsInVersion))
            {
               diffArgs.Add(new GitDiffArguments
               {
                  Mode = GitDiffArguments.DiffMode.Context,
                  CommonArgs = new GitDiffArguments.CommonArguments
                  {
                     Sha1 = version.Base_Commit_SHA,
                     Sha2 = version.Head_Commit_SHA,
                     Filename1 = diff.Old_Path,
                     Filename2 = diff.New_Path,
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
                     Sha1 = version.Base_Commit_SHA,
                     Sha2 = version.Head_Commit_SHA,
                     Filename1 = diff.Old_Path,
                     Filename2 = diff.New_Path,
                  },
                  SpecialArgs = new GitDiffArguments.DiffContextArguments
                  {
                     Context = Constants.FullContextSize
                  }
               });

               diffArgs.Add(new GitDiffArguments
               {
                  Mode = GitDiffArguments.DiffMode.NumStat,
                  CommonArgs = new GitDiffArguments.CommonArguments
                  {
                     Sha1 = version.Base_Commit_SHA,
                     Sha2 = version.Head_Commit_SHA,
                     Filter = "R"
                  }
               });

               if (!diff.New_File)
               {
                  revisionArgs.Add(new GitShowRevisionArguments
                  {
                     Filename = diff.Old_Path,
                     Sha = version.Base_Commit_SHA
                  });
               }

               if (!diff.Deleted_File)
               {
                  revisionArgs.Add(new GitShowRevisionArguments
                  {
                     Filename = diff.New_Path,
                     Sha = version.Head_Commit_SHA
                  });
               }
            }
         }
      }

      async private static Task doCacheAsync(ILocalGitRepository repo,
         HashSet<GitDiffArguments> diffArgs, HashSet<GitShowRevisionArguments> revisionArgs)
      {
         await doBatchUpdateAsync(diffArgs, x => repo.Data?.LoadFromDisk(x));
         await doBatchUpdateAsync(revisionArgs, x => repo.Data?.LoadFromDisk(x));
      }

      async private static Task doBatchUpdateAsync<T>(IEnumerable<T> args, Func<T, Task> func)
      {
         int remaining = args.Count();
         while (remaining > 0 )
         {
            Task[] tasks = args
               .Skip(args.Count() - remaining)
               .Take(MaxGitInParallel)
               .Select(x => func(x))
               .ToArray();
            remaining -= tasks.Length;

            Task aggregateTask = Task.WhenAll(tasks);
            try
            {
               await aggregateTask;
            }
            catch (Exception)
            {
               if (aggregateTask.IsFaulted)
               {
                  foreach (Exception ex in aggregateTask.Exception.Flatten().InnerExceptions)
                  {
                     ExceptionHandlers.Handle("Cannot load data from disk", ex);
                  }
                  continue;
               }
               else
               {
                  Debug.Assert(false);
               }
            }

            await Task.Delay(InterBatchDelay);
         }
      }

      private static int MaxGitInParallel  = 5;
      private static int InterBatchDelay   = 1000; // ms

      private HashSet<ILocalGitRepository> _updating = new HashSet<ILocalGitRepository>();
      private Dictionary<ILocalGitRepository, DateTime> _latestChanges =
         new Dictionary<ILocalGitRepository, DateTime>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionManager _versionManager;
      private readonly ICachedMergeRequestProvider _mergeRequestProvider;
      private readonly IProjectCheckerFactory _projectCheckerFactory;

      private static int MaxDiffsInVersion = 200;
   }
}

