using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Updates attached GitClient object
   /// </summary>
   public class GitClientUpdater
   {
      public delegate Task OnUpdate();

      /// <summary>
      /// Bind to the specific GitClient object
      /// </summary>
      internal GitClientUpdater(OnUpdate onUpdate)
      {
         _onUpdate = onUpdate;
         startTimer();
      }

      /// <summary>
      /// Set an object that allows to check for updates
      /// </summary>
      public void SetCommitChecker(CommitChecker commitChecker)
      {
         _commitChecker = commitChecker;
      }

      async public Task ForceUpdateAsync()
      {
         await doUpdate();
      }

      async private Task doUpdate()
      {
         try
         {
            await _onUpdate();
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot update git repository");
            return;
         }

         _lastUpdateTime = DateTime.Now;
      }

      async private void onTimer(object sender, EventArgs e)
      {
         if (!_lastUpdateTime.HasValue)
         {
            return;
         }

         if (_commitChecker != null && await _commitChecker.AreNewCommitsAsync(_lastUpdateTime.Value))
         {
            await doUpdate();
         }
      }

      private void startTimer()
      {
         Timer.Elapsed += new System.Timers.ElapsedEventHandler(onTimer);
         Timer.Start();
      }

      // Timestamp of the most recent update, by default it is empty
      private OnUpdate _onUpdate { get; }
      private CommitChecker _commitChecker { get; set; }
      private DateTime? _lastUpdateTime { get; set; }

      private static readonly int TimerInterval = 60000; // ms
      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = TimerInterval
         };
   }
}

