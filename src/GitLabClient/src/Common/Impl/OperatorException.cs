using System;

namespace mrHelper.Client.Common
{
   internal class OperatorException : Exception
   {
      internal OperatorException(Exception ex)
      {
         InternalException = ex;
      }

      internal Exception InternalException;
   }
}

