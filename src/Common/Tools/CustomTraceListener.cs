using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public class CustomTraceListener : TextWriterTraceListener
   {
      public CustomTraceListener(string foldername, string filename)
         : base(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), foldername, filename))
      {
         Trace.AutoFlush = true;
      }

      public override void Write(string x)
      {
         base.Write(String.Format("{0} UTC: {1}", DateTime.UtcNow, x));
      }
   }
}

