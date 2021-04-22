using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Tools
{
   public class ListWrapper<TValue>
   {
      public ListWrapper(List<TValue> list, Action onChange)
      {
         _data = list.ToList(); // make a copy
         _onChange = onChange;
      }

      public void Add(TValue value)
      {
         _data.Add(value);
         _onChange();
      }

      public void Remove(TValue value)
      {
         if (_data.Remove(value))
         {
            _onChange();
         }
      }

      public List<TValue> Data => _data;

      private readonly List<TValue> _data = new List<TValue>();
      private readonly Action _onChange;
   }

   public class DictionaryWrapper<TKey, TValue>
   {
      public DictionaryWrapper(Dictionary<TKey, TValue> dictionary, Action onChange)
      {
         _data = dictionary.ToDictionary(item => item.Key, item => item.Value); // make a copy
         _onChange = onChange;
      }

      public void Add(TKey key, TValue value)
      {
         _data.Add(key, value);
         _onChange();
      }

      public TValue this[TKey key]
      {
         get => _data[key];
         set
         {
            _data[key] = value;
            _onChange();
         }
      }

      public void Remove(TKey key)
      {
         if (_data.Remove(key))
         {
            _onChange();
         }
      }

      public void RemoveMany(IEnumerable<TKey> keys)
      {
         foreach (TKey key in keys)
         {
            _data.Remove(key);
         }

         if (keys.Any())
         {
            _onChange();
         }
      }

      public Dictionary<TKey, TValue> Data => _data;

      private readonly Dictionary<TKey, TValue> _data = new Dictionary<TKey, TValue>();
      private readonly Action _onChange;
   }
}

