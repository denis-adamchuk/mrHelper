using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mrHelper
{
   public struct DiffToolInfo
   {
      public string LeftSideFileNameBrief;
      public string LeftSideFileNameFull;
      public int LeftSideLineNumber;
      public string RightSideFileNameBrief;
      public string RightSideFileNameFull;
      public int RightSideLineNumber;
      public bool IsLeftSideCurrent;
   }

   class DiffArgumentsParser
   {
      static Regex trimmedFilenameRe = new Regex(@".*\/(right|left)\/(.*)", RegexOptions.Compiled);

      public DiffArgumentsParser(string[] arguments)
      {
         _arguments = arguments;
         Debug.Assert(arguments.Length == 6);
      }

      public DiffToolInfo Parse()
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");

         DiffToolInfo toolInfo;
         string currentFilePath = _arguments[2];
         string nextFilePath = _arguments[4];

         if (checkIfLeftSideFile(tempFolder, currentFilePath))
         {
            toolInfo.IsLeftSideCurrent = true;
            toolInfo.LeftSideFileNameFull = currentFilePath;
            toolInfo.LeftSideFileNameBrief = convertToGitlabFilename(tempFolder, currentFilePath);
            toolInfo.LeftSideLineNumber = int.Parse(_arguments[3]);
            toolInfo.RightSideFileNameFull = nextFilePath;
            toolInfo.RightSideFileNameBrief = convertToGitlabFilename(tempFolder, nextFilePath);
            toolInfo.RightSideLineNumber = int.Parse(_arguments[5]);
         }
         else
         {
            toolInfo.IsLeftSideCurrent = false;
            toolInfo.LeftSideFileNameFull = nextFilePath;
            toolInfo.LeftSideFileNameBrief = convertToGitlabFilename(tempFolder, nextFilePath);
            toolInfo.LeftSideLineNumber = int.Parse(_arguments[5]);
            toolInfo.RightSideFileNameFull = currentFilePath;
            toolInfo.RightSideFileNameBrief = convertToGitlabFilename(tempFolder, currentFilePath);
            toolInfo.RightSideLineNumber = int.Parse(_arguments[3]);
         }

         return toolInfo;
      }

      static private bool checkIfLeftSideFile(string tempFolder, string fullFilename)
      {
         return parsePath(tempFolder, fullFilename)[1].Value == "left";
      }

      static private string convertToGitlabFilename(string tempFolder, string fullFilename)
      {
         return parsePath(tempFolder, fullFilename)[2].Value;
      }

      static private string trimTemporaryFolder(string tempFolder, string fullFilename)
      {
         string trimmedFilename = fullFilename
            .Substring(tempFolder.Length, fullFilename.Length - tempFolder.Length)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
         return trimmedFilename;
      }

      static private GroupCollection parsePath(string tempFolder, string fullFilename)
      {
         string trimmed = trimTemporaryFolder(tempFolder, fullFilename);

         Match m = trimmedFilenameRe.Match(trimmed);
         if (!m.Success || m.Groups.Count < 3 || !m.Groups[1].Success || !m.Groups[2].Success)
         {
            throw new ApplicationException("Cannot parse a path obtained from difftool");
         }
         return m.Groups;
      }

      string[] _arguments;
   }
}
