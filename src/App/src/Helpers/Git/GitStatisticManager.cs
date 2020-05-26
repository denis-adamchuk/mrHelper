using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiffStatisticKey = System.Int32; // Merge Request IId
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Traces git diff statistic change for all merge requests within one or more repositories
   /// </summary>
   internal class GitStatisticManager : BaseGitHelper, IDisposable
   {
      internal GitStatisticManager(
         IMergeRequestCache mergeRequestCache,
         IDiscussionCache discussionCache,
         IProjectUpdateContextProviderFactory updateContextProviderFactory,
         ISynchronizeInvoke synchronizeInvoke,
         ILocalGitRepositoryFactoryAccessor factoryAccessor)
         : base(mergeRequestCache, discussionCache, updateContextProviderFactory, synchronizeInvoke, factoryAccessor)
      {
         BaseUpdate += onBaseUpdate;
      }

      internal event Action Update;

      public new void Dispose()
      {
         BaseUpdate -= onBaseUpdate;
         base.Dispose();
      }

      private void onBaseUpdate() => Update?.Invoke();

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

      async protected override Task doUpdate(ILocalGitRepository repo)
      {
         DateTime prevLatestChange = _gitStatistic[repo].State.LatestChange;

         // Use locally cached information for the whole Project because it is always not less
         // than the latest version of any merge request that we have locally.
         // This allows to guarantee that each MR is processed once and not on each git repository update.
         DateTime latestChange = _mergeRequestCache.GetLatestVersion(repo.ProjectKey)?.Created_At ?? DateTime.MinValue;

         Dictionary<MergeRequestKey, Version> versionsToUpdate = new Dictionary<MergeRequestKey, Version>();

         IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestCache.GetMergeRequests(repo.ProjectKey)
            .Select(x => new MergeRequestKey(repo.ProjectKey, x.IId));

         foreach (MergeRequestKey mrk in mergeRequestKeys)
         {
            Version version = _mergeRequestCache.GetLatestVersion(mrk);

            if (version == null || version.Created_At <= prevLatestChange || version.Created_At > latestChange)
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
            DiffStatisticKey key = keyValuePair.Key.IId;
            resetCachedStatistic(repo, key);
            Update?.Invoke();
         }

         int updateCount = 0;
         foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in versionsToUpdate)
         {
            DiffStatisticKey key = keyValuePair.Key.IId;
            if (String.IsNullOrEmpty(keyValuePair.Value.Base_Commit_SHA)
             || String.IsNullOrEmpty(keyValuePair.Value.Head_Commit_SHA))
            {
               updateCachedStatistic(repo, key, latestChange, null);
               Update?.Invoke();
               continue;
            }

            GitDiffArguments args = new GitDiffArguments
            (
               GitDiffArguments.DiffMode.ShortStat,
               new GitDiffArguments.CommonArguments
               (
                  keyValuePair.Value.Base_Commit_SHA,
                  keyValuePair.Value.Head_Commit_SHA,
                  null, null, null
               ),
               null
            );

            bool success = true;
            try
            {
               await repo.Data?.LoadFromDisk(args);
            }
            catch (LoadFromDiskFailedException ex)
            {
               ExceptionHandlers.Handle(String.Format(
                  "Cannot update git statistic for MR with IID {0}", key), ex);
               success = false;
            }

            if (success)
            {
               DiffStatistic? diffStat = parseGitDiffStatistic(repo, key, args);
               updateCachedStatistic(repo, key, latestChange, diffStat);
               updateCount++;
               if (updateCount % 2 == 0) // to reduce number of Update calls
               {
                  Update?.Invoke();
               }
            }
         }
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
         _gitStatistic[repo] = new LocalGitRepositoryStatistic(
            new RepositoryState(true, latestChange), repositoryStatistic);
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

      internal struct RepositoryState
      {
         public RepositoryState(bool isCloned, DateTime latestChange)
         {
            IsCloned = isCloned;
            LatestChange = latestChange;
         }

         internal bool IsCloned { get; }
         internal DateTime LatestChange { get; }
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
            string fileNumber = String.Format("{0} {1}", _filesChanged, _filesChanged > 1 ? "files" : "file");
            return String.Format("+ {1} / - {2}\n{0}", fileNumber, _insertions, _deletions);
         }

         private readonly int _filesChanged;
         private readonly int _insertions;
         private readonly int _deletions;
      }

      private struct LocalGitRepositoryStatistic
      {
         public LocalGitRepositoryStatistic(RepositoryState state, Dictionary<int, DiffStatistic?> statistic)
         {
            State = state;
            Statistic = statistic;
         }

         internal RepositoryState State { get; }
         internal Dictionary<DiffStatisticKey, DiffStatistic?> Statistic { get; }
      }

      private readonly Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic> _gitStatistic =
         new Dictionary<ILocalGitRepository, LocalGitRepositoryStatistic>();
   }
}

