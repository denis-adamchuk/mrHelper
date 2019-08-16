using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Updates attached GitClient object
   /// </summary>
   public class GitClientUpdater
   {
      /// <summary>
      /// Bind to the specific GitClient object
      /// </summary>
      internal GitClientUpdater(GitClient gitClient)
      {
         GitClient = gitClient;

         startTimer();
      }

      /// <summary>
      /// Set an object that allows to check for updates
      /// </summary>
      public void SetCommitChecker(CommitChecker commitChecker)
      {
         CommitChecker = commitChecker;
      }

      async private void onTimer(object sender, EventArgs e)
      {
         if (!GitClient.LastUpdateTime.HasValue)
         {
            return;
         }

         if (CommitChecker != null && await CommitChecker.AreNewCommitsAsync(GitClient.LastUpdateTime.Value))
         {
            await GitClient.FetchAsync(false);
         }
      }

      private void startTimer()
      {
         Timer.Elapsed += new System.Timers.ElapsedEventHandler(onTimer);
         Timer.Start();
      }

      private GitClient GitClient { get; }
      private CommitChecker CommitChecker { get; set; }

      private static readonly int TimerInterval = 60000; // ms
      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = TimerInterval
         };
   }
}

