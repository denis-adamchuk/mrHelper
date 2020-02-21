using System;
using System.Collections.Generic;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   internal class GitException : ExceptionEx
   {
      internal GitException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   internal class BadObjectException : GitException
   {
      internal BadObjectException(string message)
         : base(message, null)
      {
      }
   }

   internal class GitCallFailedException : GitException
   {
      internal GitCallFailedException(ExternalProcessFailureException ex)
         : base(String.Empty, ex)
      {
      }
   }

   internal class OperationCancelledException : GitException
   {
      internal OperationCancelledException()
         : base(String.Empty, null)
      {
      }
   }

   internal class SystemException : GitException
   {
      internal SystemException(Exception systemException)
         : base(String.Empty, systemException)
      {
      }
   }
}

