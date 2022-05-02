using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.Common.Tools
{
   public class DictionaryWrapper<TKey, TValue>
   {
      public DictionaryWrapper(Action onChange)
      {
         _onChange = onChange;
         _data = new Dictionary<TKey, TValue>();
      }

      public void Assign(Dictionary<TKey, TValue> dictionary)
      {
         _data.Clear();
         foreach (var kv in dictionary)
         {
            _data[kv.Key] = kv.Value;
         }
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
         bool anyRemoved = false;
         foreach (TKey key in keys)
         {
            anyRemoved |= _data.Remove(key);
         }

         if (anyRemoved)
         {
            _onChange();
         }
      }

      public IReadOnlyDictionary<TKey, TValue> Data => _data;

      private readonly Dictionary<TKey, TValue> _data = new Dictionary<TKey, TValue>();
      private readonly Action _onChange;
   }

   public class HashSetWrapper<TKey>
   {
      public HashSetWrapper(Action onChange)
      {
         _onChange = onChange;
         _data = new HashSet<TKey>();
      }

      public void Assign(HashSet<TKey> source)
      {
         _data.Clear();
         foreach (var key in source)
         {
            _data.Add(key);
         }
      }

      public void Add(TKey key)
      {
         _data.Add(key);
         _onChange();
      }

      public void Remove(TKey key)
      {
         if (_data.Remove(key))
         {
            _onChange();
         }
      }

      public IReadOnlyCollection<TKey> Data => _data;

      private readonly HashSet<TKey> _data = new HashSet<TKey>();
      private readonly Action _onChange;
   }
}

