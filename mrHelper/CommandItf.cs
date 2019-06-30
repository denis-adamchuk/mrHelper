using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper
{
   interface ICommand
   {
      string GetName();

      void Run(object sender, System.EventArgs e);
   }
}
