using System.Linq;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   internal class InternalUpdateContext
   {
      internal InternalUpdateContext(IEnumerable<string> sha)
      {
         Sha = sha;
      }

      internal IEnumerable<InternalUpdateContext> Split(int chunkSize)
      {
         List<InternalUpdateContext> splitted = new List<InternalUpdateContext>();
         int remaining = Sha.Count();
         while (remaining > 0)
         {
            string[] chunk = Sha
               .Skip(Sha.Count() - remaining)
               .Take(chunkSize)
               .ToArray();
            remaining -= chunk.Length;
            splitted.Add(new InternalUpdateContext(chunk));
         }
         return splitted;
      }

      internal IEnumerable<string> Sha { get; }
   }
}

