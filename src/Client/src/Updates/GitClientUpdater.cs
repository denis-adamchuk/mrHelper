using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Checks for new commits
   /// </summary>
   public class GitClientUpdater
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      internal GitClientUpdater(MergeRequestDescriptor mrd, GitClient gitClient, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         GitClient = gitClient;
         UpdateOperator = updateOperator;

         startTimer();
      }

      /// <summary>
      /// Force update GitClient if there are new commits
      /// </summary>
      async public Task Update()
      {
         if (await areNewCommitsAsync(client.LastUpdateTime.Value))
         {
            await FetchAsync();
         }
      }

      /// <summary>
      /// Checks for commits newer than the given timestamp
      /// </summary>
      async public Task<bool> areNewCommitsAsync(DateTime timestamp)
      {
         List<GitLabSharp.Entities.Version> versions = await UpdateOperator.GetVersions(MergeRequestDescriptor);
         return versions != null && versions.Count > 0 && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      private void startTimer()
      {
         Timer.Elapsed += new System.Timers.ElapsedEventHandler(onTimer);
         Timer.Start();
      }

      async private void onTimer(object sender, EventArgs e)
      {
         if (!client.LastUpdateTime.HasValue)
         {
            return true;
         }

         if (await areNewCommitsAsync(client.LastUpdateTime.Value))
         {
            await FetchAsync();
         }
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private GitClient GitClient { get; }
      private UpdateOperator UpdateOperator { get; }

      private static readonly int TimerInterval = 60000; // ms
      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = TimerInterval
         };
   }
}

