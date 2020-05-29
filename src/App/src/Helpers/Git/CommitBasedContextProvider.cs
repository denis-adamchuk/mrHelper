using System;
using System.Linq;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal class CommitBasedContextProvider : IProjectUpdateContextProvider
   {
      internal CommitBasedContextProvider(IEnumerable<string> shas)
      {
         _shas = shas;
      }

      public ProjectUpdateContext GetContext()
      {
         return new PartialUpdateContext(_shas);
      }

      public override string ToString()
      {
         return String.Format("CommitBasedContextProvider. Sha Count: {0}", _shas.Count());
      }

      private readonly IEnumerable<string> _shas;
   }
}

