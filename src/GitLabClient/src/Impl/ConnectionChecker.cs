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
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public enum ConnectionCheckStatus
   {
      OK,
      BadHostname,
      BadAccessToken
   }

   public class ConnectionChecker : IConnectionLossListener, IDisposable
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

      public bool IsConnected(string hostname)
      {
         return !_timer.ContainsKey(hostname);
      }

      public void OnConnectionLost(string hostname)
      {
         if (IsConnected(hostname))
         {
            startConnectionCheckingTimer(hostname);
            ConnectionLost?.Invoke(hostname);
         }
      }

      public event Action<string> ConnectionLost;
      public event Action<string> ConnectionRestored;

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
               ConnectionCheckStatus result = await CheckConnection(hostname, _hostProperties.GetAccessToken(hostname));
               if (result == ConnectionCheckStatus.OK)
               {
                  onConnectionRestored(hostname);
               }
            }), null);
      }

      private void onConnectionRestored(string hostname)
      {
         Debug.Assert(!IsConnected(hostname));
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

      private readonly int ConnectionCheckingTimerInterval = 30 * 1000; // 30 sec
   }
}

