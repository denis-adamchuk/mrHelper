using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class RevisionSplitContainerSite : UserControl
   {
      private Func<ProjectKey, ILocalCommitStorage> _getStorage;
      private Func<ProjectKey, RepositoryAccessor> _getRepositoryAccessor;
      private Func<MergeRequestKey, HashSet<string>> _getReviewedRevisions;
      private ILocalCommitStorage _storage;
      private RepositoryAccessor _repositoryAccessor;

      public RevisionSplitContainerSite()
      {
         InitializeComponent();
      }

      internal void Initialize(
         Func<ProjectKey, ILocalCommitStorage> getStorage,
         Func<ProjectKey, RepositoryAccessor> getRepositoryAccessor,
         Func<MergeRequestKey, HashSet<string>> getReviewedRevisions)
      {
         _getStorage = getStorage;
         _getRepositoryAccessor = getRepositoryAccessor;
         _getReviewedRevisions = getReviewedRevisions;
      }

      internal void SetData(MergeRequestKey mrk, DataCache dataCache)
      {
         _storage = _getStorage(mrk.ProjectKey);
         _repositoryAccessor?.Dispose();
         _repositoryAccessor = _getRepositoryAccessor(mrk.ProjectKey);
         updateRevisionBrowserTree(dataCache, mrk);
      }

      internal void ClearData()
      {
         _storage = null;
         clearRevisionBrowser();
      }

      public SplitContainer SplitContainer => splitContainer;

      public RevisionBrowser RevisionBrowser => revisionBrowser;

      private void updateStatusLabelLocation()
      {
         // position label at the center of the panel
         labelLoading.Location = new System.Drawing.Point(
            (panelPreviewStatus.Width - labelLoading.Width) / 2,
            (panelPreviewStatus.Height - labelLoading.Height) / 2);
      }

      private void panelLoading_SizeChanged(object sender, EventArgs e)
      {
         updateStatusLabelLocation();
      }

      private void revisionBrowser_PreSelectionChanged(object sender, EventArgs e)
      {
         _delayedSelectionHandling = true;
      }

      async private void revisionBrowser_SelectionChanged(object sender, EventArgs e)
      {
         if (!_delayedSelectionHandling)
         {
            await onRevisionSelectionChanged();
         }
      }

      async private void revisionBrowser_PostSelectionChanged(object sender, EventArgs e)
      {
         if (_delayedSelectionHandling)
         {
            _delayedSelectionHandling = false;
            await onRevisionSelectionChanged();
         }
      }

      private async Task onRevisionSelectionChanged()
      {
         _repositoryAccessor?.Cancel();
         await TaskUtils.WhileAsync(() => _isFetching); // to not shuffle states

         RevisionComparisonArguments? getArguments()
         {
            string[] selected = revisionBrowser.GetSelectedSha(out RevisionType? type);
            switch (selected.Count())
            {
               case 1: return new RevisionComparisonArguments(revisionBrowser.GetBaseCommitSha(), selected[0]);
               case 2: return new RevisionComparisonArguments(selected[0], selected[1]);
            }
            return null;
         };

         RevisionComparisonArguments? arguments = getArguments();
         if (!arguments.HasValue || !arguments.Value.IsValid())
         {
            PreviewLoadingState state = arguments.HasValue ?
               PreviewLoadingState.Failed : PreviewLoadingState.NotAvailable;
            updatePreviewState(state);
            return;
         }

         if (!isStorageAvailable())
         {
            updatePreviewState(PreviewLoadingState.StorageNotAvailable);
            return;
         }

         updatePreviewState(PreviewLoadingState.Loading);
         await showRevisionPreviewAsync(arguments.Value);
      }

      async private Task showRevisionPreviewAsync(RevisionComparisonArguments arguments)
      {
         try
         {
            _isFetching = true;
            try
            {
               await _storage.Git.FetchAsync(arguments, _repositoryAccessor);
            }
            finally
            {
               _isFetching = false;
            }
         }
         catch (FetchFailedException)
         {
            updatePreviewState(PreviewLoadingState.Failed);
            return;
         }

         if (!isStorageAvailable())
         {
            updatePreviewState(PreviewLoadingState.StorageNotAvailable);
            return;
         }

         ComparisonEx comparison = _storage.Git?.GetComparison(arguments);
         if (comparison == null)
         {
            updatePreviewState(PreviewLoadingState.Cancelled);
         }
         else if (comparison.Compare_Timeout)
         {
            updatePreviewState(PreviewLoadingState.CancelledByTimeOut);
         }
         else
         {
            updatePreviewState(PreviewLoadingState.Ready, comparison);
         }
      }

      private void updateRevisionBrowserTree(DataCache dataCache, MergeRequestKey mrk)
      {
         IMergeRequestCache cache = dataCache.MergeRequestCache;
         if (cache != null)
         {
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            IEnumerable<GitLabSharp.Entities.Version> versions = cache.GetVersions(mrk);
            IEnumerable<Commit> commits = cache.GetCommits(mrk);

            bool hasObjects = commits.Any() || versions.Any();
            if (hasObjects)
            {
               RevisionBrowserModelData data = new RevisionBrowserModelData(latestVersion?.Base_Commit_SHA,
                  commits, versions, _getReviewedRevisions(mrk));
               revisionBrowser.SetData(data, ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
            }
            else
            {
               clearRevisionBrowser();
            }
         }
      }

      private void clearRevisionBrowser()
      {
         revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
      }

      private enum PreviewLoadingState
      {
         StorageNotAvailable,
         NotAvailable,
         Loading,
         Failed,
         CancelledByTimeOut,
         Cancelled,
         Ready
      }

      private void updatePreviewState(PreviewLoadingState state, ComparisonEx comparison = null)
      {
         _previewState = state;
         switch (_previewState)
         {
            case PreviewLoadingState.StorageNotAvailable:
               labelLoading.Text = "File storage is not ready";
               panelPreviewStatus.Visible = true;
               break;
            case PreviewLoadingState.NotAvailable:
               listViewRevisionComparisonStructure.ClearData();
               panelPreviewStatus.Visible = false;
               break;
            case PreviewLoadingState.Loading:
               labelLoading.Text = "Loading...";
               panelPreviewStatus.Visible = true;
               break;
            case PreviewLoadingState.Failed:
               labelLoading.Text = "Failed to load revision comparison preview";
               panelPreviewStatus.Visible = true;
               break;
            case PreviewLoadingState.CancelledByTimeOut:
               labelLoading.Text = "Revision comparison preview loading timed out";
               panelPreviewStatus.Visible = true;
               break;
            case PreviewLoadingState.Cancelled:
               listViewRevisionComparisonStructure.ClearData();
               panelPreviewStatus.Visible = false;
               break;
            case PreviewLoadingState.Ready:
               if (comparison != null)
               {
                  listViewRevisionComparisonStructure.SetData(comparison.GetStatistic());
               }
               panelPreviewStatus.Visible = false;
               break;
         }
         if (panelPreviewStatus.Visible)
         {
            updateStatusLabelLocation();
         }
      }

      private bool isStorageAvailable()
      {
         return _storage != null && _storage.Git != null && _repositoryAccessor != null;
      }

      private PreviewLoadingState _previewState = PreviewLoadingState.NotAvailable;
      private bool _isFetching;
      private bool _delayedSelectionHandling;
   }
}

