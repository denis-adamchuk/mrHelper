using System;
using System.Linq;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   public class CommitBasedContextProvider : ICommitStorageUpdateContextProvider
   {
      public CommitBasedContextProvider(IEnumerable<string> heads, string baseSha)
      {
         _heads = heads.Distinct();
         _baseSha = baseSha;
      }

      public CommitStorageUpdateContext GetContext()
      {
         Dictionary<string, IEnumerable<string>> baseToHeads =
            new Dictionary<string, IEnumerable<string>>{ { _baseSha, _heads.Where(x => x != _baseSha) } };
         return new PartialUpdateContext(baseToHeads);
      }

      public override string ToString()
      {
         return String.Format("CommitBasedContextProvider. Sha Count: {0}. Base Sha: {1}", _heads.Count(), _baseSha);
      }

      private readonly IEnumerable<string> _heads;
      private readonly string _baseSha;
   }
}

