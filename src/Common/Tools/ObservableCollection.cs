using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Tools
{
   public interface IObservableCollection<T> : IReadOnlyCollection<T>
   {
      bool Contains(T item);
      event Action Changed;
   }

   public class ObservableHashSet<T> : ICollection<T>, IObservableCollection<T>
   {
      public int Count => _objects.Count;

      public bool IsReadOnly => false;

      public event Action Changed;

      public void Add(T item)
      {
         if (_objects.Add(item))
         {
            Changed?.Invoke();
         }
      }

      public void Clear()
      {
         if (_objects.Any())
         {
            _objects.Clear();
            Changed?.Invoke();
         }
      }

      public bool Contains(T item)
      {
         return _objects.Contains(item);
      }

      public void CopyTo(T[] array, int arrayIndex)
      {
         _objects.CopyTo(array, arrayIndex);
      }

      public IEnumerator<T> GetEnumerator()
      {
         return _objects.GetEnumerator();
      }

      public bool Remove(T item)
      {
         if (_objects.Remove(item))
         {
            Changed?.Invoke();
            return true;
         }
         return false;
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _objects.GetEnumerator();
      }

      private HashSet<T> _objects = new HashSet<T>();
   }
}

