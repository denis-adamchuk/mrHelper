using System;
using System.Linq;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   public class CommitBasedContextProvider : ICommitStorageUpdateContextProvider
   {
      public CommitBasedContextProvider(IEnumerable<string> shas)
      {
         _shas = shas;
      }

      public CommitStorageUpdateContext GetContext()
      {
         return new PartialUpdateContext(_shas.Distinct());
      }

      public override string ToString()
      {
         return String.Format("CommitBasedContextProvider. Sha Count: {0}", _shas.Count());
      }

      private readonly IEnumerable<string> _shas;
   }
}

