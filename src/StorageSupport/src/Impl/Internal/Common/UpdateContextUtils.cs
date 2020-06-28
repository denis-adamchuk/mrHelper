using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mrHelper.StorageSupport
{
   internal static class UpdateContextUtils
   {
      internal static bool IsWorthNewUpdate(CommitStorageUpdateContext proposed, CommitStorageUpdateContext updating)
      {
         Debug.Assert(proposed != null);
         if (updating == null)
         {
            return true;
         }

         if (updating.LatestChange.HasValue && proposed.LatestChange.HasValue)
         {
            return proposed.LatestChange  > updating.LatestChange
               || (proposed.LatestChange == updating.LatestChange
                  && !areEqualShaCollections(proposed.BaseToHeads, updating.BaseToHeads));
         }
         else if (updating.LatestChange.HasValue || proposed.LatestChange.HasValue)
         {
            return true;
         }

         return !areEqualShaCollections(proposed.BaseToHeads, updating.BaseToHeads);
      }

      private static bool areEqualShaCollections(
         Dictionary<string, IEnumerable<string>> a,
         Dictionary<string, IEnumerable<string>> b)
      {
         if (!Enumerable.SequenceEqual(a.Keys.OrderBy(x => x), b.Keys.OrderBy(x => x)))
         {
            return false;
         }

         foreach (KeyValuePair<string, IEnumerable<string>> kv in a)
         {
            if (!Enumerable.SequenceEqual(kv.Value, b[kv.Key]))
            {
               return false;
            }
         }

         return true;
      }
   }
}

