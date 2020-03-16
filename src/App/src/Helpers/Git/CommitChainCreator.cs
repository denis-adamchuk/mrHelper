using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitClient;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Repository;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;

namespace mrHelper.App.Helpers
{
   internal class CommitChainCreator
   {
      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
         Action<string> onGitStatusChange, ILocalGitRepository repo, MergeRequestKey mrk)
      {
         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _onGitStatusChange = onGitStatusChange;
         _repo = repo;
         _mrk = mrk;
      }

      internal CommitChainCreator(IHostProperties hostProperties, Action<string> onStatusChange,
         Action<string> onGitStatusChange, ILocalGitRepository repo, string headSha)
      {
         _hostProperties = hostProperties;
         _onStatusChange = onStatusChange;
         _onGitStatusChange = onGitStatusChange;
         _repo = repo;
         _headSha = headSha;
      }

      async public Task CreateChainAsync()
      {
         if (_repo == null)
         {
            return;
         }

         if (_headSha != null)
         {
            if (!_repo.ContainsSHA(_headSha))
            {
               await createBranches(new string[] { _headSha });
            }
         }
         else
         {
            _versionManager = new VersionManager(_hostProperties);
            IEnumerable<string> shas = await getAllVersionHeadSHA(_mrk);
            await createBranches(shas);
         }
      }

      async public Task CancelAsync()
      {
         if (_repositoryManager != null)
         {
            await _repositoryManager.CancelAsync();
         }

         if (_versionManager != null)
         {
            await _versionManager.CancelAsync();
         }

         await _repo.Updater.CancelUpdate();
      }

      async private Task<IEnumerable<string>> getAllVersionHeadSHA(MergeRequestKey mrk)
      {
         IEnumerable<GitLabSharp.Entities.Version> versions;
         try
         {
            _onStatusChange("Loading meta information about versions from GitLab");

            versions = await _versionManager.GetVersions(mrk);
            if (versions == null)
            {
               return null; // cancelled by user
            }
         }
         catch (VersionManagerException ex)
         {
            ExceptionHandlers.Handle("Cannot load meta information about versions", ex);
            return null;
         }

         HashSet<string> heads = new HashSet<string>();
         int iVersion = 1;
         foreach (GitLabSharp.Entities.Version version in versions)
         {
            GitLabSharp.Entities.Version? versionDetailed;
            try
            {
               _onStatusChange(String.Format(
                  "Loading versions from GitLab: {0}/{1}", iVersion++, versions.Count()));

               versionDetailed = await _versionManager.GetVersion(version, mrk);
               if (versionDetailed == null)
               {
                  return null; // cancelled by user
               }
            }
            catch (VersionManagerException ex)
            {
               ExceptionHandlers.Handle("Cannot load meta information about versions", ex);
               return null;
            }

            if (!_repo.ContainsSHA(versionDetailed.Value.Head_Commit_SHA))
            {
               heads.Add(versionDetailed.Value.Head_Commit_SHA);
            }
         }
         return heads;
      }

      async private Task createBranches(IEnumerable<string> shas)
      {
         if (shas == null)
         {
            return;
         }

         _repositoryManager = new RepositoryManager(_hostProperties);

         try
         {
            int iBranch = 1;
            foreach (string sha in shas)
            {
               _onStatusChange(String.Format(
                  "Creating branches at GitLab: {0}/{1}", iBranch++, shas.Count()));

               Branch? branch = await _repositoryManager.CreateNewBranchAsync(
                  _repo.ProjectKey, GitTools.FakeSHA(sha), sha);
               if (branch == null)
               {
                  return; // cancelled
               }
            }

            _onStatusChange("Fetching new branches from remote repository");
            await _repo.Updater.Update(null, _onGitStatusChange);
            _onGitStatusChange(String.Empty);
         }
         catch (RepositoryManagerException ex)
         {
            ExceptionHandlers.Handle("Cannot create a branch at GitLab", ex);
         }
         finally
         {
            int iBranch = 1;
            foreach (string sha in shas)
            {
               try
               {
                  _onStatusChange(String.Format(
                     "Deleting branches at GitLab: {0}/{1}", iBranch++, shas.Count()));

                  await _repositoryManager.DeleteBranchAsync(
                     _repo.ProjectKey, GitTools.FakeSHA(sha));
               }
               catch (RepositoryManagerException ex)
               {
                  ExceptionHandlers.Handle("Cannot delete a branch at GitLab", ex);
                  continue;
               }
            }
         }
      }

      private readonly IHostProperties _hostProperties;
      private readonly Action<string> _onStatusChange;
      private readonly Action<string> _onGitStatusChange;
      private readonly ILocalGitRepository _repo;
      private readonly MergeRequestKey _mrk;
      private readonly string _headSha;

      private RepositoryManager _repositoryManager;
      private VersionManager _versionManager;
   }
}

