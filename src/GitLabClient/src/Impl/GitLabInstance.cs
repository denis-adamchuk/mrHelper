using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class GitLabInstance : INetworkOperationStatusListener, IDisposable
   {
      public GitLabInstance(string hostname, IHostProperties hostProperties, ISynchronizeInvoke synchronizeInvoke)
      {
         HostProperties = hostProperties;
         HostName = hostname;
         _synchronizeInvoke = synchronizeInvoke;
         _modificationNotifier = new ModificationNotifier();
      }

      public void Dispose()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;
      }

      internal IHostProperties HostProperties { get; }
      internal string HostName { get; }
      internal IModificationListener ModificationListener => _modificationNotifier;
      internal IModificationNotifier ModificationNotifier => _modificationNotifier;
      internal INetworkOperationStatusListener NetworkOperationStatusListener => this;

      public void OnFailure()
      {
         if (isConnected())
         {
            startConnectionCheckingTimer();
            ConnectionLost?.Invoke();
         }
      }

      public void OnSuccess()
      {
         if (!isConnected())
         {
            stopConnectionCheckingTimer();
            ConnectionRestored?.Invoke();
         }
      }

      public event Action ConnectionLost;
      public event Action ConnectionRestored;

      private bool isConnected()
      {
         return _timer == null;
      }

      private void onTimer(object sender, ElapsedEventArgs e)
      {
         _synchronizeInvoke.BeginInvoke(new Action(async () =>
            {
               if (isConnected() || _checking)
               {
                  return;
               }
               _checking = true;

               try
               {
                  string token = HostProperties.GetAccessToken(HostName);
                  ConnectionCheckStatus result = await ConnectionChecker.CheckConnection(HostName, token);
                  if (!isConnected() && result == ConnectionCheckStatus.OK)
                  {
                     // connection has been probably already restored while we awaited for a check result
                     onConnectionRestored();
                  }
               }
               finally
               {
                  _checking = false;
               }
            }), null);
      }

      private void onConnectionRestored()
      {
         if (isConnected())
         {
            Debug.Assert(false);
            return;
         }

         stopConnectionCheckingTimer();
         ConnectionRestored?.Invoke();
      }

      private void startConnectionCheckingTimer()
      {
         Timer timer = new Timer
         {
            Interval = ConnectionCheckingTimerInterval  
         };
         timer.Elapsed += onTimer;
         timer.SynchronizingObject = _synchronizeInvoke;
         timer.Start();
         _timer = timer;
      }

      private void stopConnectionCheckingTimer()
      {
         _timer.Stop();
         _timer.Dispose();
         _timer = null;
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly ModificationNotifier _modificationNotifier;
      private bool _checking;

      private Timer _timer;
      private readonly int ConnectionCheckingTimerInterval = 30 * 1000; // 30 sec
   }
}

