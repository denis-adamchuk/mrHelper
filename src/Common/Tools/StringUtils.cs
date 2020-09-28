using mrHelper.Common.Interfaces;
using System;
using System.Linq;

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

         host = host.ToLower();
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
         return input.Replace("\n", "\r\n");
      }

      public static string ConvertNewlineWindowsToUnix(string input)
      {
         return input.Replace("\r\n", "\n");
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

      public class WordInfo
      {
         public WordInfo(int start, string word)
         {
            Start = start;
            Word = word;
            IsValid = true;
         }

         public static WordInfo Invalid => new WordInfo();

         public readonly int Start;
         public readonly string Word;
         public readonly bool IsValid;

         private WordInfo()
         {
            IsValid = false;
         }
      }

      public static WordInfo GetCurrentWord(string text, int position)
      {
         if (position < 0 || position >= text.Length || text.Length == 0 || Char.IsWhiteSpace(text[position]))
         {
            return WordInfo.Invalid;
         }

         int start = position;
         while (start > 0 && !Char.IsWhiteSpace(text[start - 1])) { --start; }

         int end = position;
         while (end < text.Length && !Char.IsWhiteSpace(text[end])) { ++end; }

         return new WordInfo(start, text.Substring(start, end - start));
      }

      public static string ReplaceWord(string text, WordInfo word, string replacement)
      {
         string prefix = text.Substring(0, word.Start);
         string suffix = text.Substring(word.Start + word.Word.Length);
         return String.Format("{0}{1}{2}", prefix, replacement, suffix);
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
         return title != null && title.StartsWith(WorkInProgressPrefix);
      }

      public static string ToggleWorkInProgressTitle(string title)
      {
         if (title == null)
         {
            return title;
         }

         return title.StartsWith(WorkInProgressPrefix)
            ? title.Substring(WorkInProgressPrefix.Length) : WorkInProgressPrefix + title;
      }

      private static readonly string WorkInProgressPrefix = "WIP: ";
   }
}

