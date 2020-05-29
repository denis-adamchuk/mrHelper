using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Common
{
   internal class OperatorException : ExceptionEx
   {
      internal OperatorException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }

      internal bool Cancelled => InnerException is GitLabTaskRunnerCancelled;
   }

   internal static class OperatorCallWrapper
   {
      internal static Task<T> Call<T>(Func<Task<T>> func)
      {
         return callTask(func, true);
      }

      internal static Task Call(Func<Task> func)
      {
         return callTask(func, true);
      }

      internal static Task<T> CallNoCancel<T>(Func<Task<T>> func)
      {
         return callTask(func, false);
      }

      internal static Task CallNoCancel(Func<Task> func)
      {
         return callTask(func, false);
      }

      async private static Task<T> callTask<T>(Func<Task<T>> func, bool enabledCancel)
      {
         try
         {
            return await func();
         }
         catch (Exception ex)
         {
            handleException(ex, enabledCancel);
            throw;
         }
      }

      async private static Task callTask(Func<Task> func, bool enabledCancel)
      {
         try
         {
            await func();
         }
         catch (Exception ex)
         {
            handleException(ex, enabledCancel);
            throw;
         }
      }

      private static void handleException(Exception ex, bool enabledCancel)
      {
         if (ex is GitLabTaskRunnerCancelled)
         {
            Debug.Assert(enabledCancel);
            throw new OperatorException(ex);
         }
         else if (ex is GitLabSharpException || ex is GitLabRequestException)
         {
            throw new OperatorException(ex);
         }
      }
   }
}

