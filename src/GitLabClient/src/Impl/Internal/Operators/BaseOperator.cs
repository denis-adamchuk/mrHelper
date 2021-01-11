using System;
using System.Threading.Tasks;
using GitLabSharp;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class BaseOperator : IDisposable
   {
      internal BaseOperator(string hostname, IHostProperties hostProperties,
         IConnectionLossListener connectionLossListener)
      {
         _settings = hostProperties;
         Hostname = hostname;
         _client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         _connectionLossListener = connectionLossListener;
      }

      async protected Task<T> callWithSharedClient<T>(Func<GitLabTaskRunner, Task<T>> func)
      {
         try
         {
            return await func(_client);
         }
         catch (OperatorException ex)
         {
            if (isConnectionFailureException(ex))
            {
               _connectionLossListener?.OnConnectionLost(Hostname);
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

      public void Dispose()
      {
         _client.Dispose();
      }

      private readonly IHostProperties _settings;
      private readonly GitLabTaskRunner _client;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

