using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;
using static mrHelper.Client.Git.Types;
using mrHelper.Client.MergeRequests;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   public class RevisionCacher
   {
      public RevisionCacher(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings, Func<ProjectKey, GitClient> getGitClient,
         IMergeRequestProvider mergeRequestProvider)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            if (_latestChanges?.Count > 0)
            {
               Trace.TraceInformation(String.Format("[RevisionCacher] Unsubscribing from {0} Git Clients",
                  _latestChanges.Count()));

               _latestChanges.Keys.ToList().ForEach(x => x.Updated -= onGitClientUpdated);
               _latestChanges.Keys.ToList().ForEach(x => x.Disposed -= onGitClientDisposed);
               _latestChanges.Clear();
            }

            // TODO Current version supports updates of projects of the most recent loaded host
            if (_latestChanges == null
             || _latestChanges.Count == 0
             || _latestChanges.Keys.First().ProjectKey.HostName != hostname)
            {
               _latestChanges = new Dictionary<GitClient, DateTime>();
               foreach (Project project in projects)
               {
                  ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
                  GitClient client = getGitClient(key);
                  if (client != null)
                  {
                     _latestChanges.Add(client, DateTime.MinValue);
                  }
               }

               Trace.TraceInformation(String.Format("[RevisionCacher] Subscribing to {0} Git Clients",
                  _latestChanges.Count()));
               _latestChanges.Keys.ToList().ForEach(x => x.Updated += onGitClientUpdated);
               _latestChanges.Keys.ToList().ForEach(x => x.Disposed += onGitClientDisposed);
            }
         };

         _synchronizeInvoke = synchronizeInvoke;
         _operator = new VersionOperator(settings);
         _mergeRequestProvider = mergeRequestProvider;
      }

      private void onGitClientUpdated(GitClient gitClient, DateTime latestChange)
      {
         if (_latestChanges == null || !_latestChanges.ContainsKey(gitClient))
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               ProjectKey projectKey = gitClient.ProjectKey;
               DateTime prevLatestChange = _latestChanges[gitClient];

               foreach (MergeRequest mergeRequest in _mergeRequestProvider.GetMergeRequests(projectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = projectKey, IId = mergeRequest.IId };
                  try
                  {
                     List<Version> newVersions  = await _operator.LoadVersions(mrk);
                     newVersions = newVersions
                        .Where(x => x.Created_At > prevLatestChange && x.Created_At <= latestChange).ToList();

                     List<Version> newVersionsDetailed = new List<Version>();
                     foreach (Version version in newVersions)
                     {
                        Version newVersionDetailed = await _operator.LoadVersion(version, mrk);
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
                           out HashSet<DiffCacheKey> diffArgs,
                           out HashSet<RevisionCacheKey> revisionArgs,
                           out HashSet<ListOfRenamesCacheKey> renamesArgs);

                        try
                        {
                           await doCacheAsync(gitClient, diffArgs, revisionArgs, renamesArgs);
                        }
                        catch (GitClientDisposedException ex)
                        {
                           ExceptionHandlers.Handle(ex, "GitClient disposed");
                           break;
                        }

                        Trace.TraceInformation(String.Format(
                           "[RevisionCacher] Processing merge request with IId={0}."
                         + "Cached git results: {1} git diff, {2} git show, {3} git rename",
                           mrk.IId, diffArgs.Count, revisionArgs.Count, renamesArgs.Count));
                     }
                  }
                  catch (OperatorException)
                  {
                     // already handled
                  }
               }
               _latestChanges[gitClient] = latestChange;
            }), null);
      }

      private void onGitClientDisposed(GitClient client)
      {
         client.Disposed -= onGitClientDisposed;
         client.Updated -= onGitClientUpdated;
         _latestChanges.Remove(client);
      }

      private void gatherArguments(List<Version> versions,
         out HashSet<DiffCacheKey> diffArgs,
         out HashSet<RevisionCacheKey> revisionArgs,
         out HashSet<ListOfRenamesCacheKey> renamesArgs)
      {
         diffArgs = new HashSet<DiffCacheKey>();
         revisionArgs = new HashSet<RevisionCacheKey>();
         renamesArgs = new HashSet<ListOfRenamesCacheKey>();

         foreach (Version version in versions)
         {
            if (version.Diffs.Count > MaxDiffsInVersion)
            {
               Trace.TraceWarning(String.Format(
                  "[RevisionCacher] Number of diffs in version {0} is {1}. It exceeds {2} and will be truncated",
                  version.Id, version.Diffs.Count, MaxDiffsInVersion));
            }

            foreach (Diff diff in version.Diffs.Take(MaxDiffsInVersion))
            {
               diffArgs.Add(new DiffCacheKey
               {
                  context = 0,
                  filename1 = diff.Old_Path,
                  filename2 = diff.New_Path,
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });

               diffArgs.Add(new DiffCacheKey
               {
                  context = mrHelper.Common.Constants.Constants.FullContextSize,
                  filename1 = diff.Old_Path,
                  filename2 = diff.New_Path,
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });

               if (!diff.New_File)
               {
                  revisionArgs.Add(new RevisionCacheKey
                  {
                     filename = diff.Old_Path,
                     sha = version.Base_Commit_SHA
                  });
               }

               if (!diff.Deleted_File)
               {
                  revisionArgs.Add(new RevisionCacheKey
                  {
                     filename = diff.New_Path,
                     sha = version.Head_Commit_SHA
                  });
               }

               renamesArgs.Add(new ListOfRenamesCacheKey
               {
                  sha1 = version.Base_Commit_SHA,
                  sha2 = version.Head_Commit_SHA
               });
            }
         }
      }

      async private static Task doCacheAsync(IGitRepository gitRepository,
         HashSet<DiffCacheKey> diffArgs,
         HashSet<RevisionCacheKey> revisionArgs,
         HashSet<ListOfRenamesCacheKey> renamesArgs)
      {
         await doCacheSingleSetAsync(diffArgs,
            x => gitRepository.DiffAsync(x.sha1, x.sha2, x.filename1, x.filename2, x.context));
         await doCacheSingleSetAsync(revisionArgs,
            x => gitRepository.ShowFileByRevisionAsync(x.filename, x.sha));
         await doCacheSingleSetAsync(renamesArgs,
            x => gitRepository.GetListOfRenamesAsync(x.sha1, x.sha2));

      }

      async private static Task doCacheSingleSetAsync<T>(HashSet<T> args, Func<T, Task<List<string>>> func)
      {
         int maxGitInParallel = 5;

         int remaining = args.Count;
         while (remaining > 0)
         {
            IEnumerable<Task<List<string>>> tasks = args
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

      private Dictionary<GitClient, DateTime> _latestChanges;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionOperator _operator;
      private readonly IMergeRequestProvider _mergeRequestProvider;

      private static int MaxDiffsInVersion = 200;
   }
}

