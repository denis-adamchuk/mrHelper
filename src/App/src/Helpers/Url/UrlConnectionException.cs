using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Helpers
{
   public class UrlConnectionException : ExceptionEx
   {
      internal UrlConnectionException(string message, Exception innerException = null)
         : base(message, innerException) { }
   }
}

