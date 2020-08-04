using System;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
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
   internal class BaseDataCacheLoader
   {
      internal BaseDataCacheLoader(DataCacheOperator op)
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
               Trace.TraceInformation(String.Format("[BaseDataCacheLoader] {0}", cancelMessage));
               throw new BaseLoaderCancelledException();
            }
            throw new BaseLoaderException(errorMessage, ex);
         }
      }

      internal DataCacheOperator _operator;
   }
}

