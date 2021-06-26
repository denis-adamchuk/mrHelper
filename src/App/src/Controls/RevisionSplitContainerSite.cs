using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class RevisionSplitContainerSite : UserControl
   {
      private Func<ProjectKey, ILocalCommitStorage> _getStorage;
      private Func<MergeRequestKey, HashSet<string>> _getReviewedRevisions;
      private ILocalCommitStorage _storage;

      public RevisionSplitContainerSite()
      {
         InitializeComponent();
      }

      internal void Initialize(
         Func<ProjectKey, ILocalCommitStorage> getStorage,
         Func<MergeRequestKey, HashSet<string>> getReviewedRevisions)
      {
         _getStorage = getStorage;
         _getReviewedRevisions = getReviewedRevisions;
      }

      internal void SetData(MergeRequestKey mrk, DataCache dataCache)
      {
         updateRevisionBrowserTree(dataCache, mrk);
         _storage = _getStorage(mrk.ProjectKey);
      }

      internal void ClearData()
      {
         _storage = null;
         clearRevisionBrowser();
      }

      public SplitContainer SplitContainer => splitContainer;

      public RevisionBrowser RevisionBrowser => revisionBrowser;

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);
      }

      private void revisionBrowser_SelectionChanged(object sender, EventArgs e)
      {
         if (_storage == null)
         {
            return;
         }

         RevisionComparisonArguments? getArguments()
         {
            string[] selected = revisionBrowser.GetSelectedSha(out RevisionType? type);
            switch (selected.Count())
            {
               case 1: return new RevisionComparisonArguments(revisionBrowser.GetBaseCommitSha(), selected[0]);
               case 2: return new RevisionComparisonArguments(selected[0], selected[1]); // TODO order?
            }
            return null;
         };

         RevisionComparisonArguments? arguments = getArguments();
         if (!arguments.HasValue)
         {
            // Clear UI
            return;
         }

         textBox1.Text = "Loading...";
         Action method = new Action(async () => await showRevisionPreviewAsync(arguments.Value));
         BeginInvoke(method, null);
      }

      async private Task showRevisionPreviewAsync(RevisionComparisonArguments arguments)
      {
         await _storage.Git?.FetchAsync(arguments);
         Comparison comparison = _storage.Git?.GetComparison(arguments);
         if (comparison == null)
         {
            Debug.Assert(false);
            return;
         }

         textBox1.Text = String.Join("\r\n", comparison.Diffs.Select(diff => diff.Diff));
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
   }
}

