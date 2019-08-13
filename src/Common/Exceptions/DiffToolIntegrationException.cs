using System;

namespace mrHelper.Common.Exceptions
{
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

