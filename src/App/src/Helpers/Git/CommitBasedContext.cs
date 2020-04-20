using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal class CommitBasedContext : IProjectUpdateContext
   {
      internal CommitBasedContext(IEnumerable<string> shas)
      {
         _shas = shas.ToList();
      }

      public Task<IProjectUpdate> GetUpdate()
      {
         return Task.FromResult((new PartialProjectUpdate { Sha = _shas }) as IProjectUpdate);
      }

      public override string ToString()
      {
         return String.Format("CommitBasedUpdateFactory. Sha Count: {0}", _shas.Count());
      }

      private readonly List<string> _shas;
   }
}

