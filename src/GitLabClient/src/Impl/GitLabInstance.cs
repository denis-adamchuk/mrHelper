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

         ModificationNotifier modificationNotifier = new ModificationNotifier();
         ModificationListener = modificationNotifier;
         ModificationNotifier = modificationNotifier;

         _synchronizeInvoke = synchronizeInvoke;
      }

      public void Dispose()
      {
         stopConnectionCheckingTimer();
      }

      internal IHostProperties HostProperties { get; }
      internal string HostName { get; }
      internal IModificationListener ModificationListener { get; }
      internal IModificationNotifier ModificationNotifier { get; }
      internal INetworkOperationStatusListener NetworkOperationStatusListener => this;

      public void OnFailure()
      {
         if (isConnected())
         {
            onConnectionLost();
         }
      }

      public void OnSuccess()
      {
         if (!isConnected())
         {
            onConnectionRestored();
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
                  ConnectionCheckStatus result = await ConnectionChecker.CheckConnectionAsync(HostName, token);
                  if (!isConnected() && result == ConnectionCheckStatus.OK)
                  {
                     // isConnected() check is needed because connection has been probably already restored
                     // while we awaited for a CheckConnectionAsync() result
                     Trace.TraceInformation("[GitLabInstance] ConnectionChecker.CheckConnection() returned OK");
                     onConnectionRestored();
                  }
               }
               finally
               {
                  _checking = false;
               }
            }), null);
      }

      private void onConnectionLost()
      {
         if (!isConnected())
         {
            Debug.Assert(false);
            return;
         }

         startConnectionCheckingTimer();
         Trace.TraceInformation("[GitLabInstance] Connection lost ({0})", HostName);
         ConnectionLost?.Invoke();
      }

      private void onConnectionRestored()
      {
         if (isConnected())
         {
            Debug.Assert(false);
            return;
         }

         stopConnectionCheckingTimer();
         Trace.TraceInformation("[GitLabInstance] Connection restored ({0})", HostName);
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

         Debug.Assert(_timer == null);
         _timer = timer;
      }

      private void stopConnectionCheckingTimer()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private bool _checking;

      private Timer _timer;
      private readonly int ConnectionCheckingTimerInterval = 30 * 1000; // 30 sec
   }
}

