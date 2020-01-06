using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Common
{
   internal static class GitLabExceptionHandlers
   {
      internal static void Handle(Exception exception, string meaning)
      {
         if (exception is GitLabSharpException ex00)
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
      }
   }
}

