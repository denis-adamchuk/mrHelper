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
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   public class RevisionCacher
   {
      public RevisionCacher(Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties settings, Func<ProjectKey, IGitRepository> getGitRepository,
         IMergeRequestProvider mergeRequestProvider)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            if (_latestChanges?.Count > 0)
            {
               Trace.TraceInformation(String.Format("[RevisionCacher] Unsubscribing from {0} Git Repos",
                  _latestChanges.Count()));

               _latestChanges.Keys.ToList().ForEach(x => x.Updated -= onGitRepositoryUpdated);
               _latestChanges.Keys.ToList().ForEach(x => x.Disposed -= onGitRepositoryDisposed);
               _latestChanges.Clear();
            }

            // TODO Current version supports updates of projects of the most recent loaded host
            if (_latestChanges == null
             || _latestChanges.Count == 0
             || _latestChanges.Keys.First().HostName != hostname)
            {
               _latestChanges = new Dictionary<IGitRepository, DateTime>();
               foreach (Project project in projects)
               {
                  ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
                  IGitRepository repo = getGitRepository(key);
                  if (repo != null)
                  {
                     _latestChanges.Add(repo, DateTime.MinValue);
                  }
               }

               Trace.TraceInformation(String.Format("[RevisionCacher] Subscribing to {0} Git Repos",
                  _latestChanges.Count()));
               _latestChanges.Keys.ToList().ForEach(x => x.Updated += onGitRepositoryUpdated);
               _latestChanges.Keys.ToList().ForEach(x => x.Disposed += onGitRepositoryDisposed);
            }
         };

         _synchronizeInvoke = synchronizeInvoke;
         _versionManager = new VersionManager(settings);
         _mergeRequestProvider = mergeRequestProvider;
      }

      private void onGitRepositoryUpdated(IGitRepository gitRepository, DateTime latestChange)
      {
         if (_latestChanges == null || !_latestChanges.ContainsKey(gitRepository))
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               ProjectKey projectKey = new ProjectKey
               {
                  HostName = gitRepository.HostName,
                  ProjectName = gitRepository.ProjectName
               };
               DateTime prevLatestChange = _latestChanges[gitRepository];

               foreach (MergeRequest mergeRequest in _mergeRequestProvider.GetMergeRequests(projectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = projectKey, IId = mergeRequest.IId };
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
                           "[RevisionCacher] Found new version of MR with IId={0} (created at {1}). "
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
                           "[RevisionCacher] Processing merge request: Host={0}, Project={1}, IId={2}. Versions: {3}",
                           mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId, newVersionsDetailed.Count));

                        gatherArguments(newVersionsDetailed,
                           out HashSet<GitDiffArguments> diffArgs,
                           out HashSet<GitRevisionArguments> revisionArgs,
                           out HashSet<GitListOfRenamesArguments> renamesArgs);

                        try
                        {
                           await doCacheAsync(gitRepository, diffArgs, revisionArgs, renamesArgs);
                        }
                        catch (GitRepositoryDisposedException ex)
                        {
                           ExceptionHandlers.Handle(ex, "GitRepository disposed");
                           break;
                        }

                        Trace.TraceInformation(String.Format(
                           "[RevisionCacher] Processing merge request with IId={0}."
                         + "Cached git results: {1} git diff, {2} git show, {3} git rename",
                           mrk.IId, diffArgs.Count, revisionArgs.Count, renamesArgs.Count));
                     }
                  }
                  catch (VersionManagerException)
                  {
                     // already handled
                  }
               }
               _latestChanges[gitRepository] = latestChange;
            }), null);
      }

      private void onGitRepositoryDisposed(IGitRepository repo)
      {
         repo.Disposed -= onGitRepositoryDisposed;
         repo.Updated -= onGitRepositoryUpdated;
         _latestChanges.Remove(repo);
      }

      private void gatherArguments(IEnumerable<Version> versions,
         out HashSet<GitDiffArguments> diffArgs,
         out HashSet<GitRevisionArguments> revisionArgs,
         out HashSet<GitListOfRenamesArguments> renamesArgs)
      {
         diffArgs = new HashSet<GitDiffArguments>();
         revisionArgs = new HashSet<GitRevisionArguments>();
         renamesArgs = new HashSet<GitListOfRenamesArguments>();

         foreach (Version version in versions)
         {
            if (version.Diffs.Count() > MaxDiffsInVersion)
            {
               Trace.TraceWarning(String.Format(
                  "[RevisionCacher] Number of diffs in version {0} is {1}. It exceeds {2} and will be truncated",
                  version.Id, version.Diffs.Count(), MaxDiffsInVersion));
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

               renamesArgs.Add(new GitListOfRenamesArguments
               {
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });
            }
         }
      }

      async private static Task doCacheAsync(IGitRepository gitRepository,
         HashSet<GitDiffArguments> diffArgs,
         HashSet<GitRevisionArguments> revisionArgs,
         HashSet<GitListOfRenamesArguments> renamesArgs)
      {
         await doCacheSingleSetAsync(diffArgs, x => gitRepository.DiffAsync(x));
         await doCacheSingleSetAsync(revisionArgs, x => gitRepository.ShowFileByRevisionAsync(x));
         await doCacheSingleSetAsync(renamesArgs, x => gitRepository.GetListOfRenamesAsync(x));
      }

      async private static Task doCacheSingleSetAsync<T>(HashSet<T> args, Func<T, Task<IEnumerable<string>>> func)
      {
         int maxGitInParallel = 5;

         int remaining = args.Count;
         while (remaining > 0)
         {
            IEnumerable<Task<IEnumerable<string>>> tasks = args
               .Skip(args.Count - remaining)
               .Take(maxGitInParallel)
               .Select(x => func(x));
            remaining -= maxGitInParallel;
            try
            {
               await Task.WhenAll(tasks);
            }
            catch (GitOperationException)
            {
               // already handled
            }
         }
      }

      private Dictionary<IGitRepository, DateTime> _latestChanges;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionManager _versionManager;
      private readonly IMergeRequestProvider _mergeRequestProvider;

      private static int MaxDiffsInVersion = 200;
   }
}

