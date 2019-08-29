using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Core.Context
{
   public class SparsedList<T> : List<T> where T: class
   {
      public SparsedListIterator<T> Begin()
      {
         return new SparsedListIterator<T>(this, 0);
      }
   }
}
