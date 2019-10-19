using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   internal class SearchResults<TSearchResult>
   {
      internal SearchResults(TSearchResult[] results = null)
      {
         _results = results == null ? null : (TSearchResult[])results.Clone();
      }

      internal TSearchResult GetAt(int index)
      {
         return _results[index];
      }

      internal int Count()
      {
         return _results.Count();
      }

      internal SearchResultsCircularIterator<TSearchResult> First()
      {
         return new SearchResultsCircularIterator<TSearchResult>(this, 0);
      }

      internal SearchResultsCircularIterator<TSearchResult> Last()
      {
         return new SearchResultsCircularIterator<TSearchResult>(this, _results.Count() - 1);
      }

      private readonly TSearchResult[] _results;
   }

   internal struct SearchResultsCircularIterator<TSearchResult>
   {
      internal SearchResultsCircularIterator(SearchResults<TSearchResult> results, int startIndex)
      {
         _results = results;
         _currentResultIndex = _results.Count() > 0 ? startIndex : -1;
      }

      internal TSearchResult Value
      {
         get
         {
            if (_currentResultIndex == -1 || _results == null)
            {
               return default(TSearchResult);
            }
            return _results.GetAt(_currentResultIndex);
         }
      }

      internal bool Next()
      {
         return advance(true);
      }

      internal bool Prev()
      {
         return advance(false);
      }

      private bool advance(bool forward)
      {
         if (_currentResultIndex == -1 || _results == null)
         {
            return false;
         }

         int first = forward ? 0 : _results.Count() - 1;
         int last = forward ? _results.Count() - 1 : 0;
         int next = forward ? _currentResultIndex + 1 : _currentResultIndex - 1;
         _currentResultIndex = _currentResultIndex == last ? first : next;
         return true;
      }

      private readonly SearchResults<TSearchResult> _results;
      private int _currentResultIndex;
   }
}

