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
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   public class RevisionCacher
   {
      public RevisionCacher(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings, Func<ProjectKey, GitClient> getGitClient,
         Func<ProjectKey, MergeRequest[]> getMergeRequests)
      {
         workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            _latestChanges = projects.ToDictionary(
               x => getGitClient(new ProjectKey { HostName = hostname, ProjectName = x.Path_With_Namespace }),
               x => DateTime.MinValue);

            Trace.TraceInformation(String.Format("[RevisionCacher] Subscribing to {0} Git Clients",
               _latestChanges.Count()));
            _latestChanges.Keys.ToList().ForEach(x => x.Updated += onGitClientUpdated);
            _latestChanges.Keys.ToList().ForEach(x => x.Disposed += (y => _latestChanges.Remove(y)));
         };

         _synchronizeInvoke = synchronizeInvoke;
         _operator = new VersionOperator(settings);
         _getMergeRequests = getMergeRequests;
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
               Trace.TraceInformation(String.Format("[RevisionCacher] Processing update of project {0} at host {1}",
                  projectKey.ProjectName, projectKey.HostName));

               DateTime prevLatestChange = _latestChanges[gitClient];
               foreach (MergeRequest mergeRequest in _getMergeRequests(projectKey))
               {
                  MergeRequestKey mrk = new MergeRequestKey { ProjectKey = projectKey, IId = mergeRequest.IId };
                  List<Version> newVersions = await _operator.LoadVersions(mrk);
                  newVersions = newVersions.Where(
                     x => x.Created_At > prevLatestChange && x.Created_At <= latestChange).ToList();
                  List<Version> newVersionsDetailed = new List<Version>();
                  newVersions.ForEach(async x => newVersionsDetailed.Add(await _operator.LoadVersion(x, mrk)));
                  newVersionsDetailed.ForEach(
                     async x =>
                  {
                     Trace.TraceInformation(String.Format(
                        "[RevisionCacher] Caching revisions for project {0} at host {1}",
                        projectKey.ProjectName, projectKey.HostName));
                     await doCacheAsync(gitClient, x.Base_Commit_SHA, x.Head_Commit_SHA, x.Diffs);
                  });
               }
               _latestChanges[gitClient] = latestChange;
            }), null);
      }

      async private static Task doCacheAsync(IGitRepository gitRepository,
         string baseSha, string headSha, List<Diff> diffs)
      {
         foreach (Diff diff in diffs)
         {
            await gitRepository.DiffAsync(baseSha, headSha, diff.Old_Path, diff.New_Path, 0);
            await gitRepository.DiffAsync(baseSha, headSha, diff.Old_Path, diff.New_Path,
               mrHelper.Common.Constants.Constants.FullContextSize);
            if (!diff.New_File)
            {
               await gitRepository.ShowFileByRevisionAsync(diff.Old_Path, baseSha);
            }
            if (!diff.Deleted_File)
            {
               await gitRepository.ShowFileByRevisionAsync(diff.New_Path, headSha);
            }
         }
      }

      private Dictionary<GitClient, DateTime> _latestChanges;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly VersionOperator _operator;
      private readonly Func<ProjectKey, MergeRequest[]> _getMergeRequests;
   }
}

