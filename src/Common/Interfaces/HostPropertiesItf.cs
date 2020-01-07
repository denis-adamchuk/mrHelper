using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IHostProperties
   {
      string GetAccessToken(string host);

      IEnumerable<string> GetEnabledProjects(string host);
   }
}

