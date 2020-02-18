using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Core.Context
{
   public class ContextMakingException : ExceptionEx
   {
      public ContextMakingException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }
}
