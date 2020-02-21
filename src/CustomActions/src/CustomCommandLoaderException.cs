using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.CustomActions
{
   public class CustomCommandLoaderException : ExceptionEx
   {
      public CustomCommandLoaderException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }
}

