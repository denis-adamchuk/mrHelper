using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal struct ConvertedArguments
   {
      internal readonly string App;
      internal readonly string Arguments;

      internal ConvertedArguments(string app, string arguments)
      {
         App = app;
         Arguments = arguments;
      }
   }

   internal class ArgumentConversionException : ExceptionEx
   {
      internal ArgumentConversionException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   internal interface IGitCommandArgumentConverter
   {
      ConvertedArguments Convert(GitDiffArguments arguments);
      ConvertedArguments Convert(DiffToolArguments arguments);
   }
}

