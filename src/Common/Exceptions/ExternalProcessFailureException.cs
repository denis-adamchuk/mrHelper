using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Exceptions
{
   public class ExternalProcessException : ExceptionEx
   {
      public ExternalProcessException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class ExternalProcessFailureException : ExternalProcessException
   {
      public ExternalProcessFailureException(string name, string arguments,
         int exitcode, IEnumerable<string> errorOutput)
         : base(String.Empty, null)
      {
         _name = name;
         _arguments = arguments;
         ExitCode = exitcode;
         Errors = errorOutput;
      }

      public override string Message
      {
         get
         {
            string brief = String.Format("Process \"{0}\" called with arguments \"{1}\" exited with code {2}",
               _name, _arguments, ExitCode.ToString());

            string errors = (Errors?.Count() ?? 0) > 0
               ? String.Format(" Details:\n{0}", String.Join("\n", Errors))
               : String.Empty;

            return String.Format("{0} {1}", brief, errors);
         }
      }

      private string _name { get; }
      private string _arguments { get; }
      public int ExitCode { get; }
      public IEnumerable<string> Errors{ get; }
   }

   public class ExternalProcessSystemException : ExternalProcessException
   {
      public ExternalProcessSystemException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }
}

