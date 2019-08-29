using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Core.Context
{
   public static class SparsedListUtils
   {
      /// <summary>
      /// Finds an N-th non-null item, where N is passed in 'index' argument
      /// </summary>
      public static SparsedListIterator<T> FindNth<T>(SparsedListIterator<T> iterator, int index) where T: class
      {
         int skipped = 0;
         while (true)
         {
            if (iterator.LineNumber.HasValue)
            {
               if (skipped == index)
               {
                  break;
               }
               skipped++;
            }

            if (!iterator.Next())
            {
               break;
            }
         }
         return iterator;
      }

      /// <summary>
      /// Advances passed iterator 'index' positions forward
      /// </summary>
      public static SparsedListIterator<T> Advance<T>(SparsedListIterator<T> iterator, int index) where T: class
      {
         int skipped = 0;
         while (true)
         {
            if (skipped == index)
            {
               break;
            }
            skipped++;

            if (!iterator.Next())
            {
               break;
            }
         }
         return iterator;
      }
   }
}

