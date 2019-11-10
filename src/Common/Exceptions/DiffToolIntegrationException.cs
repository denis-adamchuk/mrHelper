using System;

namespace mrHelper.Common.Exceptions
{
   public class DiffToolNotInstalledException : Exception
   {
      public DiffToolNotInstalledException(string message, Exception ex = null)
         : base(String.Format(message))
      {
         NestedException = ex;
      }

      public Exception NestedException { get; }
   }

   public class DiffToolIntegrationException : Exception
   {
      public DiffToolIntegrationException(string message, Exception ex = null)
         : base(String.Format(message))
      {
         NestedException = ex;
      }

      public Exception NestedException { get; }
   }
}

