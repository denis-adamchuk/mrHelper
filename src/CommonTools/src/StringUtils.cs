using System;

public static class StringUtils
{
   public static bool ContainsNoCase(string text, string substring)
   {
      return text.IndexOf(substring, StringComparison.CurrentCultureIgnoreCase) >= 0;
   }

   public static bool DoesMatchPattern(string value, string format, params object[] args)
   {
      return String.Compare(value, String.Format(format, args),
         StringComparison.CurrentCultureIgnoreCase) == 0;
   }
}

