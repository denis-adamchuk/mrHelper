using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   internal struct SearchResults<TSearchResult>
   {
      public SearchResults(TSearchResult[] results, bool forward)
      {
         _results = results;

         int first = forward ? 0 : _results.Count() - 1;
         _currentResultIndex = _results.Count() > 0 ? first : -1;
      }

      internal TSearchResult Current
      {
         get
         {
            if (_currentResultIndex == -1 || _results == null)
            {
               return default(TSearchResult);
            }
            return _results[_currentResultIndex];
         }
      }

      internal bool MoveNext(bool forward)
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

      internal int Count
      {
         get
         {
            return _results?.Count() ?? 0;
         }
      }

      private readonly TSearchResult[] _results;
      private int _currentResultIndex;
   }
}

