using System;
using System.Diagnostics;

namespace mrHelper.App.Helpers
{
   internal class CustomTraceListener : TextWriterTraceListener
   {
      internal CustomTraceListener(string filename)
         : base(filename)
      {
      }

      internal override void Write(string x)
      {
         base.Write(String.Format("{0} UTC: {1}", DateTime.UtcNow, x));
      }
   }
}

