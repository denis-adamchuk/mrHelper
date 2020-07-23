using System;
using System.Diagnostics;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public class CustomTraceListener : TextWriterTraceListener
   {
      public CustomTraceListener(string filename, string firstRecord) : base(filename)
      {
         try
         {
            string directory = System.IO.Path.GetDirectoryName(filename);
            System.IO.Directory.CreateDirectory(directory);
         }
         catch (Exception ex)
         {
            throw new ArgumentException("Bad filename", ex);
         }

         Trace.AutoFlush = true;
         FileName = filename;
         _firstRecord = firstRecord;
      }

      public string FileName { get; }

      public override void Write(string x)
      {
         if (!_madeFirstRecord)
         {
            _madeFirstRecord = true;
            if (!String.IsNullOrEmpty(_firstRecord))
            {
               Trace.TraceInformation("----------------------------------------------------------------");
               Trace.TraceInformation(_firstRecord);
            }
         }

         base.Write(String.Format("{0} UTC: {1}", DateTime.UtcNow, x));
      }

      private readonly string _firstRecord;
      private bool _madeFirstRecord = false;
   }
}

