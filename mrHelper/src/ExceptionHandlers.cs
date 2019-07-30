using System;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp;
using mrCore;

namespace mrHelperUI
{
   public class ExceptionHandlers
   {
      static public void Handle(Exception ex, string meaning, bool show = true)
      {
         Trace.TraceError("[{0}] {1}: {2}", ex.ToString(), meaning, ex.Message);
         showMessageBox(meaning, show);
      }

      static public void Handle(GitLabRequestException ex, string meaning, bool show = true)
      {
         Trace.TraceError("[{0}] {1}: {2}\nNested WebException: {3}",
            ex.ToString(), meaning, ex.Message, ex.WebException.Message);
         showMessageBox(meaning, show);
      }

      static public void Handle(GitOperationException ex, string meaning, bool show = true)
      {
         Trace.TraceError("[{0}] {1}: {2}\nDetails:\n{3}", ex.ToString(), meaning, ex.Message, ex.Details);
         showMessageBox(meaning, show);
      }

      static public void HandleUnhandled(Exception ex)
      {
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
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

