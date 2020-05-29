using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
      }

      internal event Action Update;

      public new void Dispose()
      {
         base.Dispose();
      }

      /// <summary>
      /// Returns statistic for the given MR
      /// Statistic is collected for hash tags that match the last version of a merge request
      /// </summary>
      internal DiffStatistic? GetStatistic(MergeRequestKey mrk, out string statusMessage)
      {
         statusMessage = String.Empty;
         ILocalGitRepository repo = getRepository(mrk.ProjectKey);
         if (repo == null)
         {
            statusMessage = "N/A";
         }
         else if (repo.ExpectingClone)
         {
            statusMessage = "N/A (not cloned)";
         }
         else if (!_gitStatistic.ContainsKey(mrk))
         {
            statusMessage = "N/A";
         }
         else if (!_gitStatistic[mrk].HasValue)
         {
            statusMessage = "Checking...";
         }
         else if (!_gitStatistic[mrk].Value.Statistic.HasValue)
         {
            statusMessage = "Error";
         }
         return String.IsNullOrEmpty(statusMessage) ?
            _gitStatistic[mrk].Value.Statistic.Value : new Nullable<DiffStatistic>();
      }

      protected override void preUpdate(ILocalGitRepository repo)
      {
         IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestCache.GetMergeRequests(repo.ProjectKey)
            .Select(x => new MergeRequestKey(repo.ProjectKey, x.IId))
            .Where(x => !_gitStatistic.ContainsKey(x));
         foreach (MergeRequestKey mrk in mergeRequestKeys)
         {
            _gitStatistic[mrk] = null;
         }
         Update?.Invoke();
      }

      async protected override Task doUpdate(ILocalGitRepository repo)
      {
         foreach (KeyValuePair<MergeRequestKey, Version> keyValuePair in collectLatestVersions(repo))
         {
            DiffStatistic? diffStat = null;
            if (!String.IsNullOrEmpty(keyValuePair.Value.Base_Commit_SHA)
             && !String.IsNullOrEmpty(keyValuePair.Value.Head_Commit_SHA))
            {
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

               try
               {
                  if (repo.Updater != null)
                  {
                     await repo.Updater.SilentUpdate(new CommitBasedContextProvider(
                        new string[] { keyValuePair.Value.Base_Commit_SHA, keyValuePair.Value.Head_Commit_SHA }));
                  }
                  if (repo.Data != null)
                  {
                     await repo.Data.LoadFromDisk(args);
                  }
                  diffStat = parseGitDiffStatistic(repo, keyValuePair.Key, args);
               }
               catch (LoadFromDiskFailedException ex)
               {
                  ExceptionHandlers.Handle(String.Format(
                     "Cannot update git statistic for MR with IID {0}", keyValuePair.Key), ex);
               }
            }
            _gitStatistic[keyValuePair.Key] = new MergeRequestStatistic(keyValuePair.Value.Created_At, diffStat);
            Update?.Invoke();
         }
      }

      private Dictionary<MergeRequestKey, Version> collectLatestVersions(ILocalGitRepository repo)
      {
         Dictionary<MergeRequestKey, Version> result = new Dictionary<MergeRequestKey, Version>();

         IEnumerable<MergeRequestKey> mergeRequestKeys = _mergeRequestCache.GetMergeRequests(repo.ProjectKey)
            .Select(x => new MergeRequestKey(repo.ProjectKey, x.IId));

         foreach (MergeRequestKey mrk in mergeRequestKeys)
         {
            Version version = _mergeRequestCache.GetLatestVersion(mrk);
            bool newKey = !_gitStatistic.ContainsKey(mrk) || !_gitStatistic[mrk].HasValue;
            if (version == null || (!newKey && version.Created_At <= _gitStatistic[mrk].Value.LatestChange))
            {
               continue;
            }

            Trace.TraceInformation(String.Format(
               "[GitStatisticManager] Git statistic will be updated for MR: "
             + "Host={0}, Project={1}, IId={2}. Latest version created at: {3}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId,
               version.Created_At.ToLocalTime().ToString()));

            result.Add(mrk, version);
         }

         return result;
      }

      private static readonly Regex gitDiffStatRe =
         new Regex(
            @"(?'files'\d*) file[s]? changed, ((?'ins'\d*) insertion[s]?\(\+\)(, )?)?((?'del'\d*) deletion[s]?\(\-\))?",
               RegexOptions.Compiled);

      private DiffStatistic? parseGitDiffStatistic(ILocalGitRepository repo, MergeRequestKey mrk,
         GitDiffArguments args)
      {
         void traceError(string text)
         {
            Trace.TraceError(String.Format(
               "Cannot parse git diff text {0} obtained by key {3} in the repo {2} (in \"{1}\"). "
             + "This makes impossible to show git statistic for MR with IID {4}", text, repo.Path,
               String.Format("{0}/{1}", args.CommonArgs.Sha1?.ToString() ?? "N/A",
                                        args.CommonArgs.Sha2?.ToString() ?? "N/A"),
               String.Format("{0}:{1}", repo.ProjectKey.HostName, repo.ProjectKey.ProjectName), mrk.IId));
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

      private struct MergeRequestStatistic
      {
         public MergeRequestStatistic(DateTime latestChange, DiffStatistic? statistic)
         {
            LatestChange = latestChange;
            Statistic = statistic;
         }

         internal DateTime LatestChange { get; }
         internal DiffStatistic? Statistic { get; }
      }

      private readonly Dictionary<MergeRequestKey, MergeRequestStatistic?> _gitStatistic =
         new Dictionary<MergeRequestKey, MergeRequestStatistic?>();
   }
}

