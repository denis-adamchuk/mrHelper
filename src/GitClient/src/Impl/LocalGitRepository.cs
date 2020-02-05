using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// </summary>
   internal class LocalGitRepository : ILocalGitRepository
   {
      // @{ IGitRepository
      IGitRepositoryData IGitRepository.Data => _data;

      public ProjectKey ProjectKey { get; }
      // @{ IGitRepository

      // @{ ILocalGitRepository
      public ILocalGitRepositoryData Data => _data;

      public string Path { get; }

      public ILocalGitRepositoryUpdater Updater => _updater;

      public event Action<ILocalGitRepository, DateTime> Updated;
      public event Action<ILocalGitRepository> Disposed;

      public bool DoesRequireClone()
      {
         Debug.Assert(canClone(Path) || isValidRepository(Path));
         return !isValidRepository(Path);
      }
      // @} ILocalGitRepository

      // Host Name and Project Name

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(ProjectKey projectKey, string path, IProjectWatcher projectWatcher,
         ISynchronizeInvoke synchronizeInvoke)
      {
         if (!canClone(path) && !isValidRepository(path))
         {
            throw new ArgumentException("Path \"" + path + "\" already exists but it is not a valid git repository");
         }

         ProjectKey = projectKey;
         Path = path;
         _updater = new LocalGitRepositoryUpdater(projectWatcher,
            async (reportProgress, latestChange) =>
         {
            string arguments = canClone(Path) ?
               "clone --progress " +
               ProjectKey.HostName + "/" + ProjectKey.ProjectName + " " +
               StringUtils.EscapeSpaces(Path) : "fetch --progress";

            if (_updateOperationDescriptor == null)
            {
               _updateOperationDescriptor = _operationManager.CreateDescriptor(
                  "git", arguments, canClone(Path) ? String.Empty : Path, reportProgress);

               await _operationManager.Wait(_updateOperationDescriptor);
               _updateOperationDescriptor = null;
               Updated?.Invoke(this, latestChange);
            }
            else
            {
               await _operationManager.Join(_updateOperationDescriptor, reportProgress);
            }
         },
         projectKeyToCheck => ProjectKey.Equals(projectKeyToCheck),
         synchronizeInvoke,
            async () =>
         {
            await _operationManager.Cancel(_updateOperationDescriptor);
            _updateOperationDescriptor = null;
         });

         _operationManager = new GitOperationManager(synchronizeInvoke, path);
         _data = new LocalGitRepositoryData(_operationManager, Path);

         Trace.TraceInformation(String.Format(
            "[LocalGitRepository] Created LocalGitRepository at path {0} for host {1} and project {2}",
            path, ProjectKey.HostName, ProjectKey.ProjectName));
      }

      async internal Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format("[LocalGitRepository] Disposing LocalGitRepository at path {0}", Path));
         _updater.Dispose();
         _data.DisableUpdates();
         await _operationManager.CancelAll();
         Disposed?.Invoke(this);
      }

      /// <summary>
      /// Check if Clone can be called for this LocalGitRepository
      /// </summary>
      static private bool canClone(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      static private bool isValidRepository(string path)
      {
         try
         {
            return Directory.Exists(path)
                && ExternalProcess.Start("git", "rev-parse --is-inside-work-tree", true, path).StdErr.Count() == 0;
         }
         catch (ExternalProcessException)
         {
            return false;
         }
      }

      private LocalGitRepositoryData _data;
      private LocalGitRepositoryUpdater _updater;
      private IExternalProcessManager _operationManager;
      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;
   }
}

