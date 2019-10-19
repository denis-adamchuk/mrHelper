using System;
using System.Diagnostics;

namespace mrHelper.CommonTools
{
   public class CustomTraceListener : TextWriterTraceListener
   {
      public CustomTraceListener(string filename, string firstRecord) : base(filename)
      {
         Trace.AutoFlush = true;
         _firstRecord = firstRecord;
      }

      public override void Write(string x)
      {
         if (!_madeFirstRecord)
         {
            _madeFirstRecord = true;
            Trace.TraceInformation("----------------------------------------------------------------");
            Trace.TraceInformation(_firstRecord);
         }

         base.Write(String.Format("{0} UTC: {1}", DateTime.UtcNow, x));
      }

      private string _firstRecord;
      private bool _madeFirstRecord = false;
   }
}

