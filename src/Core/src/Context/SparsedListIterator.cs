using System;
using System.Collections.Generic;

namespace mrHelper.Core.Context
{
   public class BadPosition : Exception {}

   /// <summary>
   /// Iterator for SparsedList container
   /// </summary>
   public class SparsedListIterator<T> where T: class
   {
      /// <summary>
      /// Initialize an iterator with a container and start position
      /// </summary>
      public SparsedListIterator(List<T> list, int position)
      {
         _list = list;
         _lineNumber = position;
         _nullCount = calcNullCount(list, position);
      }

      /// <summary>
      /// Increment iterator
      /// </summary>
      public bool Next()
      {
         _lineNumber++;

         if (_lineNumber >= _list.Count)
         {
            return false;
         }

         if (_list[_lineNumber] == null)
         {
            _nullCount++;
         }

         return true;
      }

      /// <summary>
      /// Return number of the current line
      /// </summary>
      public int Position => _lineNumber;

      /// <summary>
      /// Return content of the current line (if it is a non-null line)
      /// </summary>
      public T Current
      {
         get
         {
            if (_lineNumber >= _list.Count)
            {
               throw new BadPosition();
            }

            return _list[_lineNumber] != null ? _list[_lineNumber] : null;
         }
      }

      /// <summary>
      /// Return number of the current line excluding null lines (if it is a non-null line)
      /// </summary>
      public int? LineNumber
      {
         get
         {
            if (_lineNumber >= _list.Count)
            {
               throw new BadPosition();
            }

            return _list[_lineNumber] != null ? _lineNumber - _nullCount : new Nullable<int>();
         }
      }

      /// <summary>
      /// Calculate number of 'null' lines prior to the passed linenumber
      /// </summary>
      private static int calcNullCount(List<T> list, int lineNumber)
      {
         int lineCount = 0;
         int nullCount = 0;

         for (int iLine = 0; iLine < list.Count; ++iLine)
         {
            if (list[iLine] == null)
            {
               nullCount++;
            }

            if (lineCount == lineNumber)
            {
               break;
            }
            lineCount++;
         }

         return nullCount;
      }

      // container
      private List<T> _list;

      // zero-based line number
      private int _lineNumber;

      // number of null lines before and on _lineNumber
      private int _nullCount;
   }
}

