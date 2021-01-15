using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private string getStorageSummaryUpdateInformation()
      {
         if (!_mergeRequestsUpdatingByUserRequest.Any())
         {
            return "All storages are up-to-date";
         }

         var mergeRequestGroups = _mergeRequestsUpdatingByUserRequest
            .Distinct()
            .GroupBy(
               group => group.ProjectKey,
               group => group,
               (group, groupedMergeRequests) => new
               {
                  Project = group.ProjectName,
                  MergeRequests = groupedMergeRequests
               });

         List<string> storages = new List<string>();
         foreach (var group in mergeRequestGroups)
         {
            IEnumerable<string> mergeRequestIds = group.MergeRequests.Select(x => String.Format("#{0}", x.IId));
            string mergeRequestIdsString = String.Join(", ", mergeRequestIds);
            string storage = String.Format("{0} ({1})", group.Project, mergeRequestIdsString);
            storages.Add(storage);
         }

         return String.Format("Updating storage{0}: {1}...",
            storages.Count() > 1 ? "s" : "", String.Join(", ", storages));
      }

      private ILocalCommitStorageFactory getCommitStorageFactory(bool showMessageBoxOnError)
      {
         if (_storageFactory == null)
         {
            try
            {
               _storageFactory = new LocalCommitStorageFactory(this,
                  _shortcuts.GetProjectAccessor(),
                  Program.Settings.LocalGitFolder,
                  Program.Settings.RevisionsToKeep,
                  Program.Settings.ComparisonsToKeep);
               _storageFactory.GitRepositoryCloned += onGitRepositoryCloned;
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle("Cannot create LocalGitCommitStorageFactory", ex);
            }
         }

         if (_storageFactory == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format("Cannot create folder {0}", Program.Settings.LocalGitFolder),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         return _storageFactory;
      }

      private void disposeLocalGitRepositoryFactory()
      {
         if (_storageFactory != null)
         {
            _storageFactory.GitRepositoryCloned -= onGitRepositoryCloned;
            _storageFactory.Dispose();
            _storageFactory = null;
         }
      }

      private void onGitRepositoryCloned(ILocalCommitStorage storage)
      {
         requestCommitStorageUpdate(storage.ProjectKey);
      }

      /// <summary>
      /// Make some checks and create a commit storage
      /// </summary>
      /// <returns>null if could not create a repository</returns>
      private ILocalCommitStorage getCommitStorage(ProjectKey projectKey, bool showMessageBoxOnError)
      {
         ILocalCommitStorageFactory factory = getCommitStorageFactory(showMessageBoxOnError);
         if (factory == null)
         {
            return null;
         }

         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         ILocalCommitStorage repo = factory.GetStorage(projectKey, type);
         if (repo == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format(
               "Cannot obtain disk storage for project {0} in \"{1}\"",
               projectKey.ProjectName, Program.Settings.LocalGitFolder),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         return repo;
      }

      private void changeStorageFolder(string newFolder)
      {
         if (_storageFactory == null || _storageFactory.ParentFolder != newFolder)
         {
            textBoxStorageFolder.Text = storageFolderBrowser.SelectedPath;
            Program.Settings.LocalGitFolder = storageFolderBrowser.SelectedPath;

            MessageBox.Show("Storage folder is changed.\n Please restart Diff Tool if you have already launched it.",
               "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            addOperationRecord(String.Format("[MainForm] File storage path has changed to {0}", newFolder));

            Trace.TraceInformation(String.Format("[MainForm] Reconnecting after file storage path change"));
            reconnect();
         }
      }

      private static void disableSSLVerification()
      {
         if (Program.Settings.DisableSSLVerification)
         {
            try
            {
               GitTools.DisableSSLVerification();
               Program.Settings.DisableSSLVerification = false;
            }
            catch (GitTools.SSLVerificationDisableException ex)
            {
               ExceptionHandlers.Handle("Cannot disable SSL verification", ex);
            }
         }
      }

      private void onAbortGitByUserRequest()
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         ILocalCommitStorage repo = getCommitStorage(mrk.Value.ProjectKey, false);
         if (repo == null || repo.Updater == null || !repo.Updater.CanBeStopped())
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         string message = String.Format("Do you really want to abort current git update operation for {0}?",
            mrk.Value.ProjectKey.ProjectName);
         if (MessageBox.Show(message, "Confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
         {
            Trace.TraceInformation(String.Format("[MainForm] User declined to abort current operation for project {0}",
               mrk.Value.ProjectKey.ProjectName));
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] User decided to abort current operation for project {0}",
            mrk.Value.ProjectKey.ProjectName));
         repo.Updater.StopUpdate();
      }

      private void launchStorageFolderChangeDialog()
      {
         storageFolderBrowser.SelectedPath = textBoxStorageFolder.Text;
         if (storageFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            string newFolder = storageFolderBrowser.SelectedPath;
            Trace.TraceInformation(String.Format("[MainForm] User decided to change file storage to {0}", newFolder));
            changeStorageFolder(newFolder);
         }
      }

      private void requestCommitStorageUpdate(ProjectKey projectKey)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);

         IEnumerable<GitLabSharp.Entities.Version> versions = dataCache?.MergeRequestCache?.GetVersions(projectKey);
         if (versions != null)
         {
            VersionBasedContextProvider contextProvider = new VersionBasedContextProvider(versions);
            ILocalCommitStorage storage = getCommitStorage(projectKey, false);
            storage?.Updater?.RequestUpdate(contextProvider, null);
         }
      }

      async private Task<bool> prepareCommitStorage(
         MergeRequestKey mrk, ILocalCommitStorage storage, ICommitStorageUpdateContextProvider contextProvider,
         bool isLimitExceptionFatal)
      {
         Trace.TraceInformation(String.Format(
            "[MainForm] Preparing commit storage by user request for MR IId {0} (at {1})...",
            mrk.IId, storage.Path));

         try
         {
            _mergeRequestsUpdatingByUserRequest.Add(mrk);
            updateStorageDependentControlState(mrk);
            addOperationRecord(getStorageSummaryUpdateInformation());
            await storage.Updater.StartUpdate(contextProvider, status => onStorageUpdateProgressChange(status, mrk),
               () => onStorageUpdateStateChange());
            return true;
         }
         catch (Exception ex)
         {
            if (ex is LocalCommitStorageUpdaterCancelledException)
            {
               MessageBox.Show("Cannot perform requested action without up-to-date storage", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               addOperationRecord("Storage update cancelled by user");
            }
            else if (ex is LocalCommitStorageUpdaterFailedException fex)
            {
               ExceptionHandlers.Handle(ex.Message, ex);
               MessageBox.Show(fex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               addOperationRecord("Failed to update storage");
            }
            else if (ex is LocalCommitStorageUpdaterLimitException mex)
            {
               ExceptionHandlers.Handle(ex.Message, mex);
               if (!isLimitExceptionFatal)
               {
                  return true;
               }
               string extraMessage = "If there are multiple revisions try selecting two other ones";
               MessageBox.Show(mex.OriginalMessage + ". " + extraMessage, "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
               addOperationRecord("Failed to update storage");
            }
            return false;
         }
         finally
         {
            if (!_exiting)
            {
               _mergeRequestsUpdatingByUserRequest.Remove(mrk);
               updateStorageDependentControlState(mrk);
               addOperationRecord(getStorageSummaryUpdateInformation());
            }
         }
      }

   }
}

