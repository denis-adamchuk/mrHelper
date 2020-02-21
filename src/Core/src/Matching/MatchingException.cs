using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Core.Matching
{
   public class MatchingException : ExceptionEx
   {
      public MatchingException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }
}
