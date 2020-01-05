using System.Collections.Generic;

namespace mrHelper.Common.Types
{
   public class SparsedList<T> : List<T> where T: class
   {
      public SparsedListIterator<T> Begin()
      {
         return new SparsedListIterator<T>(this, 0);
      }
   }
}
