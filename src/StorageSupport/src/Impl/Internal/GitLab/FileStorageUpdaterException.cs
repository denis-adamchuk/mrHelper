using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class FileStorageUpdaterException : ExceptionEx
   {
      internal FileStorageUpdaterException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   internal class FileStorageUpdateCancelledException : FileStorageUpdaterException
   {
      internal FileStorageUpdateCancelledException()
         : base(String.Empty, null)
      {
      }
   }
}

