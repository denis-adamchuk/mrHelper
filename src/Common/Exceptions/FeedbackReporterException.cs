using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Exceptions
{
   public class FeedbackReporterException : Exception
   {
      public FeedbackReporterException(string message, Exception ex) : base(message, ex) { }
   }
}

