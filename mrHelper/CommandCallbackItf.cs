using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper
{
   interface ICommandCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestId();
   }
}
