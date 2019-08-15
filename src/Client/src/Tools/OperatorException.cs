using System;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Tools
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

