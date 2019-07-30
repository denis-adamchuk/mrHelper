using System;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp;
using mrCore;

namespace mrHelperUI
{
   public class ExceptionHandlers
   {
      static public void Handle(Exception ex, string meaning, bool notify = true)
      {
         Trace.TraceError("{0} {1}: {2}", meaning, ex.ToString(), ex.Message);
         if (notify)
         {
            MessageBox.Show(meaning, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      static public void Handle(GitLabRequestException ex, string meaning, bool notify = true)
      {
         Trace.TraceError("{0} {1}: {2}\nNested WebException:{3}", meaning, ex.ToString(), ex.Message,
            ex.WebException.ToString());
         if (notify)
         {
            MessageBox.Show(meaning, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      static public void Handle(GitOperationException ex, string meaning, bool notify = true)
      {
         Trace.TraceError("{0} {1}: {2}\nDetails:\n{3}", meaning, ex.ToString(), ex.Message, ex.Details);
         if (notify)
         {
            MessageBox.Show(meaning, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}

