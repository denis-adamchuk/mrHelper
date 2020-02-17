using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Workflow;
using System.ComponentModel;
using mrHelper.GitClient;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Versions;
using Version = GitLabSharp.Entities.Version;
using DiffStatisticKey = System.Int32; // Merge Request IId

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Traces git diff statistic change for all merge requests within one or more repositories
   /// </summary>
   internal class GitStatisticManager
   {
      internal GitStatisticManager(Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         IHostProperties settings, Func<ProjectKey, Task<ILocalGitRepository>> getLocalGitRepository,
         IMergeRequestProvider mergeRequestProvider)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            synchronizeInvoke.BeginInvoke(new Action(
               async () =>
            {
               if (_gitStatistic?.Count > 0)
               {
                  Trace.TraceInformation(String.Format(
                     "[GitStatisticManager] Unsubscribing from {0} Git Repos", _gitStatistic.Count()));

                  _gitStatistic.Keys.ToList().ForEach(x => x.Updated -= onLocalGitRepositoryUpdated);
                  _gitStatistic.Keys.ToList().ForEach(x => x.Disposed -= onLocalGitRepositoryDisposed);
                  _gitStatistic.Clear();
               }

               // TODO Current version supports updates of projects of the most recent loaded host
               if (_gitStatistic == null
                || _gitStatistic.Count == 0
                || _gitStatistic.Keys.First().ProjectKey.HostName != hostname)
               {
                  _gitStatistic = new Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic>();
                  foreach (Project project in projects)
                  {
                     ProjectKey key = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
                     ILocalGitRepository repo = await getLocalGitRepository(key);
                     if (repo != null)
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

                        // TODO It might require to make ForceUpdate() here if GitStatisticManager wants to
                        // guaranteely not miss any repository updates
                     }
                  }

                  Trace.TraceInformation(String.Format("[GitStatisticManager] Subscribing to {0} Git Repos",
                     _gitStatistic.Count()));
                  _gitStatistic.Keys.ToList().ForEach(x => x.Updated += onLocalGitRepositoryUpdated);
                  _gitStatistic.Keys.ToList().ForEach(x => x.Disposed += onLocalGitRepositoryDisposed);
               }

               Update?.Invoke();
            }), null);
         };

         _synchronizeInvoke = synchronizeInvoke;
         _versionManager = new VersionManager(settings);
         _mergeRequestProvider = mergeRequestProvider;
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
            Debug.Assert(false);
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
            errorMessage = "Updating git...";
            return null;
         }
         else if (!stat.Value.HasValue)
         {
            errorMessage = "Loading...";
            return null;
         }

         errorMessage = String.Empty;
         return stat.Value;
      }

      internal event Action Update;

      private void onLocalGitRepositoryUpdated(ILocalGitRepository repo, DateTime latestChange)
      {
         if (!_gitStatistic.ContainsKey(repo))
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               DateTime prevLatestChange = _gitStatistic[repo].State.LatestChange;

               Dictionary<MergeRequestKey, Version> versionsToUpdate = new Dictionary<MergeRequestKey, Version>();

               foreach (MergeRequest mergeRequest in _mergeRequestProvider.GetMergeRequests(repo.ProjectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = repo.ProjectKey, IId = mergeRequest.IId };
                  Version version;
                  try
                  {
                     version = await _versionManager.GetLatestVersion(mrk);
                  }
                  catch (VersionManagerException)
                  {
                     // already handled
                     continue;
                  }

                  if (version.Created_At <= prevLatestChange || version.Created_At > latestChange)
                  {
                     continue;
                  }

                  versionsToUpdate.Add(mrk, version);
               }

               foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in versionsToUpdate)
               {
                  DiffStatisticKey key = keyValuePair.Key.IId;
                  resetCachedStatistic(repo, key);
                  Update?.Invoke();
               }

               foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in versionsToUpdate)
               {
                  DiffStatisticKey key = keyValuePair.Key.IId;
                  resetCachedStatistic(repo, key);
                  Update?.Invoke();

                  GitDiffArguments args = new GitDiffArguments
                  {
                     Mode = GitDiffArguments.DiffMode.ShortStat,
                     CommonArgs = new GitDiffArguments.CommonArguments
                     {
                        Sha1 = keyValuePair.Value.Base_Commit_SHA,
                        Sha2 = keyValuePair.Value.Head_Commit_SHA
                     }
                  };

                  await repo.Data.Update(new GitDiffArguments[] { args });

                  if (!_gitStatistic.ContainsKey(repo))
                  {
                     // LocalGitRepository was removed from collection while we were caching current MR
                     break;
                  }

                  DiffStatistic? diffStat = parseGitDiffStatistic(repo, key, args);
                  updateCachedStatistic(repo, key, latestChange, diffStat);
                  Update?.Invoke();
               }
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
         Action<string> traceError = (text) =>
         {
            Trace.TraceError(String.Format(
               "Cannot parse git diff text {0} obtained by key {3} in the repo {2} (in \"{1}\"). "
             + "This makes impossible to show git statistic for MR with IID {4}", text, repo.Path,
               String.Format("{0}/{1}", args.CommonArgs.Sha1, args.CommonArgs.Sha2),
               String.Format("{0}:{1}", repo.ProjectKey.HostName, repo.ProjectKey.ProjectName), key));
         };

         IEnumerable<string> statText = repo.Data.Get(args);
         if (statText == null || !statText.Any())
         {
            traceError(statText == null ? "\"null\"" : "(empty)");
            return null;
         }

         Func<string, int> parseOrZero = (x) => int.TryParse(x, out int result) ? result : 0;

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

         private int _filesChanged;
         private int _insertions;
         private int _deletions;
      }

      private struct LocalGitRepositoryStatistic
      {
         internal RepositoryState State;
         internal Dictionary<DiffStatisticKey, DiffStatistic?> Statistic;
      }

      private Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic> _gitStatistic =
         new Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionManager _versionManager;
      private readonly IMergeRequestProvider _mergeRequestProvider;
   }
}

