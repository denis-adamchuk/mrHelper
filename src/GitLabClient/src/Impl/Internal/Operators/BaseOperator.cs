using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Interfaces;
using mrHelper.GitLabClient.Operators.Search;

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

      /*
         protected Task<T> callWithSharedClient<T>(Func<GitLabTaskRunner, Task<T>> func)
         {
            return callWithSharedClientRecursive(func, 1);
         }

         async protected Task<T> callWithSharedClientRecursive<T>(Func<GitLabTaskRunner, Task<T>> func, int attempt)
         {
            try
            {
               return await func(_client);
            }
            catch (OperatorException ex)
            {
               if (!isConnectionFailureException(ex))
               {
                  throw;
               }
            }

            if (attempt == 1)
            {
               _connectionLossListener?.OnConnectionLost(Hostname);
            }
            await Task.Delay(30000);
            T result = await callWithSharedClientRecursive(func, attempt + 1);
            if (attempt == 1)
            {
               _connectionLossListener?.OnConnectionRestored(Hostname);
            }
            return result;
         }
      */

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

      internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(SearchQuery searchQuery)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<MergeRequest>)(await client.RunAsync(
                        async (gl) =>
                           await (new MergeRequestSearchProcessor(searchQuery).Process(gl))))));
      }

      internal Task<User> SearchCurrentUserAsync()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (User)await client.RunAsync(
                        async (gl) =>
                           await gl.CurrentUser.LoadTaskAsync())));
      }

      private readonly IHostProperties _settings;
      private readonly GitLabTaskRunner _client;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

