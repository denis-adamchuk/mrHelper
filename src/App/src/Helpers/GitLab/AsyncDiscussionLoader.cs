using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   internal class AsyncDiscussionLoader
   {
      internal AsyncDiscussionLoader(
         MergeRequestKey mrk,
         DataCache dataCache,
         Func<MergeRequestKey, IEnumerable<Discussion>, Task> updateGit,
         ISynchronizeInvoke synchronizeInvoke)
      {
         _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
         _mergeRequestKey = mrk;
         _updateGit = updateGit;
         _synchronizeInvoke = synchronizeInvoke;
      }

      internal event Action<string> StatusChanged;
      internal event Action<IEnumerable<Discussion>> Loaded;

      public void LoadDiscussions()
      {
         _synchronizeInvoke?.BeginInvoke(new Action(
            async () =>
            {
               if (_dataCache?.DiscussionCache == null)
               {
                  Loaded?.Invoke(null);
                  return;
               }

               Trace.TraceInformation(String.Format(
                  "[AsyncDiscussionLoader] Loading discussions. Hostname: {0}, Project: {1}, MR IId: {2}...",
                     _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId));

               IEnumerable<Discussion> discussions = null;
               StatusChanged?.Invoke("Loading discussions");
               try
               {
                  discussions = await _dataCache.DiscussionCache.LoadDiscussions(_mergeRequestKey);
               }
               catch (DiscussionCacheException ex)
               {
                  string message = "Cannot load discussions from GitLab";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               finally
               {
                  StatusChanged?.Invoke(String.Empty);
               }

               if (discussions != null)
               {
                  Trace.TraceInformation("[DiscussionsForm] Checking for new commits...");

                  StatusChanged?.Invoke("Checking for new commits");
                  await _updateGit(_mergeRequestKey, discussions);
                  StatusChanged?.Invoke(String.Empty);
               }

               Loaded?.Invoke(discussions);
            }), null);
      }

      private readonly DataCache _dataCache;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly Func<MergeRequestKey, IEnumerable<Discussion>, Task> _updateGit;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
   }
}

