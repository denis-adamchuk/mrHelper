using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using mrHelper.Core.Matching;

namespace mrHelper.App.Interprocess
{
   /// <summary>
   /// Parses command-line arguments to LineMatchInfo structure
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

         if (arguments.Length != 5)
         {
            throw new ArgumentException(
               String.Format("Bad number of arguments ({0} were given, 4 or 5 are expected)", arguments.Length));
         }

         if (arguments[4] != String.Empty)
         {
            // Expected arguments (when comparing two files):
            // (0) Current-pane file name with path 
            // (1) Current-pane line number 
            // (2) Next-pane file name with path 
            _arguments = new string[3];
            Array.Copy(arguments, 2, _arguments, 0, 3);
         }
         else
         {
            // Expected arguments (when a single file is opened in a diff tool):
            // (0) Current-pane file name with path 
            // (1) Current-pane line number 
            _arguments = new string[2];
            Array.Copy(arguments, 2, _arguments, 0, 2);
         }
      }

      /// <summary>
      /// Creates LineMatchInfo structure.
      /// Throws ArgumentException.
      /// </summary>
      public MatchInfo Parse()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");

         if (!int.TryParse(_arguments[1], out int currentLineNumber))
         {
            throw new ArgumentException(
               String.Format("Bad argument \"{0}\" at position 1", _arguments[1]));
         }

         GroupCollection groupCollection = parsePath(tempFolder, _arguments[0]);
         bool isLeftSide = groupCollection[1].Value == "left";
         string currentFileName = groupCollection[2].Value;
         string nextFileName = _arguments.Length > 2 ? parsePath(tempFolder, _arguments[2])[2].Value : String.Empty;
         return new MatchInfo
         {
            IsLeftSideLineNumber = isLeftSide,
            LeftFileName = isLeftSide ? currentFileName : nextFileName,
            RightFileName = isLeftSide ? nextFileName : currentFileName,
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
