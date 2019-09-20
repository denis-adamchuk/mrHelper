using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mrHelper.Core.Interprocess
{
   /// <summary>
   /// Parses command-line arguments to DiffToolInfo structure
   /// </summary>
   public class DiffArgumentParser
   {
      private static readonly Regex trimmedFileNameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);

      /// <summary>
      /// Loads command-line arguments into internal storage.
      /// Throws ArgumentException.
      /// </summary>
      public DiffArgumentParser(string[] arguments)
      {
         Debug.Assert(arguments[1] == "diff");

         if (arguments.Length == 4)
         {
            // Expected arguments (when a single file is opened in a diff tool):
            // (0) Current-pane file name with path 
            // (1) Current-pane line number 
            _arguments = new string[2];
            Array.Copy(arguments, 2, _arguments, 0, 2);
         }
         else
         {
            throw new ArgumentException(
               String.Format("Bad number of arguments ({0} were given, 4 expected)", arguments.Length));
         }
      }

      /// <summary>
      /// Creates DiffToolInfo structure.
      /// Throws ArgumentException.
      /// </summary>
      public DiffToolInfo Parse()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");

         if (!int.TryParse(_arguments[1], out int currentLineNumber))
         {
            throw new ArgumentException(
               String.Format("Bad argument \"{0}\" at position 1", _arguments[1]));
         }

         GroupCollection groupCollection = parsePath(tempFolder, _arguments[0]);
         return new DiffToolInfo
         {
            IsLeftSide = groupCollection[1].Value == "left",
            FileName = groupCollection[2].Value,
            LineNumber = currentLineNumber
         };
      }

      static private GroupCollection parsePath(string tempFolder, string fullFileName)
      {
         string trimmedFileName = fullFileName
            .Substring(tempFolder.Length, fullFileName.Length - tempFolder.Length)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

         Match m = trimmedFileNameRe.Match(trimmedFileName);
         if (!m.Success || m.Groups.Count < 3 || !m.Groups[1].Success || !m.Groups[2].Success)
         {
            throw new ArgumentException(
               String.Format("Cannot parse path \"{0}\" obtained from diff tool", fullFileName));
         }
         return m.Groups;
      }

      private readonly string[] _arguments;
   }
}
