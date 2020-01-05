using System;
using System.Diagnostics;

namespace mrHelper.Common.Exceptions
{
   public class ExceptionHandlers
   {
      static public void Handle(Exception exception, string meaning)
      {
         if (exception is DiffToolIntegrationException ex2)
         {
            Trace.TraceError("[{0}] {1}", ex2.GetType().ToString(), meaning);
            Trace.TraceError("{0}", ex2.Message);
            Trace.TraceError("Nested Exception: {0}", (ex2.NestedException != null ? ex2.NestedException.Message : "N/A"));
         }
         else if (exception is CustomCommandLoaderException ex3)
         {
            Trace.TraceError("[{0}] {1}", ex3.GetType().ToString(), meaning);
            Trace.TraceError("{0}", ex3.Message);
            Trace.TraceError("Nested Exception: {0}", (ex3.NestedException != null ? ex3.NestedException.Message : "N/A"));
         }
         else if (exception is GitOperationException ex4)
         {
            Trace.TraceError("[{0}] {1}: {2}\nDetails:\n{3}",
                  ex4.GetType().ToString(), meaning, ex4.Message, ex4.Details);
         }
         else if (exception is FeedbackReporterException ex5)
         {
            Trace.TraceError("[{0}] {1}", ex5.GetType().ToString(), meaning);
            Trace.TraceError("{0}", ex5.Message);
            Trace.TraceError("Inner Exception: {0}", (ex5.InnerException != null ? ex5.InnerException.Message : "N/A"));
         }
         else if (exception != null)
         {
            Trace.TraceError("[{0}] {1}: {2}", exception.GetType().ToString(), meaning, exception.Message);
         }
      }
   }
}

