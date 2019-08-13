using System;

namespace mrHelper.Common.Exceptions
{
   public class CustomCommandLoaderException : Exception
   {
      public CustomCommandLoaderException(string message, Exception ex = null)
         : base(String.Format(message))
      {
         NestedException = ex;
      }

      public Exception NestedException { get; }
   }
}

