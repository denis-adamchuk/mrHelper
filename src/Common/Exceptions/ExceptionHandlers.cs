using System;
using System.Diagnostics;

namespace mrHelper.Common.Exceptions
{
   public class ExceptionHandlers
   {
      static public void Handle(string meaning, Exception exception)
      {
         if (exception == null)
         {
            Trace.TraceError("[null] {0}", meaning);
         }
         else if (String.IsNullOrWhiteSpace(exception.Message))
         {
            Trace.TraceError("[{0}] {1}: N/A", exception.GetType().ToString(), meaning);
         }
         else
         {
            Trace.TraceError("[{0}] {1}: {2}", exception.GetType().ToString(), meaning, exception.Message);
         }
      }
   }
}

