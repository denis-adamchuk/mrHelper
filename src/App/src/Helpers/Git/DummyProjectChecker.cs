using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   internal class DummyProjectChecker : IInstantProjectChecker
   {
      internal DummyProjectChecker(IEnumerable<string> shas)
      {
         _shas = shas.ToList();
      }

      public Task<ProjectSnapshot> GetProjectSnapshot()
      {
         return Task.FromResult(new ProjectSnapshot { Sha = _shas });
      }

      private List<string> _shas;
   }
}
