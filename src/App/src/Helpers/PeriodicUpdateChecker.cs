using System;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Constants;

namespace mrHelper.App.Helpers
{
   internal class PeriodicUpdateChecker : IDisposable
   {
      internal PeriodicUpdateChecker(ISynchronizeInvoke synchronizeInvoke)
      {
         _checkForUpdatesTimer.Elapsed += onCheckForUpdatesTimer;
         _checkForUpdatesTimer.SynchronizingObject = synchronizeInvoke;
         _checkForUpdatesTimer.Start();
      }

      public void Dispose()
      {
         _checkForUpdatesTimer?.Stop();
         _checkForUpdatesTimer?.Dispose();
         _checkForUpdatesTimer = null;
      }

      internal event Action NewVersionAvailable;

      private void onCheckForUpdatesTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         Trace.TraceInformation("[CheckForUpdates] Checking for updates on timer");
         ISynchronizeInvoke synchronizeInvoke = (sender as System.Timers.Timer).SynchronizingObject;
         synchronizeInvoke.BeginInvoke(new Action(async () =>
         {
            await StaticUpdateChecker.CheckForUpdatesAsync(Program.ServiceManager);
            if (StaticUpdateChecker.NewVersionInformation != null
             && StaticUpdateChecker.NewVersionInformation.VersionNumber != _previousNewVersionNumber)
            {
               _previousNewVersionNumber = StaticUpdateChecker.NewVersionInformation.VersionNumber;
               NewVersionAvailable?.Invoke();
            }
         }), null);
      }

      private string _previousNewVersionNumber;

      private System.Timers.Timer _checkForUpdatesTimer = new System.Timers.Timer
      {
         Interval = Constants.CheckForUpdatesTimerInterval
      };
   }
}

