using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public class GitException : ExceptionEx
   {
      public GitException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class BadObjectException : GitException
   {
      public BadObjectException(string message)
         : base(message, null)
      {
      }
   }

   public class GitCallFailedException : GitException
   {
      public GitCallFailedException(ExternalProcessFailureException ex)
         : base(String.Empty, ex)
      {
      }
   }

   public class OperationCancelledException : GitException
   {
      public OperationCancelledException()
         : base(String.Empty, null)
      {
      }
   }

   public class SystemException : GitException
   {
      public SystemException(Exception systemException)
         : base(String.Empty, systemException)
      {
      }
   }
}

