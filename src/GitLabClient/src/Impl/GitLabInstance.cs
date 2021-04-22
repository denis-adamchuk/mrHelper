using System;
using System.Timers;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
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

      async public Task<bool?> IsApprovalStatusSupported()
      {
         if (_isApprovalStatusSupportedCached.HasValue)
         {
            return _isApprovalStatusSupportedCached.Value;
         }

         GitLabVersion version = await getVersionAsync();
         if (version != null)
         {
            _isApprovalStatusSupportedCached = isApprovalStatusSupported(version);
            return _isApprovalStatusSupportedCached.Value;
         }
         return null;
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

      // TODO_MF Implement this using RawDataAccessor to trace connection loss
      async private Task<GitLabVersion> getVersionAsync()
      {
         string token = HostProperties.GetAccessToken(HostName);
         using (GitLabTaskRunner client = new GitLabTaskRunner(HostName, token))
         {
            try
            {
               return (GitLabVersion)(await client.RunAsync(async (gl) => await gl.Version.LoadTaskAsync()));
            }
            catch (GitLabSharpException ex)
            {
               ExceptionHandlers.Handle("Cannot obtain GitLab server version", ex);
            }
            catch (GitLabRequestException ex)
            {
               ExceptionHandlers.Handle("Cannot obtain GitLab server version", ex);
            }
            catch (GitLabTaskRunnerCancelled)
            {
            }
            return null;
         }
      }

      private static readonly Regex GitLabVersionRegex = new Regex(
         @"(?'major_version'\d*)\.(?'minor_version'\d*)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

      private readonly System.Version EarliestGitLabVersionWithApprovalsSupport = new System.Version(13, 6);

      /// <summary>
      /// This is a VERY simplified way of functionality checking because GitLab has complicated editions and plans.
      /// TODO: Make approval status support check better.
      /// </summary>
      private bool isApprovalStatusSupported(GitLabVersion version)
      {
         Debug.Assert(version != null);

         Match m = GitLabVersionRegex.Match(version.Version);
         if (m.Success
          && m.Groups["major_version"].Success
          && int.TryParse(m.Groups["major_version"].Value, out int major_version)
          && m.Groups["minor_version"].Success
          && int.TryParse(m.Groups["minor_version"].Value, out int minor_version))
         {
            System.Version gitLabVersion = new System.Version(major_version, minor_version);
            return gitLabVersion >= EarliestGitLabVersionWithApprovalsSupport;
         }
         return false;
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private bool _checking;
      private bool? _isApprovalStatusSupportedCached;

      private Timer _timer;
      private readonly int ConnectionCheckingTimerInterval = 30 * 1000; // 30 sec
   }
}

