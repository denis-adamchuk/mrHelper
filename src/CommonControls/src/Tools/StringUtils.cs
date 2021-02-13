using System;

namespace mrHelper.CommonControls.Tools
{
   public static class TextUtils
   {
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
   }
}

