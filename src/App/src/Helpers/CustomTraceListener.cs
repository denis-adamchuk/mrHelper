using System;
using System.Diagnostics;

namespace mrHelper.App
{
   public class CustomTraceListener : TextWriterTraceListener
   {
      public CustomTraceListener(string filename)
         : base(filename)
      {
      }

      public override void Write(string x)
      {
         base.Write(String.Format("{0} UTC: {1}", DateTime.UtcNow, x));
      }
   }
}

