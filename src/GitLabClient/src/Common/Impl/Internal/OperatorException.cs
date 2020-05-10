using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Common
{
   internal class OperatorException : ExceptionEx
   {
      internal OperatorException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }
}

