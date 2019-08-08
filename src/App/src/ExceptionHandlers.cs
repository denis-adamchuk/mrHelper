using System;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp;
using mrHelper.Core;
using mrHelper.CustomActions;
using mrHelper.DiffTool;

namespace mrHelper.Client
{
   public class ExceptionHandlers
   {
      static public void Handle(Exception exception, string meaning, bool show = true)
      {
         if (exception is GitLabRequestException ex1)
         {
            Trace.TraceError("[{0}] {1}: {2}\nNested WebException: {3}",
                  ex1.GetType().ToString(), meaning, ex1.Message, ex1.WebException.Message);
         }
         else if (exception is DiffToolIntegrationException ex2)
         {
            Trace.TraceError("[{0}] {1}: {2}\nNested Exception: {3}",
                  ex2.GetType().ToString(), meaning, ex2.Message,
                  (ex2.NestedException != null ? ex2.NestedException.Message : "N/A"));
         }
         else if (exception is CustomCommandLoaderException ex3)
         {
            Trace.TraceError("[{0}] {1}: {2}\nNested Exception: {3}",
                  ex3.GetType().ToString(), meaning, ex3.Message,
                  (ex3.NestedException != null ? ex3.NestedException.Message : "N/A"));
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
         showMessageBox(meaning, show);
      }

      static public void HandleUnhandled(Exception ex, bool show)
      {
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
         showMessageBox("Fatal error occurred, see details in log file", show);
         Application.Exit();
      }

      static private void showMessageBox(string text, bool show)
      {
         if (show)
         {
            MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}

