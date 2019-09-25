using System;
using System.Diagnostics;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Tools
{
   public class ExceptionHandlers
   {
      static public void Handle(Exception exception, string meaning)
      {
         if (exception is OperatorException)
         {
            Debug.Assert(false);
            Trace.TraceError(meaning);
         }
         else if (exception is GitLabSharpException ex00)
         {
            Trace.TraceError("[{0}] {1}", ex00.GetType().ToString(), meaning);
            Trace.TraceError("{0}", ex00.Message);
            Trace.TraceError("Nested Exception: {0}", ex00.InternalException?.Message ?? "null");
         }
         else if (exception is GitLabClientCancelled)
         {
            Trace.TraceInformation("GitLab request was cancelled by a subsequent request");
         }
         else if (exception is GitLabRequestException ex1)
         {
            Trace.TraceError("[{0}] {1}", ex1.GetType().ToString(), meaning);
            Trace.TraceError("{0}", ex1.Message);
            Trace.TraceError("Nested Exception: {0}", ex1.WebException?.Message ?? "null");
         }
         else if (exception is DiffToolIntegrationException ex2)
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
         else
         {
            Trace.TraceError("[{0}] {1}: {2}", exception.GetType().ToString(), meaning, exception.Message);
         }
      }
   }
}

