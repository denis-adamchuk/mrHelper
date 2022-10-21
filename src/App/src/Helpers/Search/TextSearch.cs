using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Controls;

namespace mrHelper.App.Helpers
{
   internal class TextSearch
   {
      internal TextSearch(IEnumerable<ITextControl> allControls, SearchQuery query)
      {
         Query = query;
         _allControls = allControls.ToArray();
      }

      internal SearchQuery Query { get; private set; }

      internal IEnumerable<TextSearchResult> FindAll()
      {
         List<TextSearchResult> results = new List<TextSearchResult>();

         foreach (ITextControl textControl in _allControls)
         {
            int startPosition = 0;
            while (doesMatchText(textControl, Query, true, startPosition, out int insideControlPosition))
            {
               results.Add(new TextSearchResult(textControl, insideControlPosition));
               startPosition = insideControlPosition + 1;
            }
         }

         return results;
      }

      internal TextSearchResult? FindFirst(out int count)
      {
         IEnumerable<TextSearchResult> allResults = FindAll();
         count = allResults.Any() ? allResults.Count() : 0;
         return allResults.Any() ? allResults.First() : new TextSearchResult?();
      }

      internal TextSearchResult? FindNext(ITextControl control, int startPosition)
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

      internal TextSearchResult? FindPrev(ITextControl control, int startPosition)
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

