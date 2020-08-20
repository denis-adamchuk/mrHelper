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
         BaseToHeadsCollection baseToHeads = new BaseToHeadsCollection(
            new Dictionary<BaseInfo, IEnumerable<HeadInfo>>
               { { new BaseInfo(_baseSha), _heads.Where(x => x != _baseSha).Select(x => new HeadInfo(x, null)) } });
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

