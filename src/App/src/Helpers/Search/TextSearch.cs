using mrHelper.App.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Helpers
{
   internal struct TextSearchResult
   {
      public Control Control;
      public int InsideControlPosition;
   }

   internal struct SearchQuery
   {
      public string Text;
      public bool CaseSensitive;
   }

   internal class TextSearch
   {
      internal TextSearch(Control container, SearchQuery query, Func<Control, bool> isSearchableControl)
      {
         Query = query;
         _allControls = getControls(container);
         _isSearchableControl = isSearchableControl;
      }

      private static Control[] getControls(Control container)
      {
         List<Control> controlList = new List<Control>();
         foreach (Control control in container.Controls)
         {
            controlList.AddRange(getControls(control));
            controlList.Add(control);
         }
         return controlList.ToArray();
      }

      internal SearchQuery Query { get; private set; }

      internal TextSearchResult? FindFirst(out int count)
      {
         count = 0;
         TextSearchResult? result = new Nullable<TextSearchResult>();

         foreach (Control control in _allControls)
         {
            if (_isSearchableControl(control))
            {
               int startPosition = 0;
               while (doesMatchText(control, Query, true, startPosition, out int insideControlPosition))
               {
                  if (!result.HasValue)
                  {
                     result = new TextSearchResult { Control = control, InsideControlPosition = insideControlPosition };
                  }
                  startPosition = insideControlPosition + 1;
                  ++count;
               }
            }
         }

         return result;
      }

      internal TextSearchResult? FindNext(TextSearchResult current)
      {
         int iCurrent = 0;
         while (_allControls[iCurrent] != current.Control) ++iCurrent;

         return find(0, _allControls.Count(), iCurrent, current.InsideControlPosition, true);
      }

      internal TextSearchResult? FindPrev(TextSearchResult current)
      {
         int iCurrent = _allControls.Count() - 1;
         while (_allControls[iCurrent] != current.Control) --iCurrent;

         return find(_allControls.Count() - 1, -1, iCurrent, current.InsideControlPosition, false);
      }

      internal TextSearchResult? find(int iStart, int iEnd, int iCurrent, int iCurrentInsideControlPosition, bool forward)
      {
         Control currentControl = _allControls[iCurrent];
         if (_isSearchableControl(currentControl) &&
             doesMatchText(currentControl, Query, forward, iCurrentInsideControlPosition, out int insideControlPosition))
         {
            return new TextSearchResult { Control = currentControl, InsideControlPosition = insideControlPosition };
         }

         for (int iControl = iCurrent + (forward ? 1 : -1); iControl != iEnd; iControl += (forward ? 1 : -1))
         {
            Control control = _allControls[iControl];
            int startPosition = forward ? 0 : control.Text.Length;
            if (_isSearchableControl(control) && doesMatchText(control, Query, forward, startPosition, out insideControlPosition))
            {
               return new TextSearchResult { Control = control, InsideControlPosition = insideControlPosition };
            }
         }

         for (int iControl = iStart; iControl != iCurrent + (forward ? 1 : -1); iControl += (forward ? 1 : -1))
         {
            Control control = _allControls[iControl];
            int startPosition = forward ? 0 : control.Text.Length;
            if (_isSearchableControl(control) && doesMatchText(control, Query, forward, startPosition, out insideControlPosition))
            {
               return new TextSearchResult { Control = control, InsideControlPosition = insideControlPosition };
            }
         }

         return null;
      }

      private bool doesMatchText(Control control, SearchQuery query, bool forward, int startPosition, out int insideControlPosition)
      {
         insideControlPosition = -1;
         if (startPosition < 0 || startPosition > control.Text.Length)
         {
            return false;
         }

         StringComparison stringComparison = query.CaseSensitive ?
            StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
         if (forward)
         {
            int position = control.Text.IndexOf(query.Text, startPosition, stringComparison);
            if (position != -1)
            {
               insideControlPosition = position;
               return true;
            }
         }
         else
         {
            string reverseText = String.Join("", control.Text.Reverse().ToArray());
            string reverseQuery = String.Join("", query.Text.Reverse().ToArray());
            startPosition = control.Text.Length - startPosition;
            int position = reverseText.IndexOf(reverseQuery, startPosition, stringComparison);
            if (position != -1)
            {
               insideControlPosition = control.Text.Length - position - query.Text.Length;
               return true;
            }
         }

         return false;
      }

      private readonly Control[] _allControls;
      private readonly Func<Control, bool> _isSearchableControl;
   }
}

