﻿using System;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Constants;

namespace mrHelper.App.Helpers
{
   internal class PeriodicUpdateChecker : IDisposable
   {
      internal PeriodicUpdateChecker(ISynchronizeInvoke synchronizeInvoke)
      {
         startApplicationUpdateTimer(synchronizeInvoke);
      }

      public void Dispose()
      {
         stopApplicationUpdateTimer();
      }

      internal event Action NewVersionAvailable;

      private void startApplicationUpdateTimer(ISynchronizeInvoke synchronizeInvoke)
      {
         if (!_checkForUpdatesTimer.Enabled)
         {
            _checkForUpdatesTimer.Elapsed += onCheckForUpdatesTimer;
            _checkForUpdatesTimer.SynchronizingObject = synchronizeInvoke;
            _checkForUpdatesTimer.Start();
         }
      }

      private void stopApplicationUpdateTimer()
      {
         if (_checkForUpdatesTimer.Enabled)
         {
            _checkForUpdatesTimer.Stop();
            _checkForUpdatesTimer.Dispose();
         }
      }

      private void onNewVersionCopiedFromServer(string filePath, string versionNumber)
      {
         NewVersionAvailable?.Invoke();
      }

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

      private readonly System.Timers.Timer _checkForUpdatesTimer = new System.Timers.Timer
      {
         Interval = Constants.CheckForUpdatesTimerInterval
      };
   }
}
