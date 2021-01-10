using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Interfaces
{
   public interface IConnectionLossListener
   {
      void OnConnectionLost(string hostname);
   }
}

