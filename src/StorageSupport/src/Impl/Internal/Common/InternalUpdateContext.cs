using System.Linq;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   internal class InternalUpdateContext
   {
      internal InternalUpdateContext(Dictionary<string, IEnumerable<string>> baseToHeads)
      {
         BaseToHeads = baseToHeads;
      }

      internal IEnumerable<InternalUpdateContext> Split(int chunkSize)
      {
         List<InternalUpdateContext> splitted = new List<InternalUpdateContext>();
         foreach (KeyValuePair<string, IEnumerable<string>> kv in BaseToHeads)
         {
            string baseSha = kv.Key;
            int remaining = kv.Value.Count();
            while (remaining > 0)
            {
               string[] headsChunk = kv.Value
                  .Skip(kv.Value.Count() - remaining)
                  .Take(chunkSize)
                  .ToArray();
               remaining -= headsChunk.Length;
               var chunk = new Dictionary<string, IEnumerable<string>> { { baseSha, headsChunk } };
               splitted.Add(new InternalUpdateContext(chunk));
            }
         }
         return splitted;
      }

      internal Dictionary<string, IEnumerable<string>> BaseToHeads { get; }
   }
}

