using System;
using System.Threading.Tasks;
using GitLabSharp;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class BaseOperator : IDisposable
   {
      internal BaseOperator(string hostname, IHostProperties hostProperties,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = hostProperties;
         Hostname = hostname;
         _client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      public void Dispose()
      {
         _client?.Dispose();
         _client = null;
      }

      async protected Task<T> callWithSharedClient<T>(Func<GitLabTaskRunner, Task<T>> func)
      {
         if (_client == null)
         {
            return default(T);
         }

         try
         {
            T result = await func(_client);
            _networkOperationStatusListener?.OnSuccess();
            return result;
         }
         catch (OperatorException ex)
         {
            if (isConnectionFailureException(ex))
            {
               _networkOperationStatusListener?.OnFailure();
            }
            else if (!ex.Cancelled)
            {
               _networkOperationStatusListener?.OnSuccess();
            }
            throw;
         }
      }

      private bool isConnectionFailureException(OperatorException ex)
      {
         if (ex.InnerException is GitLabSharp.Accessors.GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               return wx.Status != System.Net.WebExceptionStatus.Success && wx.Response == null;
            }
            return rx.InnerException is System.TimeoutException;
         }
         return false;
      }

      protected string Hostname { get; }

      private readonly IHostProperties _settings;
      private GitLabTaskRunner _client;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

