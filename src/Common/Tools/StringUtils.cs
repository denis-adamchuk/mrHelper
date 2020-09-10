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
         if (position < 0 || position >= text.Length || text.Length == 0 || text[position] == ' ')
         {
            return WordInfo.Invalid;
         }

         int start = position;
         while (start > 0 && text[start - 1] != ' ') { --start; }

         int end = position;
         while (end < text.Length && text[end] != ' ') { ++end; }

         return new WordInfo(start, text.Substring(start, end - start));
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

      private static string WorkInProgressPrefix = "WIP: ";
   }
}

