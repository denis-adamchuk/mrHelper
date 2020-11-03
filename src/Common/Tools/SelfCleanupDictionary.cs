using System;
using System.Linq;
using System.Collections.Generic;

namespace mrHelper.Common.Tools
{
   public class SelfCleanUpDictionary<TKey, TValue>
   {
      public SelfCleanUpDictionary(int cleanupPeriodSeconds)
      {
         _cleanupPeriod = new TimeSpan(0, 0, cleanupPeriodSeconds);
      }

      public void Add(TKey key, TValue value)
      {
         _data.Add(key, value);
         _timestamps.Add(key, DateTime.Now);

         cleanup();
      }

      public bool ContainsKey(TKey key)
      {
         return _data.ContainsKey(key);
      }

      public bool TryGetValue(TKey key, out TValue value)
      {
         return _data.TryGetValue(key, out value);
      }

      private void cleanup()
      {
         if (!canCleanupNow())
         {
            return;
         }

         TKey[] toRemove = _timestamps
            .Where(kv => DateTime.Now.Subtract(kv.Value) > _cleanupPeriod)
            .Select(kv => kv.Key)
            .ToArray();
         foreach (TKey key in toRemove)
         {
            _data.Remove(key);
            _timestamps.Remove(key);
         }
         _latestCleanupTimestamp = DateTime.Now;
      }

      private bool canCleanupNow()
      {
         return !_latestCleanupTimestamp.HasValue
             || DateTime.Now.Subtract(_latestCleanupTimestamp.Value) > _cleanupFrequency;
      }

      private readonly Dictionary<TKey, TValue> _data = new Dictionary<TKey, TValue>();
      private readonly Dictionary<TKey, DateTime> _timestamps = new Dictionary<TKey, DateTime>();

      private readonly TimeSpan _cleanupPeriod;
      private DateTime? _latestCleanupTimestamp;
      private readonly static TimeSpan _cleanupFrequency = new TimeSpan(1, 0, 0); // 1 hour
   }
}

