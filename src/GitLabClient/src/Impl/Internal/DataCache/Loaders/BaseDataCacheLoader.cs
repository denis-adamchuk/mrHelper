using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class BaseLoaderException : ExceptionEx
   {
      internal BaseLoaderException(string message, Exception innerException)
         : base(message, innerException) { }

      internal System.Net.HttpWebResponse GetWebResponse()
      {
         if (InnerException?.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response;
            }
         }
         return null;
      }

   }

   internal class BaseLoaderCancelledException : BaseLoaderException
   {
      internal BaseLoaderCancelledException()
         : base(String.Empty, null) { }
   }

   internal class BaseLoader
   {
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
   }

   internal class BaseDataCacheLoader : BaseLoader
   {
      internal BaseDataCacheLoader(DataCacheOperator op)
      {
         _operator = op;
      }

      internal DataCacheOperator _operator;
   }
}

