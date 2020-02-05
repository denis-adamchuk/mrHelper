using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using mrHelper.GitClient;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   internal class GitDataUpdater
   {
      internal GitDataUpdater(Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties settings, Func<ProjectKey, Task<ILocalGitRepository>> getLocalGitRepository,
         IMergeRequestProvider mergeRequestProvider)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            synchronizeInvoke.BeginInvoke(new Action(
               async () =>
            {
               if (_latestChanges?.Count > 0)
               {
                  Trace.TraceInformation(String.Format(
                     "[LocalGitRepositoryDataUpdater] Unsubscribing from {0} Git Repos", _latestChanges.Count()));

                  _latestChanges.Keys.ToList().ForEach(x => x.Updated -= onLocalGitRepositoryUpdated);
                  _latestChanges.Keys.ToList().ForEach(x => x.Disposed -= onLocalGitRepositoryDisposed);
                  _latestChanges.Clear();
               }

               // TODO Current version supports updates of projects of the most recent loaded host
               if (_latestChanges == null
                || _latestChanges.Count == 0
                || _latestChanges.Keys.First().ProjectKey.HostName != hostname)
               {
                  _latestChanges = new Dictionary<ILocalGitRepository, DateTime>();
                  foreach (Project project in projects)
                  {
                     ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
                     ILocalGitRepository repo = await getLocalGitRepository(key);
                     if (repo != null)
                     {
                        _latestChanges.Add(repo, DateTime.MinValue);
                     }
                  }

                  Trace.TraceInformation(String.Format("[LocalGitRepositoryDataUpdater] Subscribing to {0} Git Repos",
                     _latestChanges.Count()));
                  _latestChanges.Keys.ToList().ForEach(x => x.Updated += onLocalGitRepositoryUpdated);
                  _latestChanges.Keys.ToList().ForEach(x => x.Disposed += onLocalGitRepositoryDisposed);
               }
            }), null);
         };

         _synchronizeInvoke = synchronizeInvoke;
         _versionManager = new VersionManager(settings);
         _mergeRequestProvider = mergeRequestProvider;
      }

      private void onLocalGitRepositoryUpdated(ILocalGitRepository repo, DateTime latestChange)
      {
         if (_latestChanges == null || !_latestChanges.ContainsKey(repo))
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               DateTime prevLatestChange = _latestChanges[repo];

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
                           "[LocalGitRepositoryDataUpdater] Found new version of MR with IId={0} (created at {1}). "
                         + "PrevLatestChange={2}, LatestChange={3}",
                           mrk.IId,
                           newVersionDetailed.Created_At.ToLocalTime().ToString(),
                           prevLatestChange.ToLocalTime().ToString(),
                           latestChange.ToLocalTime().ToString()));
                        newVersionsDetailed.Add(newVersionDetailed);
                     }

                     if (newVersionsDetailed.Count > 0)
                     {
                        Trace.TraceInformation(String.Format(
                           "[LocalGitRepositoryDataUpdater] Start processing of merge request: "
                         + "Host={0}, Project={1}, IId={2}. Versions: {3}",
                           mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId, newVersionsDetailed.Count));

                        gatherArguments(newVersionsDetailed,
                           out HashSet<GitDiffArguments> diffArgs,
                           out HashSet<GitRevisionArguments> revisionArgs,
                           out HashSet<GitNumStatArguments> renamesArgs);

                        await doCacheAsync(repo, diffArgs, revisionArgs, renamesArgs);

                        Trace.TraceInformation(String.Format(
                           "[LocalGitRepositoryDataUpdater] Finished processing of merge request with IId={0}. "
                         + "Cached git results: {1} git diff, {2} git show, {3} git rename",
                           mrk.IId, diffArgs.Count, revisionArgs.Count, renamesArgs.Count));
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
            }), null);
      }

      private void onLocalGitRepositoryDisposed(ILocalGitRepository repo)
      {
         repo.Disposed -= onLocalGitRepositoryDisposed;
         repo.Updated -= onLocalGitRepositoryUpdated;
         _latestChanges.Remove(repo);
      }

      private void gatherArguments(IEnumerable<Version> versions,
         out HashSet<GitDiffArguments> diffArgs,
         out HashSet<GitRevisionArguments> revisionArgs,
         out HashSet<GitNumStatArguments> renamesArgs)
      {
         diffArgs = new HashSet<GitDiffArguments>();
         revisionArgs = new HashSet<GitRevisionArguments>();
         renamesArgs = new HashSet<GitNumStatArguments>();

         foreach (Version version in versions)
         {
            if (version.Diffs.Count() > MaxDiffsInVersion)
            {
               Trace.TraceWarning(String.Format(
                  "[LocalGitRepositoryDataUpdater] Number of diffs in version {0} is {1}. "
                + "It exceeds {2} and will be truncated", version.Id, version.Diffs.Count(), MaxDiffsInVersion));
            }

            foreach (Diff diff in version.Diffs.Take(MaxDiffsInVersion))
            {
               diffArgs.Add(new GitDiffArguments
               {
                  context = 0,
                  filename1 = diff.Old_Path,
                  filename2 = diff.New_Path,
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });

               diffArgs.Add(new GitDiffArguments
               {
                  context = Constants.FullContextSize,
                  filename1 = diff.Old_Path,
                  filename2 = diff.New_Path,
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });

               if (!diff.New_File)
               {
                  revisionArgs.Add(new GitRevisionArguments
                  {
                     filename = diff.Old_Path,
                     sha = version.Base_Commit_SHA
                  });
               }

               if (!diff.Deleted_File)
               {
                  revisionArgs.Add(new GitRevisionArguments
                  {
                     filename = diff.New_Path,
                     sha = version.Head_Commit_SHA
                  });
               }

               renamesArgs.Add(new GitNumStatArguments
               {
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA,
                  filter = "R"
               });
            }
         }
      }

      async private static Task doCacheAsync(ILocalGitRepository repo,
         HashSet<GitDiffArguments> diffArgs,
         HashSet<GitRevisionArguments> revisionArgs,
         HashSet<GitNumStatArguments> renamesArgs)
      {
         await repo.Data.Update(diffArgs);
         await repo.Data.Update(revisionArgs);
         await repo.Data.Update(renamesArgs);
      }

      private Dictionary<ILocalGitRepository, DateTime> _latestChanges;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionManager _versionManager;
      private readonly IMergeRequestProvider _mergeRequestProvider;

      private static int MaxDiffsInVersion = 200;
   }
}

