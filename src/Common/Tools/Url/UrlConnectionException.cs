using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public class UrlConnectionException : ExceptionEx
   {
      public UrlConnectionException(string message, Exception innerException = null)
         : base(message, innerException) { }
   }
}

