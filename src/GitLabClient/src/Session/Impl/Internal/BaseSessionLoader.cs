using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Session
{
   internal class BaseLoaderException : ExceptionEx
   {
      internal BaseLoaderException(string message, Exception innerException)
         : base(message, innerException) { }
   }

   internal class BaseLoaderCancelledException : BaseLoaderException
   {
      internal BaseLoaderCancelledException()
         : base(String.Empty, null) { }
   }

   /// <summary>
   /// </summary>
   internal class BaseSessionLoader
   {
      internal BaseSessionLoader(SessionOperator op)
      {
         _operator = op;
      }

      async protected static Task<T> call<T>(Func<Task<T>> func, string cancelMessage, string errorMessage)
      {
         try
         {
            return await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[BaseSessionLoader] {0}", cancelMessage));
               throw new BaseLoaderCancelledException();
            }
            throw new BaseLoaderException(errorMessage, ex);
         }
      }

      internal SessionOperator _operator;
   }
}

