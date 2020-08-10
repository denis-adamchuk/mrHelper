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
         catch (Exception ex)
         {
            handleException(ex);
            throw;
         }
      }

      async private static Task callTask(Func<Task> func)
      {
         try
         {
            await func();
         }
         catch (Exception ex)
         {
            handleException(ex);
            throw;
         }
      }

      private static void handleException(Exception ex)
      {
         if (ex is GitLabTaskRunnerCancelled)
         {
            throw new OperatorException(ex);
         }
         else if (ex is GitLabSharpException || ex is GitLabRequestException)
         {
            throw new OperatorException(ex);
         }
      }
   }
}

