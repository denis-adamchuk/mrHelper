using System;
using System.Linq;
using mrHelper.App.Controls;

namespace mrHelper.App.Helpers
{
   internal static class SearchHelper
   {
      internal static bool SearchForward(ITextControl control, SearchQuery query, int startPosition,
         out int insideControlPosition)
      {
         StringComparison stringComparison = query.CaseSensitive ?
            StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
         int position = control.Text.IndexOf(query.Text, startPosition, stringComparison);
         if (position != -1)
         {
            insideControlPosition = position;
            return true;
         }
         insideControlPosition = -1;
         return false;
      }

      internal static bool SearchBackward(ITextControl control, SearchQuery query, int startPosition,
         out int insideControlPosition)
      {
         StringComparison stringComparison = query.CaseSensitive ?
            StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
         string reverseText = String.Join("", control.Text.Reverse());
         string reverseQuery = String.Join("", query.Text.Reverse());
         startPosition = control.Text.Length - startPosition;
         int position = reverseText.IndexOf(reverseQuery, startPosition, stringComparison);
         if (position != -1)
         {
            insideControlPosition = control.Text.Length - position - query.Text.Length;
            return true;
         }
         insideControlPosition = -1;
         return false;
      }
   }
}

