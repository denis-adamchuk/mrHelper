using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public enum ConnectionCheckStatus
   {
      OK,
      BadHostname,
      BadAccessToken
   }

   public class ConnectionChecker : INetworkOperationStatusListener, IDisposable
   {
      public ConnectionChecker(IHostProperties hostProperties, ISynchronizeInvoke synchronizeInvoke)
      {
         _hostProperties = hostProperties;
         _synchronizeInvoke = synchronizeInvoke;
      }

      public void Dispose()
      {
         foreach (KeyValuePair<string, Timer> kv in _timer)
         {
            kv.Value.Stop();
            kv.Value.Dispose();
         }
         _timer.Clear();
      }

      async public Task<ConnectionCheckStatus> CheckConnection(string hostname, string token)
      {
         using (GitLabTaskRunner client = new GitLabTaskRunner(hostname, token))
         {
            try
            {
               await client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync());
               return ConnectionCheckStatus.OK;
            }
            catch (Exception ex)
            {
               if (ex.InnerException is GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     if (wx.Response is System.Net.HttpWebResponse response
                      && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                        return ConnectionCheckStatus.BadAccessToken;
                     }
                  }
               }
            }
            return ConnectionCheckStatus.BadHostname;
         }
      }

      public void OnFailure(string hostname)
      {
         if (isConnected(hostname))
         {
            startConnectionCheckingTimer(hostname);
            ConnectionLost?.Invoke(hostname);
         }
      }

      public void OnSuccess(string hostname)
      {
         if (!isConnected(hostname))
         {
            stopConnectionCheckingTimer(hostname);
            ConnectionRestored?.Invoke(hostname);
         }
      }

      public event Action<string> ConnectionLost;
      public event Action<string> ConnectionRestored;

      private bool isConnected(string hostname)
      {
         return !_timer.ContainsKey(hostname);
      }

      private void onTimer(object sender, ElapsedEventArgs e)
      {
         string hostname = _timer.SingleOrDefault(timer => timer.Value == sender).Key;
         if (hostname == null)
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action(async () =>
            {
               Debug.Assert(!isConnected(hostname));
               if (_checking.Add(hostname))
               {
                  return;
               }

               try
               {
                  string token = _hostProperties.GetAccessToken(hostname);
                  ConnectionCheckStatus result = await CheckConnection(hostname, token);
                  if (!isConnected(hostname) && result == ConnectionCheckStatus.OK)
                  {
                     // connection has been probably already restored while we awaited for a check result
                     onConnectionRestored(hostname);
                  }
               }
               finally
               {
                  _checking.Remove(hostname);
               }
            }), null);
      }

      private void onConnectionRestored(string hostname)
      {
         if (isConnected(hostname))
         {
            Debug.Assert(false);
            return;
         }

         stopConnectionCheckingTimer(hostname);
         ConnectionRestored?.Invoke(hostname);
      }

      private void startConnectionCheckingTimer(string hostname)
      {
         Timer timer = new System.Timers.Timer
         {
            Interval = ConnectionCheckingTimerInterval
         };
         timer.Elapsed += onTimer;
         timer.SynchronizingObject = _synchronizeInvoke;
         timer.Start();
         _timer[hostname] = timer;
      }

      private void stopConnectionCheckingTimer(string hostname)
      {
         _timer[hostname].Stop();
         _timer[hostname].Dispose();
         _timer.Remove(hostname);
      }

      private readonly IHostProperties _hostProperties;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly Dictionary<string, Timer> _timer = new Dictionary<string, Timer>();
      private readonly HashSet<string> _checking = new HashSet<string>();

      private readonly int ConnectionCheckingTimerInterval = 30 * 1000; // 30 sec
   }
}

