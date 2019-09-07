using System;
using System.Collections.Generic;

namespace mrHelper.Common.Exceptions
{
   public class GitOperationException : Exception
   {
      public GitOperationException(string command, int exitcode, List<string> errorOutput)
         : base(String.Format("command \"{0}\" exited with code {1}", command, exitcode.ToString()))
      {
         Details = String.Join("\n", errorOutput);
         ExitCode = exitcode;
      }

      public string Details { get; }
      public int ExitCode { get; }
      public bool Cancelled { get; set; } = false;
   }

   public class GitObjectException : Exception
   {
      public GitObjectException(string message) : base(message) { }
   }
}

