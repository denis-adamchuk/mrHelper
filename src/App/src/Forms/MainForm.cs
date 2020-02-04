using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;

namespace mrHelper.App.Forms
{
   internal partial class MainForm : Form
   {
      internal MainForm()
      {
         InitializeComponent();
         Debug.Assert(textBox1.Location.X >= 0 && textBox1.Location.Y >= 0);
      }
   }
}

