using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Exceptions
{
   public class ExternalProcessException : Exception
   {
      public ExternalProcessException(string command, int exitcode, IEnumerable<string> errorOutput)
      {
         Command = command;
         ExitCode = exitcode;
         Errors = errorOutput.ToArray();
      }

      public string Command { get; }
      public int ExitCode { get; }
      public IEnumerable<string> Errors{ get; }
   }
}

