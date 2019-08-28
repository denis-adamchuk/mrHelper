using System;
using System.Collections.Generic;

namespace mrHelper.Core.Context
{
   public class SizeMismatch : Exception {}

   public class BadPosition : Exception {}

   /// <summary>
   /// Iterator for two lists of the same size. List items may contain null elements and iterator specially treats them.
   /// </summary>
   public class TwoListIterator<T> where T: class
   {
      /// <summary>
      /// Initialize an iterator with two containers and start position, which is a line number within one of the lists.
      /// </summary>
      public TwoListIterator(List<T> left, List<T> right, int startPosition, bool isRightSideStartPosition)
      {
         if (left.Count != right.Count)
         {
            throw new SizeMismatch();
         }

         calcNullCounts(left, right, startPosition, isRightSideStartPosition, out _leftNullCount, out _rightNullCount);
         _lineNumber = getAbsLineNumber(startPosition, isRightSideStartPosition, _leftNullCount, _rightNullCount);
         if (_lineNumber >= left.Count)
         {
            throw new BadPosition();
         }

         _left = left;
         _right = right;
      }

      /// <summary>
      /// Increment iterator
      /// </summary>
      public bool Next()
      {
         _lineNumber++;

         if (_lineNumber >= _left.Count)
         {
            return false;
         }

         if (_right[_lineNumber] == null)
         {
            _rightNullCount++;
         }

         if (_left[_lineNumber] == null)
         {
            _leftNullCount++;
         }

         return true;
      }

      /// <summary>
      /// Return content of the current line in the left list (if it is a non-null line)
      /// </summary>
      public T LeftLine()
      {
         if (_lineNumber >= _left.Count)
         {
            throw new BadPosition();
         }

         return _left[_lineNumber] != null ? _left[_lineNumber] : null;
      }

      /// <summary>
      /// Return content of the current line in the right list (if it is a non-null line)
      /// </summary>
      public T RightLine()
      {
         if (_lineNumber >= _right.Count)
         {
            throw new BadPosition();
         }

         return _right[_lineNumber] != null ? _right[_lineNumber] : null;
      }

      /// <summary>
      /// Return number of the current line in the left list (if it is a non-null line)
      /// </summary>
      public int? LeftLineNumber()
      {
         if (_lineNumber >= _left.Count)
         {
            throw new BadPosition();
         }

         return _left[_lineNumber] != null ? _lineNumber - _leftNullCount : new Nullable<int>();
      }

      /// <summary>
      /// Return number of the current line in the right list (if it is a non-null line)
      /// </summary>
      public int? RightLineNumber()
      {
         if (_lineNumber >= _right.Count)
         {
            throw new BadPosition();
         }

         return _right[_lineNumber] != null ? _lineNumber - _rightNullCount : new Nullable<int>();
      }

      /// <summary>
      /// Convert a line number within a list to a number of line in both list
      /// </summary>
      private static int getAbsLineNumber(int lineNumber, bool isRightSideLineNumber,
         int leftNullCount, int rightNullCount)
      {
         return isRightSideLineNumber ? lineNumber + rightNullCount : lineNumber + leftNullCount;
      }

      /// <summary>
      /// Calculate number of 'null' lines at each side prior to the passed linenumber
      /// </summary>
      private static void calcNullCounts(List<T> left, List<T> right, int lineNumber, bool isRightSideLineNumber,
         out int leftNullCount, out int rightNullCount)
      {
         int lineCount = 0; // counts lines at the right side if isRightSideLineNumber is true
         leftNullCount = 0;
         rightNullCount = 0;

         for (int iLine = 0; iLine < left.Count; ++iLine)
         {
            if (left[iLine] == null)
            {
               leftNullCount++;
            }

            if (right[iLine] == null)
            {
               rightNullCount++;
            }

            if ((isRightSideLineNumber && right[iLine] != null)
            || (!isRightSideLineNumber && left[iLine] != null))
            {
               ++lineCount;
               if (lineCount == lineNumber + 1)
               {
                  // we're finishing to calculate 'null' lines when passed linenumber is reached
                  break;
               }
            }
         }
      }

      // left-side list
      private List<T> _left;

      // right-side list
      private List<T> _right;

      // zero-based line number in both lists
      private int _lineNumber = 0;

      // number of null lines at the left side before and on current line
      private int _leftNullCount = 0;

      // number of null lines at the right side before and on current line
      private int _rightNullCount = 0;
   }
}

