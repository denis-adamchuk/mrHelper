using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal class CommitBasedContextProvider : IProjectUpdateContextProvider
   {
      internal CommitBasedContextProvider(IEnumerable<string> shas)
      {
         _shas = shas.ToList();
      }

      public Task<IProjectUpdateContext> GetContext()
      {
         return Task.FromResult((new PartialUpdateContext { Sha = _shas }) as IProjectUpdateContext);
      }

      public override string ToString()
      {
         return String.Format("CommitBasedContextProvider. Sha Count: {0}", _shas.Count());
      }

      private readonly List<string> _shas;
   }
}

