using mrHelper.Common.Exceptions;
using System;

namespace mrHelper.DiffTool
{
   public class DiffToolIntegrationException : ExceptionEx
   {
      public DiffToolIntegrationException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class DiffToolNotInstalledException : DiffToolIntegrationException
   {
      public DiffToolNotInstalledException(string message)
         : base(message, null)
      {
      }
   }
}

