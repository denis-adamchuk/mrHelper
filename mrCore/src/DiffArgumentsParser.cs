using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mrCore
{
   public struct DiffToolInfo
   {
      public struct Side
      {
         public string FileName;
         public int LineNumber;

         public Side(string filename, int linenumber)
         {
            FileName = filename;
            LineNumber = linenumber;
         }
      }

      public bool IsValid()
      {
         if (!Left.HasValue && !Right.HasValue)
         {
            return false;
         }
         if (IsLeftSideCurrent && (!Left.HasValue || Left.Value.FileName == null))
         {
            return false;
         }
         if (!IsLeftSideCurrent && (!Right.HasValue || Right.Value.FileName == null))
         {
            return false;
         }
         return true;
      }

      public Side? Left;
      public Side? Right;
      public bool IsLeftSideCurrent;
   }

   // It expected that one of paths has word 'right' and another one 'left' (git difftool --dir-diff makes them)
   public class DiffArgumentsParser
   {
      private static readonly Regex trimmedFileNameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);

      public DiffArgumentsParser(string[] arguments)
      {
         if (arguments.Length == 6)
         {
            // Expected arguments (when comparing two files):
            // (0) Current-pane file name with path 
            // (1) Current-pane line number 
            // (2) Next-pane file name with path 
            // (3) Next-pane line number
            _arguments = new string[4];
            Array.Copy(arguments, 2, _arguments, 0, 4);
         }
         else if (arguments.Length == 5)
         {
            // Expected arguments (when a single file is opened in a diff tool):
            // (0) Current-pane file name with path 
            // (1) Current-pane line number 
            _arguments = new string[2];
            Array.Copy(arguments, 2, _arguments, 0, 2);
         }
         else
         {
            throw new ApplicationException("Bad number of arguments");
         }
      }

      public DiffToolInfo Parse()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");

         if (!int.TryParse(_arguments[1], out int currentLineNumber))
         {
            throw new ApplicationException("Bad argument \"" + _arguments[1] + "\" at position 1");
         }

         int nextLineNumber = 0;
         if (_arguments.Length > 2 && !int.TryParse(_arguments[3], out nextLineNumber))
         {
            throw new ApplicationException("Bad argument \"" + _arguments[3] + "\" at position 3");
         }

         DiffToolInfo.Side? current = new DiffToolInfo.Side(
            convertToGitFileName(tempFolder, _arguments[0]), currentLineNumber);

         DiffToolInfo.Side? next = _arguments.Length > 2
            ? new DiffToolInfo.Side(convertToGitFileName(tempFolder, _arguments[2]), nextLineNumber)
            : new Nullable<DiffToolInfo.Side>();

         DiffToolInfo toolInfo;

         if (checkIfLeftSideFile(tempFolder, _arguments[0]))
         {
            toolInfo.IsLeftSideCurrent = true;
            toolInfo.Left = current;
            toolInfo.Right = next;
         }
         else
         {
            toolInfo.IsLeftSideCurrent = false;
            toolInfo.Left = next;
            toolInfo.Right = current;
         }

         return toolInfo;
      }

      static private bool checkIfLeftSideFile(string tempFolder, string fullFileName)
      {
         return parsePath(tempFolder, fullFileName)[1].Value == "left";
      }

      static private string convertToGitFileName(string tempFolder, string fullFileName)
      {
         return parsePath(tempFolder, fullFileName)[2].Value;
      }

      static private string trimTemporaryFolder(string tempFolder, string fullFileName)
      {
         string trimmedFileName = fullFileName
            .Substring(tempFolder.Length, fullFileName.Length - tempFolder.Length)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
         return trimmedFileName;
      }

      static private GroupCollection parsePath(string tempFolder, string fullFileName)
      {
         string trimmed = trimTemporaryFolder(tempFolder, fullFileName);

         Match m = trimmedFileNameRe.Match(trimmed);
         if (!m.Success || m.Groups.Count < 3 || !m.Groups[1].Success || !m.Groups[2].Success)
         {
            throw new ApplicationException("Cannot parse a path obtained from diff tool");
         }
         return m.Groups;
      }

      private readonly string[] _arguments;
   }
}
