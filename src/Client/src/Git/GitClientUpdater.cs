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
   public class GitClientUpdater: IDisposable
   {
      public delegate Task OnUpdate(bool reportProgress);

      /// <summary>
      /// Bind to the specific GitClient object
      /// </summary>
      internal GitClientUpdater(OnUpdate onUpdate)
      {
         _onUpdate = onUpdate;
      }

      public void Dispose()
      {
         stopTimer();
         Timer.Dispose();
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
         Debug.WriteLine("GitClientUpdater.ForceUpdateAsync is called");
         _updating = true;
         try
         {
            await doUpdate(true); // this may cancel currently running onTimer update
         }
         finally
         {
            _updating = false;
         }

         // if doUpdate succeeded, it is ok to start periodic updates
         startTimer();
      }

      async private void onTimer(object sender, EventArgs e)
      {
         if (!_lastUpdateTime.HasValue || !EnablePeriodicUpdates || _updating)
         {
            return;
         }

         Debug.WriteLine("GitClientUpdater.onTimer -- begin");
         _updating = true;
         try
         {
            await doUpdate(false);
         }
         catch (GitOperationException ex)
         {
            // just swallow it
            Debug.WriteLine(ex.Message);
         }
         finally
         {
            _updating = false;
            Debug.WriteLine("GitClientUpdater.onTimer -- end");
         }
      }

      async private Task doUpdate(bool reportProgress)
      {
         if (_commitChecker == null)
         {
            return;
         }

         {
            try
            {
               await _onUpdate(reportProgress);
            }
            catch (GitOperationException ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot update git repository");
               throw;
            }
         }

         _lastUpdateTime = DateTime.Now;
      }

      private void startTimer()
      {
         if (!EnablePeriodicUpdates)
         {
            Timer.Elapsed += new System.Timers.ElapsedEventHandler(onTimer);
            Timer.Start();
            EnablePeriodicUpdates = true;
         }
      }

      private void stopTimer()
      {
         EnablePeriodicUpdates = false;
         Timer.Stop();
      }

      // Timestamp of the most recent update, by default it is empty
      private OnUpdate _onUpdate { get; }
      private CommitChecker _commitChecker { get; set; }
      private DateTime? _lastUpdateTime { get; set; } = DateTime.MinValue;

      private static readonly int TimerInterval = 1000; // ms
      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = TimerInterval
         };
      private bool EnablePeriodicUpdates { get; set; } = false;
      private bool _updating = false;
   }
}

