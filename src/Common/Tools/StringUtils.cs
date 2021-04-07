using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Common.Tools
{
   public static class StringUtils
   {
      public static bool ContainsNoCase(string text, string substring)
      {
         return text.IndexOf(substring, StringComparison.CurrentCultureIgnoreCase) >= 0;
      }

      public static bool DoesMatchPattern(string value, string format, params object[] args)
      {
         return String.Compare(value, String.Format(format, args), true) == 0;
      }

      public static bool DoesMatchPattern(IEnumerable<string> values, string format, params object[] args)
      {
         return values.All(value => DoesMatchPattern(value, format, args));
      }

      public static string EscapeSpaces(string unescaped)
      {
         return unescaped.Contains(' ') ? '"' + unescaped + '"' : unescaped;
      }

      public static string GetHostWithPrefix(string host)
      {
         if (String.IsNullOrWhiteSpace(host))
         {
            return host;
         }

         string supportedProtocolPrefix = "https://";
         string unsupportedProtocolPrefix = "http://";

         host = host.ToLower().TrimEnd('/');
         if (host.StartsWith(supportedProtocolPrefix))
         {
            return host;
         }
         else if (host.StartsWith(unsupportedProtocolPrefix))
         {
           return host.Replace(unsupportedProtocolPrefix, supportedProtocolPrefix);
         }

         return supportedProtocolPrefix + host;
      }

      public static string GetUploadsPrefix(ProjectKey projectKey)
      {
         if (String.IsNullOrEmpty(projectKey.HostName) || string.IsNullOrEmpty(projectKey.ProjectName))
         {
            return String.Empty;
         }
         return String.Format("{0}/{1}", StringUtils.GetHostWithPrefix(projectKey.HostName), projectKey.ProjectName);
      }

      public static string GetDefaultInstallLocation(string manufacturer)
      {
         return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            manufacturer, "mrHelper");
      }

      public static string GetShortcutFilePath()
      {
         return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            "mrHelper", "mrHelper.lnk");
      }

      public static string ConvertNewlineUnixToWindows(string input)
      {
         return isUnixNewline(input) ? input.Replace("\n", "\r\n") : input;
      }

      public static string ConvertNewlineWindowsToUnix(string input)
      {
         return isUnixNewline(input) ? input : input.Replace("\r\n", "\n");
      }

      private static bool isUnixNewline(string input)
      {
         int crCount = System.Text.RegularExpressions.Regex.Matches(input, "\r").Count;
         int lfCount = System.Text.RegularExpressions.Regex.Matches(input, "\n").Count;
         return lfCount > crCount * 2;
      }

      public static string GetGitLabAttachmentPrefix(string host, string projectname)
      {
         return String.Format("{0}/{1}", StringUtils.GetHostWithPrefix(host), projectname);
      }

      public static string AddAtSignToLetterSubstring(string word)
      {
         if (String.IsNullOrEmpty(word))
         {
            return String.Empty;
         }

         for (int iChar = 0; iChar < word.Length; ++iChar)
         {
            if (Char.IsLetter(word[iChar]))
            {
               if (iChar > 0 && word[iChar - 1] == '@')
               {
                  return word;
               }
               return word.Insert(iChar, "@");
            }
         }

         return word;
      }

      private static readonly char[] SpecialCharacters = new char[] { '<', '>', '\\' };

      private enum CodeBlockType
      {
         Span,
         Block,
         None
      }

      /// <summary>
      /// Detects whether characters from SpecialCharacters array appear in text not surrounded by apostrophes or tilde.
      /// See https://docs.gitlab.com/ee/user/markdown.html#code-spans-and-blocks
      /// Note that 4-spaces code indentation is not supported yet.
      /// This function should have been extracted into a separate parser of course.
      /// </summary>
      public static bool DoesContainUnescapedSpecialCharacters(string text)
      {
         bool isApostrophe(char ch) => ch == '`';
         bool isTilde(char ch) => ch == '~';

         int? startCount = null;
         bool isTildeBlock = false;
         bool areSpecialCharacterSinceLastApostrophe = false;

         void onOpenSpanOrBlock(int startCharacterCount, bool isTildeBlockOpens)
         {
            startCount = startCharacterCount;
            isTildeBlock = isTildeBlockOpens;
            areSpecialCharacterSinceLastApostrophe = false;
         };

         void onCloseSpanOrBlock()
         {
            startCount = null;
            isTildeBlock = false;
            areSpecialCharacterSinceLastApostrophe = false;
         }

         void onSpecialCharacterFound()
         {
            areSpecialCharacterSinceLastApostrophe = true;
         }

         CodeBlockType detectCodeBlockType() =>
            startCount.HasValue ? (startCount < 3 ? CodeBlockType.Span : CodeBlockType.Block) : CodeBlockType.None;

         for (int iCurrentChar = 0; iCurrentChar < text.Length; ++iCurrentChar)
         {
            char currentChar = text[iCurrentChar];

            bool isEmptyLineFound = iCurrentChar + 3 < text.Length
                  && text[iCurrentChar + 0] == '\r' && text[iCurrentChar + 1] == '\n'
                  && text[iCurrentChar + 2] == '\r' && text[iCurrentChar + 3] == '\n';
            bool isSpecialCharacterFound = SpecialCharacters.Contains(currentChar);
            bool isFenceCharacterFound = isApostrophe(currentChar) || isTilde(currentChar);

            int fenceCharacterCount = 0;
            if (isFenceCharacterFound)
            {
               while (iCurrentChar < text.Length && currentChar == text[iCurrentChar])
               {
                  ++iCurrentChar;
                  ++fenceCharacterCount;
               }
            }

            bool isBlockFenceFound = fenceCharacterCount >= 3;
            bool isSpanOrBlockFenceFound = isApostrophe(currentChar) || (isTilde(currentChar) && isBlockFenceFound);
            bool isFoundClosingFence = fenceCharacterCount > 0
               && startCount == fenceCharacterCount
               && (isTildeBlock ? isTilde(currentChar) : isApostrophe(currentChar));

            switch (detectCodeBlockType())
            {
               case CodeBlockType.None:
                  if (isSpanOrBlockFenceFound)
                  {
                     onOpenSpanOrBlock(fenceCharacterCount, isTilde(currentChar));
                  }
                  else if (isSpecialCharacterFound)
                  {
                     return true;
                  }
                  break;

               case CodeBlockType.Span:
                  if ((isBlockFenceFound || isEmptyLineFound) && areSpecialCharacterSinceLastApostrophe)
                  {
                     return true;
                  }
                  else if (isBlockFenceFound)
                  {
                     onOpenSpanOrBlock(fenceCharacterCount, isTilde(currentChar));
                  }
                  else if (isEmptyLineFound || isFoundClosingFence)
                  {
                     onCloseSpanOrBlock();
                  }
                  else if (isSpecialCharacterFound)
                  {
                     onSpecialCharacterFound();
                  }
                  break;

               case CodeBlockType.Block:
                  if (isFoundClosingFence)
                  {
                     onCloseSpanOrBlock();
                  }
                  else if (isSpecialCharacterFound)
                  {
                     onSpecialCharacterFound();
                  }
                  break;
            }
         }
         return areSpecialCharacterSinceLastApostrophe;
      }

      public static bool IsWorkInProgressTitle(string title)
      {
         return title != null && (title.StartsWith(WorkInProgressPrefix) || title.StartsWith(DraftPrefix));
      }

      public static string ToggleDraftTitle(string title)
      {
         if (title == null)
         {
            return title;
         }

         if (title.StartsWith(WorkInProgressPrefix))
         {
            return title.Substring(WorkInProgressPrefix.Length);
         }
         else if (title.StartsWith(DraftPrefix))
         {
            return title.Substring(DraftPrefix.Length);
         }
         return DraftPrefix + title;
      }

      public static byte[] GetBytesSafe(string value)
      {
         if (value != null)
         {
            try
            {
               return Encoding.ASCII.GetBytes(value);
            }
            catch (Exception ex) // Any exception from Encoding.ASCII.GetBytes()
            {
               Exceptions.ExceptionHandlers.Handle("Cannot get bytes from string", ex);
            }
         }
         return null;
      }

      public static string GetStringSafe(byte[] value)
      {
         if (value != null)
         {
            try
            {
               return Encoding.ASCII.GetString(value);
            }
            catch (Exception ex) // Any exception from Encoding.ASCII.GetString()
            {
               Exceptions.ExceptionHandlers.Handle("Cannot get string from bytes", ex);
            }
         }
         return null;
      }

      public static string JoinSubstrings(IEnumerable<string> substrings)
      {
         return String.Join("\n", substrings);
      }

      public static string JoinSubstringsLimited(IEnumerable<string> substrings, int maxRows, string maxRowsHint)
      {
         if (substrings.Count() == maxRows)
         {
            return JoinSubstrings(substrings.Take(maxRows));
         }
         string result = JoinSubstrings(substrings.Take(maxRows - 1));
         if (substrings.Count() > maxRows)
         {
            result += String.Format("\n{0}", maxRowsHint);
         }
         return result;
      }

      private static readonly string WorkInProgressPrefix = "WIP: ";
      private static readonly string DraftPrefix = "Draft: ";
   }
}

