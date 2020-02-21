using System;

namespace mrHelper.Client.Common
{
   internal class OperatorException : Exception
   {
      internal OperatorException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }
}

