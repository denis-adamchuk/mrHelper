using System;

namespace mrHelper.GitLabClient
{
   public class DataCacheException : mrHelper.Common.Exceptions.ExceptionEx
   {
      internal DataCacheException(string message, Exception innerException)
         : base(message, innerException) {}
   }

   public class DataCacheConnectionCancelledException : DataCacheException
   {
      internal DataCacheConnectionCancelledException()
         : base(String.Empty, null) {}
   }
}

