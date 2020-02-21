using System;
using System.Linq;
using System.Diagnostics;

namespace mrHelper.Common.Exceptions
{
   public class ExceptionHandlers
   {
      static public void Handle(string meaning, Exception exception)
      {
         Trace.TraceError("[{0}] {1}: {2}",
            exception.GetType().ToString(), meaning, String.IsNullOrEmpty(exception.Message) ? "N/A" : exception.Message);
      }
   }
}

