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

      public static string Base64Decode(string base64EncodedData)
      {
        byte[] base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
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
   }
}

