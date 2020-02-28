using GitLabSharp.Entities;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Client.Workflow;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiffStatisticKey = System.Int32; // Merge Request IId
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Traces git diff statistic change for all merge requests within one or more repositories
   /// </summary>
   internal class GitStatisticManager : IDisposable
   {
      internal GitStatisticManager(IWorkflowEventNotifier workflowEventNotifier, ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepositoryFactoryAccessor factoryAccessor,
         ICachedMergeRequestProvider mergeRequestProvider, IProjectCheckerFactory projectCheckerFactory)
      {
         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;

         _factoryAccessor = factoryAccessor;
         _synchronizeInvoke = synchronizeInvoke;
         _mergeRequestProvider = mergeRequestProvider;
         _projectCheckerFactory = projectCheckerFactory;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;

         foreach (KeyValuePair<ILocalGitRepository, LocalGitRepositoryStatistic> keyValuePair in _gitStatistic)
         {
            keyValuePair.Key.Updated -= onLocalGitRepositoryUpdated;
            keyValuePair.Key.Disposed -= onLocalGitRepositoryDisposed;
         }
         _gitStatistic.Clear();
      }

      /// <summary>
      /// Returns statistic for the given MR
      /// Statistic is collected for hash tags that match the last version of a merge request
      /// </summary>
      internal DiffStatistic? GetStatistic(FullMergeRequestKey fmk, out string errorMessage)
      {
         KeyValuePair<ILocalGitRepository, LocalGitRepositoryStatistic> repository2Statistic =
            _gitStatistic.SingleOrDefault(x => x.Key.ProjectKey.Equals(fmk.ProjectKey));
         if (repository2Statistic.Key == null)
         {
            errorMessage = "N/A";
            return null;
         }

         if (!repository2Statistic.Value.State.IsCloned)
         {
            errorMessage = "N/A (not cloned)";
            return null;
         }

         KeyValuePair<DiffStatisticKey, DiffStatistic?> stat =
            repository2Statistic.Value.Statistic.SingleOrDefault(x => x.Key == fmk.MergeRequest.IId);
         if (stat.Key == default(DiffStatisticKey))
         {
            // This is to be shown while "silent update" is in progress.
            // If update fails, it is still shown, can be considered a bug... so TODO.
            errorMessage = "Checking...";
            return null;
         }
         else if (!stat.Value.HasValue)
         {
            errorMessage = _updating.Contains(repository2Statistic.Key) ? "Loading..." : "Error";
            return null;
         }

         errorMessage = String.Empty;
         return stat.Value;
      }

      internal event Action Update;

      private void onLocalGitRepositoryUpdated(ILocalGitRepository repo)
      {
         if (!_gitStatistic.ContainsKey(repo))
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
                     "[GitStatisticManager] Update failed. LocalGitRepositoryData is not ready (Host={0}, Project={1})",
                     repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
                  return;
               }

               if (!_updating.Add(repo))
               {
                  return;
               }

               DateTime prevLatestChange = _gitStatistic[repo].State.LatestChange;

               // Use local project checker for the whole Project because it is always not less
               // than the latest version of any merge request that we have locally.
               // This allows to guarantee that each MR is processed once and not on each git repository update.
               DateTime latestChange = await _projectCheckerFactory.GetLocalProjectChecker(repo.ProjectKey).
                  GetLatestChangeTimestamp();

               Dictionary<MergeRequestKey, Version> versionsToUpdate = new Dictionary<MergeRequestKey, Version>();

               foreach (MergeRequest mergeRequest in _mergeRequestProvider.GetMergeRequests(repo.ProjectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = repo.ProjectKey, IId = mergeRequest.IId };
                  Version version = _mergeRequestProvider.GetLatestVersion(mrk);

                  if (version.Created_At <= prevLatestChange || version.Created_At > latestChange)
                  {
                     continue;
                  }

                  Trace.TraceInformation(String.Format(
                     "[GitStatisticManager] Git statistic will be updated for MR: "
                   + "Host={0}, Project={1}, IId={2}. Latest version created at: {3}",
                     mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId,
                     version.Created_At.ToLocalTime().ToString()));

                  versionsToUpdate.Add(mrk, version);
               }

               foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in versionsToUpdate)
               {
                  if (!_gitStatistic.ContainsKey(repo))
                  {
                     // LocalGitRepository was removed from collection while we were caching current MR
                     break;
                  }

                  DiffStatisticKey key = keyValuePair.Key.IId;
                  resetCachedStatistic(repo, key);
                  Update?.Invoke();
               }

               foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in versionsToUpdate)
               {
                  if (!_gitStatistic.ContainsKey(repo))
                  {
                     // LocalGitRepository was removed from collection while we were caching current MR
                     break;
                  }

                  DiffStatisticKey key = keyValuePair.Key.IId;
                  if (String.IsNullOrEmpty(keyValuePair.Value.Base_Commit_SHA)
                   || String.IsNullOrEmpty(keyValuePair.Value.Head_Commit_SHA))
                  {
                     updateCachedStatistic(repo, key, latestChange, null);
                     Update?.Invoke();
                     continue;
                  }

                  GitDiffArguments args = new GitDiffArguments
                  {
                     Mode = GitDiffArguments.DiffMode.ShortStat,
                     CommonArgs = new GitDiffArguments.CommonArguments
                     {
                        Sha1 = keyValuePair.Value.Base_Commit_SHA,
                        Sha2 = keyValuePair.Value.Head_Commit_SHA
                     }
                  };

                  try
                  {
                     await repo.Data?.LoadFromDisk(args);
                  }
                  catch (LoadFromDiskFailedException ex)
                  {
                     ExceptionHandlers.Handle(String.Format(
                        "Cannot update git statistic for MR with IID {0}", key), ex);
                     continue;
                  }

                  if (!_gitStatistic.ContainsKey(repo))
                  {
                     // LocalGitRepository was removed from collection while we were caching current MR
                     break;
                  }

                  DiffStatistic? diffStat = parseGitDiffStatistic(repo, key, args);
                  updateCachedStatistic(repo, key, latestChange, diffStat);
                  Update?.Invoke();
               }

               _updating.Remove(repo);
               Update?.Invoke();
            }), null);
      }

      private void resetCachedStatistic(ILocalGitRepository repo, DiffStatisticKey key)
      {
         _gitStatistic[repo].Statistic[key] = null;
      }

      private void updateCachedStatistic(ILocalGitRepository repo, DiffStatisticKey key,
         DateTime latestChange, DiffStatistic? diffStat)
      {
         _gitStatistic[repo].Statistic[key] = diffStat;

         Dictionary<DiffStatisticKey, DiffStatistic?> repositoryStatistic = _gitStatistic[repo].Statistic;
         _gitStatistic[repo] = new LocalGitRepositoryStatistic
         {
            Statistic = repositoryStatistic,
            State = new RepositoryState
            {
               IsCloned = true,
               LatestChange = latestChange
            }
         };
      }

      private static readonly Regex gitDiffStatRe =
         new Regex(
            @"(?'files'\d*) file[s]? changed, ((?'ins'\d*) insertion[s]?\(\+\)(, )?)?((?'del'\d*) deletion[s]?\(\-\))?",
               RegexOptions.Compiled);

      private DiffStatistic? parseGitDiffStatistic(ILocalGitRepository repo, DiffStatisticKey key,
         GitDiffArguments args)
      {
         void traceError(string text)
         {
            Trace.TraceError(String.Format(
               "Cannot parse git diff text {0} obtained by key {3} in the repo {2} (in \"{1}\"). "
             + "This makes impossible to show git statistic for MR with IID {4}", text, repo.Path,
               String.Format("{0}/{1}", args.CommonArgs.Sha1?.ToString() ?? "N/A",
                                        args.CommonArgs.Sha2?.ToString() ?? "N/A"),
               String.Format("{0}:{1}", repo.ProjectKey.HostName, repo.ProjectKey.ProjectName), key));
         }

         IEnumerable<string> statText = null;
         try
         {
            statText = repo.Data?.Get(args);
         }
         catch (GitNotAvailableDataException ex)
         {
            ExceptionHandlers.Handle("Cannot obtain git statistic", ex);
         }

         if (statText == null || !statText.Any())
         {
            traceError(statText == null ? "\"null\"" : "(empty)");
            return null;
         }

         int parseOrZero(string x) => int.TryParse(x, out int result) ? result : 0;

         string firstLine = statText.First();
         Match m = gitDiffStatRe.Match(firstLine);
         if (!m.Success || !m.Groups["files"].Success || parseOrZero(m.Groups["files"].Value) < 1)
         {
            traceError(firstLine);
            return null;
         }

         return new DiffStatistic(parseOrZero(m.Groups["files"].Value),
            parseOrZero(m.Groups["ins"].Value), parseOrZero(m.Groups["del"].Value));
      }

      private void onLocalGitRepositoryDisposed(ILocalGitRepository repo)
      {
         repo.Disposed -= onLocalGitRepositoryDisposed;
         repo.Updated -= onLocalGitRepositoryUpdated;
         _gitStatistic.Remove(repo);
         Update?.Invoke();
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
         {
            foreach (Project project in projects)
            {
               ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
               ILocalGitRepository repo =
                  (await _factoryAccessor.GetFactory())?.GetRepository(key.HostName, key.ProjectName);
               if (repo != null && !_gitStatistic.ContainsKey(repo))
               {
                  _gitStatistic.Add(repo, new LocalGitRepositoryStatistic()
                  {
                     State = new RepositoryState
                     {
                        LatestChange = DateTime.MinValue,
                        IsCloned = !repo.DoesRequireClone()
                     },
                     Statistic = new Dictionary<DiffStatisticKey, DiffStatistic?>()
                  });

                  Trace.TraceInformation(String.Format("[GitStatisticManager] Subscribing to Git Repo {0}/{1}",
                     repo.ProjectKey.HostName, repo.ProjectKey.ProjectName));
                  repo.Updated += onLocalGitRepositoryUpdated;
                  repo.Disposed += onLocalGitRepositoryDisposed;
               }
            }

            Update?.Invoke();
         }), null);
      }

      internal struct RepositoryState
      {
         internal bool IsCloned;
         internal DateTime LatestChange;
      }

      internal struct DiffStatistic
      {
         internal DiffStatistic(int files, int insertions, int deletions)
         {
            _filesChanged = files;
            _insertions = insertions;
            _deletions = deletions;
         }

         public override string ToString()
         {
            return String.Format("+ {1} / - {2}\n{0} files", _filesChanged, _insertions, _deletions);
         }

         private readonly int _filesChanged;
         private readonly int _insertions;
         private readonly int _deletions;
      }

      private struct LocalGitRepositoryStatistic
      {
         internal RepositoryState State;
         internal Dictionary<DiffStatisticKey, DiffStatistic?> Statistic;
      }

      private readonly HashSet<ILocalGitRepository> _updating = new HashSet<ILocalGitRepository>();
      private readonly Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic> _gitStatistic =
         new Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly ICachedMergeRequestProvider _mergeRequestProvider;
      private readonly IProjectCheckerFactory _projectCheckerFactory;
      private readonly ILocalGitRepositoryFactoryAccessor _factoryAccessor;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

