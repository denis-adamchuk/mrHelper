using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mrCore
{
   public struct DiffToolInfo
   {
      public string LeftSideFileNameBrief;
      public int LeftSideLineNumber;
      public string RightSideFileNameBrief;
      public int RightSideLineNumber;
      public bool IsLeftSideCurrent;
   }

   // This class expects 4 arguments obtained from a two-pane diff tool:
   // (0) Current-pane file name with path 
   // (1) Current-pane line number 
   // (2) Next-pane file name with path 
   // (3) Next-pane line number
   // It also expected that one of paths has word 'right' and another one 'left' (git difftool --dir-diff makes them)
   public class DiffArgumentsParser
   {
      static Regex trimmedFileNameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);

      public DiffArgumentsParser(string[] arguments)
      {
         _arguments = arguments;
         if (_arguments.Length != 4)
         {
            throw new ApplicationException("Bad number of arguments");
         }
      }

      public DiffToolInfo Parse()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");

         DiffToolInfo toolInfo;
         string currentFilePath = _arguments[0];
         string nextFilePath = _arguments[2];

         if (checkIfLeftSideFile(tempFolder, currentFilePath))
         {
            toolInfo.IsLeftSideCurrent = true;
            toolInfo.LeftSideFileNameBrief = convertToGitlabFileName(tempFolder, currentFilePath);
            toolInfo.LeftSideLineNumber = int.Parse(_arguments[1]);
            toolInfo.RightSideFileNameBrief = convertToGitlabFileName(tempFolder, nextFilePath);
            toolInfo.RightSideLineNumber = int.Parse(_arguments[3]);
         }
         else
         {
            toolInfo.IsLeftSideCurrent = false;
            toolInfo.LeftSideFileNameBrief = convertToGitlabFileName(tempFolder, nextFilePath);
            toolInfo.LeftSideLineNumber = int.Parse(_arguments[3]);
            toolInfo.RightSideFileNameBrief = convertToGitlabFileName(tempFolder, currentFilePath);
            toolInfo.RightSideLineNumber = int.Parse(_arguments[1]);
         }

         return toolInfo;
      }

      static private bool checkIfLeftSideFile(string tempFolder, string fullFileName)
      {
         return parsePath(tempFolder, fullFileName)[1].Value == "left";
      }

      static private string convertToGitlabFileName(string tempFolder, string fullFileName)
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

      string[] _arguments;
   }
}
