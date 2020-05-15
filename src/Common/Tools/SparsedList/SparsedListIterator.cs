using System;
using System.Collections.Generic;

namespace mrHelper.Common.Tools
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
         Position = position;
         _nullCount = calcNullCount(list, position);
      }

      /// <summary>
      /// Increment iterator
      /// </summary>
      public bool Next()
      {
         Position++;

         if (Position >= _list.Count)
         {
            return false;
         }

         if (_list[Position] == null)
         {
            _nullCount++;
         }

         return true;
      }

      /// <summary>
      /// Return number of the current line
      /// </summary>
      public int Position { get; private set; }

      /// <summary>
      /// Return content of the current line (if it is a non-null line)
      /// </summary>
      public T GetCurrent()
      {
         if (Position >= _list.Count)
         {
            throw new BadPosition();
         }

         return _list[Position] ?? null;
      }

      /// <summary>
      /// Return number of the current line excluding null lines (if it is a non-null line)
      /// </summary>
      public int? GetLineNumber()
      {
         if (Position >= _list.Count)
         {
            throw new BadPosition();
         }

         return _list[Position] != null ? Position - _nullCount : new Nullable<int>();
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
      private readonly List<T> _list;

      // number of null lines before and on _lineNumber
      private int _nullCount;
   }
}

