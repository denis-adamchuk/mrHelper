using System;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient.Operators
{
   internal class OperatorException : ExceptionEx
   {
      internal OperatorException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }

      public override string UserMessage
      {
         get
         {
            if (InnerException is GitLabRequestException rx)
            {
               if (rx.InnerException is System.Net.WebException wx)
               {
                  if (wx.Response is System.Net.HttpWebResponse response
                   && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                  {
                     return wx.Message + " Check your access token!";
                  }
                  return wx.Message;
               }
               else if (rx.InnerException != null)
               {
                  return rx.InnerException.Message;
               }
            }
            return base.UserMessage;
         }
      }

      internal bool Cancelled => InnerException is GitLabTaskRunnerCancelled;
   }

   internal static class OperatorCallWrapper
   {
      internal static Task<T> Call<T>(Func<Task<T>> func)
      {
         return callTask(func);
      }

      internal static Task Call(Func<Task> func)
      {
         return callTask(func);
      }

      async private static Task<T> callTask<T>(Func<Task<T>> func)
      {
         try
         {
            return await func();
         }
         catch (GitLabTaskRunnerCancelled ex)
         {
            throw new OperatorException(ex);
         }
         catch (GitLabSharpException ex)
         {
            throw new OperatorException(ex);
         }
         catch (GitLabRequestException ex)
         {
            throw new OperatorException(ex);
         }
      }

      async private static Task callTask(Func<Task> func)
      {
         try
         {
            await func();
         }
         catch (GitLabTaskRunnerCancelled ex)
         {
            throw new OperatorException(ex);
         }
         catch (GitLabSharpException ex)
         {
            throw new OperatorException(ex);
         }
         catch (GitLabRequestException ex)
         {
            throw new OperatorException(ex);
         }
      }
   }
}

