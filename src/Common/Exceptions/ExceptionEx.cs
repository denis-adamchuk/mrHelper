using System;

namespace mrHelper.Common.Exceptions
{
   public class ExceptionEx : Exception
   {
      public ExceptionEx(string message, Exception innerException)
         : base(message, innerException)
      {
      }

      public override string Message
      {
         get
         {
            string innerExceptionMessage = InnerException != null
               ? String.Format("\n[{0}] {1}",
                  InnerException.GetType().ToString(), InnerException.Message)
               : String.Empty;
            return String.Format("{0} {1}", base.Message, innerExceptionMessage);
         }
      }

      public string OriginalMessage => base.Message;

      public virtual string UserMessage => InnerException is ExceptionEx inex ? inex.UserMessage : OriginalMessage;
   }
}

