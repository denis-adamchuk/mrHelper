using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Controls;

namespace mrHelper.App.Helpers
{
   internal struct TextSearchResult
   {
      public TextSearchResult(ITextControl control, int insideControlPosition)
      {
         Control = control;
         InsideControlPosition = insideControlPosition;
      }

      public ITextControl Control { get; }
      public int InsideControlPosition { get; }
   }

   internal struct SearchQuery : IEquatable<SearchQuery>
   {
      public SearchQuery(string text, bool caseSensitive)
      {
         Text = text;
         CaseSensitive = caseSensitive;
      }

      public string Text { get; }
      public bool CaseSensitive { get; }

      public override bool Equals(object obj)
      {
         return obj is SearchQuery query && Equals(query);
      }

      public bool Equals(SearchQuery other)
      {
         return Text == other.Text &&
                CaseSensitive == other.CaseSensitive;
      }

      public override int GetHashCode()
      {
         int hashCode = -102066407;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
         hashCode = hashCode * -1521134295 + CaseSensitive.GetHashCode();
         return hashCode;
      }
   }

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

   internal class TextSearch
   {
      internal TextSearch(IEnumerable<ITextControl> allControls, SearchQuery query)
      {
         Query = query;
         _allControls = allControls.ToArray();
      }

      internal SearchQuery Query { get; private set; }

      internal TextSearchResult? FindFirst(out int count)
      {
         count = 0;
         TextSearchResult? result = new TextSearchResult?();

         foreach (ITextControl textControl in _allControls)
         {
            int startPosition = 0;
            while (doesMatchText(textControl, Query, true, startPosition, out int insideControlPosition))
            {
               if (!result.HasValue)
               {
                  result = new TextSearchResult(textControl, insideControlPosition);
               }
               startPosition = insideControlPosition + 1;
               ++count;
            }
         }

         return result;
      }

      internal TextSearchResult? FindNext(Control control, int startPosition)
      {
         int iDefaultCurrent = 0;
         int iCurrent = iDefaultCurrent;
         while (iCurrent < _allControls.Length && _allControls[iCurrent] != control) ++iCurrent;
         if (iCurrent >= _allControls.Length)
         {
            iCurrent = iDefaultCurrent;
         }

         return iCurrent < _allControls.Length ? find(0, _allControls.Length, iCurrent, startPosition, true) : null;
      }

      internal TextSearchResult? FindPrev(Control control, int startPosition)
      {
         int iDefaultCurrent = _allControls.Count() - 1;
         int iCurrent = iDefaultCurrent;
         while (iCurrent >= 0 && _allControls[iCurrent] != control) --iCurrent;
         if (iCurrent < 0)
         {
            iCurrent = iDefaultCurrent;
         }

         return iCurrent >= 0 ? find(_allControls.Count() - 1, -1, iCurrent, startPosition, false) : null;
      }

      internal TextSearchResult? find(int iStart, int iEnd, int iCurrent, int iCurrentInsideControlPosition,
         bool forward)
      {
         ITextControl currentTextControl = _allControls[iCurrent];
         if (doesMatchText(currentTextControl, Query, forward, iCurrentInsideControlPosition,
               out int insideControlPosition))
         {
            return new TextSearchResult(currentTextControl, insideControlPosition);
         }

         for (int iControl = iCurrent + (forward ? 1 : -1); iControl != iEnd; iControl += (forward ? 1 : -1))
         {
            ITextControl textControl = _allControls[iControl];
            if (doesMatchText(textControl, Query, forward, forward ? 0 : textControl.Text.Length,
                  out insideControlPosition))
            {
               return new TextSearchResult(textControl, insideControlPosition);
            }
         }

         for (int iControl = iStart; iControl != iCurrent + (forward ? 1 : -1); iControl += (forward ? 1 : -1))
         {
            ITextControl textControl = _allControls[iControl];
            if (doesMatchText(textControl, Query, forward, forward ? 0 : textControl.Text.Length,
                  out insideControlPosition))
            {
               return new TextSearchResult(textControl, insideControlPosition);
            }
         }

         return null;
      }

      private bool doesMatchText(ITextControl control, SearchQuery query, bool forward, int startPosition,
         out int insideControlPosition)
      {
         insideControlPosition = -1;
         if (startPosition < 0 || startPosition > control.Text.Length)
         {
            return false;
         }

         return forward
            ? SearchHelper.SearchForward(control, query, startPosition, out insideControlPosition)
            : SearchHelper.SearchBackward(control, query, startPosition, out insideControlPosition);
      }

      private readonly ITextControl[] _allControls;
   }
}

