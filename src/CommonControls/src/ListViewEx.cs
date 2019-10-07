using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.CommonControls
{
   public class ListViewEx : ListView
   {
      public ListViewEx()
      {
         DoubleBuffered = true;
      }
   }
}
