using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class GitCommandException : ExceptionEx
   {
      internal GitCommandException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   internal class BadObjectException : GitCommandException
   {
      internal BadObjectException(string message)
         : base(message, null)
      {
      }
   }

   internal class GitCallFailedException : GitCommandException
   {
      internal GitCallFailedException(ExternalProcessFailureException ex)
         : base(String.Empty, ex)
      {
      }
   }

   internal class OperationCancelledException : GitCommandException
   {
      internal OperationCancelledException()
         : base(String.Empty, null)
      {
      }
   }

   internal class SystemException : GitCommandException
   {
      internal SystemException(Exception systemException)
         : base(String.Empty, systemException)
      {
      }
   }
}

